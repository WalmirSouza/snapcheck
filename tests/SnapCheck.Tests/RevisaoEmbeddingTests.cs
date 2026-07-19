using Dapper;
using SnapCheck.Data;
using SnapCheck.Data.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Tests;

/// <summary>
/// Cobre o item 03.5 (ADR 0005, Decisões 2/3): atualização supervisionada de
/// embeddings por blend ponderado quando uma revisão de baixa confiança é
/// confirmada, e os casos em que NÃO deve atualizar.
/// </summary>
[Collection("Postgres")]
public sealed class RevisaoEmbeddingTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly IPessoaRepository _pessoaRepository = new PessoaRepository(fixture.ConnectionFactory);
    private readonly IRevisaoPresencaRepository _revisaoRepository = new RevisaoPresencaRepository(fixture.ConnectionFactory);
    private int _tenantId;
    private int _pessoaId;

    public async Task InitializeAsync()
    {
        _tenantId = await CriarTenantAsync();
        _pessoaId = await _pessoaRepository.InserirAsync(_tenantId, "Pessoa Embedding Teste", EmbeddingHelper.ToBytes(EmbeddingConstante(1.0f)));
    }

    public async Task DisposeAsync()
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM pessoa_embeddings_historico WHERE pessoa_id = @pessoaId", new { pessoaId = _pessoaId });
        await connection.ExecuteAsync("DELETE FROM revisao_presenca_auditoria WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM revisao_faces_itens WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM evidencias_foto_grupo WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM revisoes_presenca_grupo WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM presencas WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM pessoas WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM tenants WHERE id = @tenantId", new { tenantId = _tenantId });
    }

    [Fact]
    public async Task ConfirmarAprovadaComBaixaConfianca_FazBlendPonderadoDoEmbedding()
    {
        var revisaoId = await _revisaoRepository.CriarAsync(new RevisaoCriacaoInput
        {
            TenantId = _tenantId,
            Turma = "Turma Teste",
            CriadoPor = "chat:1",
            ExpiraEm = DateTime.UtcNow.AddHours(24),
            ReferenciaArquivo = "foto.jpg",
            Itens =
            [
                new RevisaoCriacaoItemInput
                {
                    PessoaSugeridaId = _pessoaId,
                    NomeSugerido = "Pessoa Embedding Teste",
                    Confianca = 0.35f, // faixa incerta (0.30-0.42)
                    Embedding = EmbeddingHelper.ToBytes(EmbeddingConstante(0.0f))
                }
            ]
        });

        var detalhe = await _revisaoRepository.ObterDetalheAsync(_tenantId, revisaoId);
        var itemId = detalhe!.Itens.Single().Id;

        var resultado = await _revisaoRepository.ConfirmarAsync(new RevisaoConfirmacaoInput
        {
            RevisaoId = revisaoId,
            TenantId = _tenantId,
            ConfirmadoPor = "Professor Teste",
            PerfilConfirmador = "professor",
            Itens = [new RevisaoConfirmacaoItemInput { ItemId = itemId, Decisao = RevisaoDecisao.Aprovada }]
        });

        Assert.Equal(ConfirmacaoRevisaoStatus.Confirmada, resultado.Status);

        var pessoa = await _pessoaRepository.ObterPorNomeAsync(_tenantId, "Pessoa Embedding Teste");
        var embeddingFinal = EmbeddingHelper.FromBytes(pessoa!.Embedding);

        // 0.7 * 1.0 + 0.3 * 0.0 = 0.7 (ADR 0005, Decisão 2)
        Assert.All(embeddingFinal, valor => Assert.Equal(0.7f, valor, precision: 4));

        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        var historico = await connection.QueryFirstOrDefaultAsync(
            "SELECT embedding_anterior, revisao_id, motivo FROM pessoa_embeddings_historico WHERE pessoa_id = @pessoaId",
            new { pessoaId = _pessoaId });

        Assert.NotNull(historico);
        Assert.Equal(revisaoId, (int)historico!.revisao_id);
        Assert.Equal("revisao_confirmada", (string)historico!.motivo);
        Assert.All(EmbeddingHelper.FromBytes((byte[])historico!.embedding_anterior), valor => Assert.Equal(1.0f, valor, precision: 4));
    }

    [Fact]
    public async Task ConfirmarComAltaConfianca_NaoAtualizaEmbedding()
    {
        // Alta confiança (>= 0.42) só cai em revisão por causa de múltiplos rostos
        // na foto (ADR 0005, Decisão 3) — não deve reforçar o embedding.
        var revisaoId = await _revisaoRepository.CriarAsync(new RevisaoCriacaoInput
        {
            TenantId = _tenantId,
            Turma = "Turma Teste",
            CriadoPor = "chat:1",
            ExpiraEm = DateTime.UtcNow.AddHours(24),
            ReferenciaArquivo = "foto.jpg",
            Itens =
            [
                new RevisaoCriacaoItemInput
                {
                    PessoaSugeridaId = _pessoaId,
                    NomeSugerido = "Pessoa Embedding Teste",
                    Confianca = 0.90f,
                    Embedding = EmbeddingHelper.ToBytes(EmbeddingConstante(0.0f))
                }
            ]
        });

        var detalhe = await _revisaoRepository.ObterDetalheAsync(_tenantId, revisaoId);
        var itemId = detalhe!.Itens.Single().Id;

        await _revisaoRepository.ConfirmarAsync(new RevisaoConfirmacaoInput
        {
            RevisaoId = revisaoId,
            TenantId = _tenantId,
            ConfirmadoPor = "Professor Teste",
            PerfilConfirmador = "professor",
            Itens = [new RevisaoConfirmacaoItemInput { ItemId = itemId, Decisao = RevisaoDecisao.Aprovada }]
        });

        var pessoa = await _pessoaRepository.ObterPorNomeAsync(_tenantId, "Pessoa Embedding Teste");
        var embeddingFinal = EmbeddingHelper.FromBytes(pessoa!.Embedding);

        // Embedding original (1.0) não foi alterado.
        Assert.All(embeddingFinal, valor => Assert.Equal(1.0f, valor, precision: 4));
    }

    [Fact]
    public async Task ConfirmarReatribuidaParaOutraPessoa_NaoAtualizaEmbeddingDaSugerida()
    {
        var outraPessoaId = await _pessoaRepository.InserirAsync(
            _tenantId, "Outra Pessoa", EmbeddingHelper.ToBytes(EmbeddingConstante(2.0f)));

        var revisaoId = await _revisaoRepository.CriarAsync(new RevisaoCriacaoInput
        {
            TenantId = _tenantId,
            Turma = "Turma Teste",
            CriadoPor = "chat:1",
            ExpiraEm = DateTime.UtcNow.AddHours(24),
            ReferenciaArquivo = "foto.jpg",
            Itens =
            [
                new RevisaoCriacaoItemInput
                {
                    PessoaSugeridaId = _pessoaId,
                    NomeSugerido = "Pessoa Embedding Teste",
                    Confianca = 0.35f,
                    Embedding = EmbeddingHelper.ToBytes(EmbeddingConstante(0.0f))
                }
            ]
        });

        var detalhe = await _revisaoRepository.ObterDetalheAsync(_tenantId, revisaoId);
        var itemId = detalhe!.Itens.Single().Id;

        await _revisaoRepository.ConfirmarAsync(new RevisaoConfirmacaoInput
        {
            RevisaoId = revisaoId,
            TenantId = _tenantId,
            ConfirmadoPor = "Professor Teste",
            PerfilConfirmador = "professor",
            Itens = [new RevisaoConfirmacaoItemInput { ItemId = itemId, Decisao = RevisaoDecisao.Reatribuida, PessoaFinalId = outraPessoaId }]
        });

        var pessoaSugerida = await _pessoaRepository.ObterPorNomeAsync(_tenantId, "Pessoa Embedding Teste");
        var embeddingSugerida = EmbeddingHelper.FromBytes(pessoaSugerida!.Embedding);

        // Embedding da pessoa originalmente sugerida (errada) não foi mexido.
        Assert.All(embeddingSugerida, valor => Assert.Equal(1.0f, valor, precision: 4));
    }

    private static float[] EmbeddingConstante(float valor) => Enumerable.Repeat(valor, 128).ToArray();

    private async Task<int> CriarTenantAsync()
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO tenants (nome, codigo_ativacao, status) VALUES (@nome, @codigo, 'ativo') RETURNING id",
            new { nome = "Tenant Embedding Teste", codigo = $"teste-embedding-{Guid.NewGuid():N}" });
    }
}
