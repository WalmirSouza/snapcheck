using System.Text.Json;
using Dapper;
using SnapCheck.Data.Models;

namespace SnapCheck.Data.Repositories;

public interface IRevisaoPresencaRepository
{
    Task<int> CriarAsync(RevisaoCriacaoInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RevisaoResumo>> ListarPendentesAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<RevisaoDetalhe?> ObterDetalheAsync(int tenantId, int revisaoId, CancellationToken cancellationToken = default);
    Task<ConfirmacaoRevisaoResultado> ConfirmarAsync(
        RevisaoConfirmacaoInput input,
        CancellationToken cancellationToken = default);
}

public sealed class RevisaoPresencaRepository(IDbConnectionFactory connectionFactory) : IRevisaoPresencaRepository
{
    // Mantido em sync manualmente com o default de FaceService.CompararComCadastro
    // (ADR 0005, Decisão 3) — abaixo disso é o caso de real ganho de aprendizado
    // supervisionado; igual ou acima já teria sido auto-aceito de qualquer forma.
    private const float LimiarAutoAceite = 0.42f;
    private const float PesoEmbeddingAtual = 0.7f;
    private const float PesoEmbeddingNovo = 0.3f;


    public async Task<int> CriarAsync(RevisaoCriacaoInput input, CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sqlRevisao = """
            INSERT INTO revisoes_presenca_grupo (tenant_id, turma, chat_id, status, expira_em, criado_por, criado_em)
            VALUES (@tenantId, @turma, @chatId, @status, @expiraEm, @criadoPor, NOW())
            RETURNING id
            """;

        var revisaoId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sqlRevisao,
                new
                {
                    tenantId = input.TenantId,
                    turma = input.Turma,
                    chatId = input.ChatId,
                    status = RevisaoStatus.Pendente,
                    expiraEm = input.ExpiraEm,
                    criadoPor = input.CriadoPor
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string sqlEvidencia = """
            INSERT INTO evidencias_foto_grupo (tenant_id, revisao_id, origem, referencia_arquivo, expira_em)
            VALUES (@tenantId, @revisaoId, @origem, @referenciaArquivo, @expiraEm)
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sqlEvidencia,
                new
                {
                    tenantId = input.TenantId,
                    revisaoId,
                    origem = input.Origem,
                    referenciaArquivo = input.ReferenciaArquivo,
                    expiraEm = input.ExpiraEm
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string sqlItem = """
            INSERT INTO revisao_faces_itens
                (revisao_id, tenant_id, pessoa_sugerida_id, nome_sugerido, confianca, embedding, decisao)
            VALUES
                (@revisaoId, @tenantId, @pessoaSugeridaId, @nomeSugerido, @confianca, @embedding, @decisao)
            """;

        foreach (var item in input.Itens)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sqlItem,
                    new
                    {
                        revisaoId,
                        tenantId = input.TenantId,
                        pessoaSugeridaId = item.PessoaSugeridaId,
                        nomeSugerido = item.NomeSugerido,
                        confianca = item.Confianca,
                        embedding = item.Embedding,
                        decisao = RevisaoDecisao.Pendente
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }

        await RegistrarAuditoriaAsync(
            connection,
            transaction,
            input.TenantId,
            revisaoId,
            "revisao_criada",
            JsonSerializer.Serialize(new { totalItens = input.Itens.Count }),
            input.CriadoPor,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return revisaoId;
    }

    public async Task<IReadOnlyList<RevisaoResumo>> ListarPendentesAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.id,
                   r.tenant_id AS TenantId,
                   r.turma AS Turma,
                   r.status AS Status,
                   r.expira_em AS ExpiraEm,
                   r.criado_por AS CriadoPor,
                   r.criado_em AS CriadoEm,
                   COUNT(i.id) AS TotalItens,
                   COUNT(*) FILTER (WHERE i.decisao = 'pendente') AS ItensPendentes
            FROM revisoes_presenca_grupo r
            LEFT JOIN revisao_faces_itens i ON i.revisao_id = r.id
            WHERE r.tenant_id = @tenantId
              AND r.status = 'pendente'
            GROUP BY r.id, r.tenant_id, r.turma, r.status, r.expira_em, r.criado_por, r.criado_em
            ORDER BY r.criado_em DESC
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<RevisaoResumo>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<RevisaoDetalhe?> ObterDetalheAsync(
        int tenantId,
        int revisaoId,
        CancellationToken cancellationToken = default)
    {
        const string sqlRevisao = """
            SELECT id,
                   tenant_id AS TenantId,
                   turma AS Turma,
                   chat_id AS ChatId,
                   status AS Status,
                   expira_em AS ExpiraEm,
                   criado_por AS CriadoPor,
                   criado_em AS CriadoEm,
                   confirmado_por AS ConfirmadoPor,
                   confirmado_em AS ConfirmadoEm
            FROM revisoes_presenca_grupo
            WHERE id = @revisaoId
              AND tenant_id = @tenantId
            LIMIT 1
            """;

        const string sqlItens = """
            SELECT id,
                   revisao_id AS RevisaoId,
                   tenant_id AS TenantId,
                   pessoa_sugerida_id AS PessoaSugeridaId,
                   nome_sugerido AS NomeSugerido,
                   confianca AS Confianca,
                   decisao AS Decisao,
                   pessoa_final_id AS PessoaFinalId,
                   decidido_por AS DecididoPor,
                   decidido_em AS DecididoEm,
                   motivo AS Motivo
            FROM revisao_faces_itens
            WHERE revisao_id = @revisaoId
              AND tenant_id = @tenantId
            ORDER BY id
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var revisao = await connection.QueryFirstOrDefaultAsync<RevisaoDetalhe>(
            new CommandDefinition(sqlRevisao, new { tenantId, revisaoId }, cancellationToken: cancellationToken));

        if (revisao is null)
        {
            return null;
        }

        var itens = await connection.QueryAsync<RevisaoFaceItem>(
            new CommandDefinition(sqlItens, new { tenantId, revisaoId }, cancellationToken: cancellationToken));

        revisao.Itens = itens.AsList();
        return revisao;
    }

    public async Task<ConfirmacaoRevisaoResultado> ConfirmarAsync(
        RevisaoConfirmacaoInput input,
        CancellationToken cancellationToken = default)
    {
        if (!PerfilPodeConfirmar(input.PerfilConfirmador))
        {
            return new ConfirmacaoRevisaoResultado
            {
                Status = ConfirmacaoRevisaoStatus.PerfilNaoPermitido,
                Mensagem = "Somente professor e coordenador podem confirmar revisão."
            };
        }

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string sqlRevisao = """
            SELECT id,
                   tenant_id AS TenantId,
                   turma AS Turma,
                   status AS Status,
                   expira_em AS ExpiraEm
            FROM revisoes_presenca_grupo
            WHERE id = @revisaoId
              AND tenant_id = @tenantId
            FOR UPDATE
            """;

        var revisao = await connection.QueryFirstOrDefaultAsync<RevisaoConfirmacaoContexto>(
            new CommandDefinition(
                sqlRevisao,
                new { revisaoId = input.RevisaoId, tenantId = input.TenantId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (revisao is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ConfirmacaoRevisaoResultado
            {
                Status = ConfirmacaoRevisaoStatus.NaoEncontrada,
                Mensagem = "Revisão não encontrada."
            };
        }

        if (!string.Equals(revisao.Status, RevisaoStatus.Pendente, StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ConfirmacaoRevisaoResultado
            {
                Status = ConfirmacaoRevisaoStatus.JaProcessada,
                Mensagem = "Revisão já foi processada."
            };
        }

        if (revisao.ExpiraEm < DateTime.UtcNow)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE revisoes_presenca_grupo
                    SET status = @statusExpirada
                    WHERE id = @revisaoId
                    """,
                    new { statusExpirada = RevisaoStatus.Expirada, revisaoId = input.RevisaoId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            await RegistrarAuditoriaAsync(
                connection,
                transaction,
                input.TenantId,
                input.RevisaoId,
                "revisao_expirada",
                "Confirmação recusada: revisão fora do prazo de 24h.",
                input.ConfirmadoPor,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ConfirmacaoRevisaoResultado
            {
                Status = ConfirmacaoRevisaoStatus.Expirada,
                Mensagem = "Revisão expirada."
            };
        }

        const string sqlAtualizarItem = """
            UPDATE revisao_faces_itens
            SET decisao = @decisao,
                pessoa_final_id = @pessoaFinalId,
                motivo = @motivo,
                decidido_por = @decididoPor,
                decidido_em = NOW()
            WHERE id = @itemId
              AND revisao_id = @revisaoId
              AND tenant_id = @tenantId
            """;

        foreach (var item in input.Itens)
        {
            var afetadas = await connection.ExecuteAsync(
                new CommandDefinition(
                    sqlAtualizarItem,
                    new
                    {
                        decisao = item.Decisao,
                        pessoaFinalId = item.PessoaFinalId,
                        motivo = item.Motivo,
                        decididoPor = input.ConfirmadoPor,
                        itemId = item.ItemId,
                        revisaoId = input.RevisaoId,
                        tenantId = input.TenantId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (afetadas == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ConfirmacaoRevisaoResultado
                {
                    Status = ConfirmacaoRevisaoStatus.ValidacaoInvalida,
                    Mensagem = $"Item {item.ItemId} não pertence à revisão informada."
                };
            }
        }

        const string sqlAprovados = """
            SELECT id,
                   COALESCE(pessoa_final_id, pessoa_sugerida_id) AS PessoaId,
                   pessoa_sugerida_id AS PessoaSugeridaId,
                   pessoa_final_id AS PessoaFinalId,
                   confianca AS Confianca,
                   embedding AS Embedding
            FROM revisao_faces_itens
            WHERE revisao_id = @revisaoId
              AND tenant_id = @tenantId
              AND decisao IN ('aprovada', 'reatribuida')
              AND COALESCE(pessoa_final_id, pessoa_sugerida_id) IS NOT NULL
            """;

        var aprovados = await connection.QueryAsync<RevisaoItemAprovado>(
            new CommandDefinition(
                sqlAprovados,
                new { revisaoId = input.RevisaoId, tenantId = input.TenantId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        const string sqlInserirPresenca = """
            INSERT INTO presencas (tenant_id, pessoa_id, data_hora, turma, data_dia, turma_normalizada)
            VALUES (@tenantId, @pessoaId, NOW(), @turma, CURRENT_DATE, @turmaNormalizada)
            ON CONFLICT (tenant_id, pessoa_id, turma_normalizada, data_dia) DO NOTHING
            RETURNING id
            """;

        var turmaNormalizada = NormalizarTurma(revisao.Turma);
        var presencasEfetivadas = 0;
        var duplicidadesIgnoradas = 0;

        foreach (var aprovado in aprovados)
        {
            var insertedId = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    sqlInserirPresenca,
                    new
                    {
                        tenantId = input.TenantId,
                        pessoaId = aprovado.PessoaId,
                        turma = revisao.Turma,
                        turmaNormalizada
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (insertedId.HasValue)
            {
                presencasEfetivadas++;
            }
            else
            {
                duplicidadesIgnoradas++;
            }

            var deveAtualizarEmbedding =
                aprovado.Embedding is not null &&
                aprovado.Confianca is not null &&
                aprovado.Confianca < LimiarAutoAceite &&
                (aprovado.PessoaFinalId is null || aprovado.PessoaFinalId == aprovado.PessoaSugeridaId);

            if (deveAtualizarEmbedding)
            {
                await AtualizarEmbeddingSupervisionadoAsync(
                    connection, transaction, input.TenantId, aprovado.PessoaId, aprovado.Embedding!, input.RevisaoId, cancellationToken);
            }
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE revisoes_presenca_grupo
                SET status = @statusConfirmada,
                    confirmado_por = @confirmadoPor,
                    confirmado_em = NOW()
                WHERE id = @revisaoId
                  AND tenant_id = @tenantId
                """,
                new
                {
                    statusConfirmada = RevisaoStatus.Confirmada,
                    confirmadoPor = input.ConfirmadoPor,
                    revisaoId = input.RevisaoId,
                    tenantId = input.TenantId
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await RegistrarAuditoriaAsync(
            connection,
            transaction,
            input.TenantId,
            input.RevisaoId,
            "revisao_confirmada",
            JsonSerializer.Serialize(new
            {
                presencasEfetivadas,
                duplicidadesIgnoradas
            }),
            input.ConfirmadoPor,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ConfirmacaoRevisaoResultado
        {
            Status = ConfirmacaoRevisaoStatus.Confirmada,
            Mensagem = "Revisão confirmada com sucesso.",
            PresencasEfetivadas = presencasEfetivadas
        };
    }

    private static bool PerfilPodeConfirmar(string? perfil) =>
        string.Equals(perfil, "professor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(perfil, "coordenador", StringComparison.OrdinalIgnoreCase);

    private static string NormalizarTurma(string? turma) => (turma ?? string.Empty).Trim().ToLowerInvariant();

    private static async Task RegistrarAuditoriaAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        int tenantId,
        int revisaoId,
        string evento,
        string? detalhes,
        string? ator,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO revisao_presenca_auditoria (tenant_id, revisao_id, evento, detalhes, ator, criado_em)
                VALUES (@tenantId, @revisaoId, @evento, @detalhes, @ator, NOW())
                """,
                new { tenantId, revisaoId, evento, detalhes, ator },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }

    private sealed class RevisaoConfirmacaoContexto
    {
        public string? Turma { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime ExpiraEm { get; init; }
    }

    private static async Task AtualizarEmbeddingSupervisionadoAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        int tenantId,
        int pessoaId,
        byte[] embeddingConfirmado,
        int revisaoId,
        CancellationToken cancellationToken)
    {
        var embeddingAtualBytes = await connection.QueryFirstOrDefaultAsync<byte[]?>(
            new CommandDefinition(
                "SELECT embedding FROM pessoas WHERE id = @pessoaId AND tenant_id = @tenantId",
                new { pessoaId, tenantId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (embeddingAtualBytes is null || embeddingAtualBytes.Length == 0)
        {
            return;
        }

        var embeddingAtual = EmbeddingHelper.FromBytes(embeddingAtualBytes);
        var embeddingNovo = EmbeddingHelper.FromBytes(embeddingConfirmado);

        if (embeddingAtual.Length != embeddingNovo.Length)
        {
            // Embeddings de dimensões diferentes (ex.: gerados por versões
            // diferentes do modelo de reconhecimento) — não mistura, evita corromper.
            return;
        }

        var embeddingBlend = new float[embeddingAtual.Length];
        for (var i = 0; i < embeddingBlend.Length; i++)
        {
            embeddingBlend[i] = (PesoEmbeddingAtual * embeddingAtual[i]) + (PesoEmbeddingNovo * embeddingNovo[i]);
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO pessoa_embeddings_historico (pessoa_id, embedding_anterior, revisao_id, motivo)
                VALUES (@pessoaId, @embeddingAnterior, @revisaoId, 'revisao_confirmada')
                """,
                new { pessoaId, embeddingAnterior = embeddingAtualBytes, revisaoId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE pessoas SET embedding = @embedding WHERE id = @pessoaId",
                new { embedding = EmbeddingHelper.ToBytes(embeddingBlend), pessoaId },
                transaction: transaction,
                cancellationToken: cancellationToken));
    }

    private sealed class RevisaoItemAprovado
    {
        public int PessoaId { get; init; }
        public int? PessoaSugeridaId { get; init; }
        public int? PessoaFinalId { get; init; }
        public float? Confianca { get; init; }
        public byte[]? Embedding { get; init; }
    }
}

public sealed class RevisaoCriacaoInput
{
    public int TenantId { get; init; }
    public string? Turma { get; init; }
    public long? ChatId { get; init; }
    public string CriadoPor { get; init; } = string.Empty;
    public DateTime ExpiraEm { get; init; }
    public string Origem { get; init; } = "telegram";
    public string ReferenciaArquivo { get; init; } = string.Empty;
    public IReadOnlyList<RevisaoCriacaoItemInput> Itens { get; init; } = [];
}

public sealed class RevisaoCriacaoItemInput
{
    public int? PessoaSugeridaId { get; init; }
    public string? NomeSugerido { get; init; }
    public float? Confianca { get; init; }
    public byte[]? Embedding { get; init; }
}

public sealed class RevisaoResumo
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string? Turma { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ExpiraEm { get; init; }
    public string CriadoPor { get; init; } = string.Empty;
    public DateTime CriadoEm { get; init; }
    public int TotalItens { get; init; }
    public int ItensPendentes { get; init; }
}

public sealed class RevisaoDetalhe : RevisaoPresencaGrupo
{
    public IReadOnlyList<RevisaoFaceItem> Itens { get; set; } = [];
}

public sealed class RevisaoConfirmacaoInput
{
    public int RevisaoId { get; init; }
    public int TenantId { get; init; }
    public string ConfirmadoPor { get; init; } = string.Empty;
    public string PerfilConfirmador { get; init; } = string.Empty;
    public IReadOnlyList<RevisaoConfirmacaoItemInput> Itens { get; init; } = [];
}

public sealed class RevisaoConfirmacaoItemInput
{
    public int ItemId { get; init; }
    public string Decisao { get; init; } = RevisaoDecisao.Pendente;
    public int? PessoaFinalId { get; init; }
    public string? Motivo { get; init; }
}

public enum ConfirmacaoRevisaoStatus
{
    Confirmada = 1,
    NaoEncontrada = 2,
    JaProcessada = 3,
    Expirada = 4,
    PerfilNaoPermitido = 5,
    ValidacaoInvalida = 6
}

public sealed class ConfirmacaoRevisaoResultado
{
    public ConfirmacaoRevisaoStatus Status { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public int PresencasEfetivadas { get; init; }
}

