using Dapper;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Tests;

/// <summary>
/// Cobre parte do critério de aceite do item 02.6: duplicidade pessoa+turma+dia
/// e presença manual auditada. Janela de horário (02.4) é coberta em
/// ValidarJanelaPresencaEtapaTests (unitário, não precisa de banco).
/// </summary>
[Collection("Postgres")]
public sealed class PresencaAntifraudeTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly IPessoaRepository _pessoaRepository = new PessoaRepository(fixture.ConnectionFactory);
    private readonly IPresencaRepository _presencaRepository = new PresencaRepository(fixture.ConnectionFactory);
    private int _tenantId;
    private int _pessoaId;

    public async Task InitializeAsync()
    {
        _tenantId = await CriarTenantAsync();
        _pessoaId = await _pessoaRepository.InserirAsync(_tenantId, "Pessoa Antifraude", [0x00]);
    }

    public async Task DisposeAsync()
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM presencas WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM pessoas WHERE tenant_id = @tenantId", new { tenantId = _tenantId });
        await connection.ExecuteAsync("DELETE FROM tenants WHERE id = @tenantId", new { tenantId = _tenantId });
    }

    [Fact]
    public async Task RegistrarDuasVezesMesmoDiaMesmaTurma_SegundaVezRetornaDuplicada()
    {
        var primeiro = await _presencaRepository.RegistrarAsync(_tenantId, _pessoaId, "Turma X");
        var segundo = await _presencaRepository.RegistrarAsync(_tenantId, _pessoaId, "Turma X");

        Assert.Equal(RegistroPresencaResultado.Registrada, primeiro);
        Assert.Equal(RegistroPresencaResultado.Duplicada, segundo);
    }

    [Fact]
    public async Task RegistrarEmTurmasDiferentes_MesmoDia_AmbasRegistram()
    {
        var turmaA = await _presencaRepository.RegistrarAsync(_tenantId, _pessoaId, "Turma A");
        var turmaB = await _presencaRepository.RegistrarAsync(_tenantId, _pessoaId, "Turma B");

        Assert.Equal(RegistroPresencaResultado.Registrada, turmaA);
        Assert.Equal(RegistroPresencaResultado.Registrada, turmaB);
    }

    [Fact]
    public async Task PresencaManual_RegistraComResponsavelEMotivo()
    {
        var resultado = await _presencaRepository.RegistrarManualAsync(
            _tenantId, _pessoaId, "Turma Manual", "Coordenador Teste", "Esqueceu de tirar foto");

        Assert.Equal(RegistroPresencaResultado.Registrada, resultado);

        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        var linha = await connection.QueryFirstAsync(
            "SELECT status_presenca, responsavel_manual, motivo_manual FROM presencas WHERE tenant_id = @tenantId AND pessoa_id = @pessoaId",
            new { tenantId = _tenantId, pessoaId = _pessoaId });

        Assert.Equal("manual", (string)linha.status_presenca);
        Assert.Equal("Coordenador Teste", (string)linha.responsavel_manual);
        Assert.Equal("Esqueceu de tirar foto", (string)linha.motivo_manual);
    }

    [Fact]
    public async Task PresencaManual_RespeitaIdempotenciaComRegistroAutomaticoExistente()
    {
        await _presencaRepository.RegistrarAsync(_tenantId, _pessoaId, "Turma Y");

        var resultadoManual = await _presencaRepository.RegistrarManualAsync(
            _tenantId, _pessoaId, "Turma Y", "Coordenador Teste", "Confirmação manual redundante");

        Assert.Equal(RegistroPresencaResultado.Duplicada, resultadoManual);
    }

    private async Task<int> CriarTenantAsync()
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO tenants (nome, codigo_ativacao, status) VALUES (@nome, @codigo, 'ativo') RETURNING id",
            new { nome = "Tenant Antifraude Teste", codigo = $"teste-antifraude-{Guid.NewGuid():N}" });
    }
}
