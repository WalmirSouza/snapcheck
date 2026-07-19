using Dapper;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Tests;

/// <summary>
/// Cobre o critério de aceite do item 01.7: uma consulta no contexto de um
/// tenant nunca retorna dado de outro, e nome de pessoa é único por tenant
/// (não globalmente).
/// </summary>
[Collection("Postgres")]
public sealed class TenantIsolationTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly IPessoaRepository _pessoaRepository = new PessoaRepository(fixture.ConnectionFactory);
    private int _tenantA;
    private int _tenantB;

    public async Task InitializeAsync()
    {
        _tenantA = await CriarTenantAsync("teste-isolamento-a");
        _tenantB = await CriarTenantAsync("teste-isolamento-b");
    }

    public async Task DisposeAsync()
    {
        await RemoverTenantAsync(_tenantA);
        await RemoverTenantAsync(_tenantB);
    }

    [Fact]
    public async Task ObterPorNome_NaoRetornaPessoaDeOutroTenant()
    {
        await _pessoaRepository.InserirAsync(_tenantA, "Joao Isolamento", [0x00]);
        await _pessoaRepository.InserirAsync(_tenantB, "Maria Isolamento", [0x00]);

        var resultadoNoTenantA = await _pessoaRepository.ObterPorNomeAsync(_tenantA, "Maria Isolamento");
        var resultadoNoTenantB = await _pessoaRepository.ObterPorNomeAsync(_tenantB, "Joao Isolamento");

        Assert.Null(resultadoNoTenantA);
        Assert.Null(resultadoNoTenantB);
    }

    [Fact]
    public async Task ListarAtivas_SoRetornaPessoasDoProprioTenant()
    {
        await _pessoaRepository.InserirAsync(_tenantA, "Pessoa Tenant A", [0x00]);
        await _pessoaRepository.InserirAsync(_tenantB, "Pessoa Tenant B", [0x00]);

        var pessoasTenantA = await _pessoaRepository.ListarAtivasAsync(_tenantA);

        Assert.Contains(pessoasTenantA, p => p.Nome == "Pessoa Tenant A");
        Assert.DoesNotContain(pessoasTenantA, p => p.Nome == "Pessoa Tenant B");
    }

    [Fact]
    public async Task MesmoNome_PermiteDoisTenantsDiferentes()
    {
        await _pessoaRepository.InserirAsync(_tenantA, "Nome Repetido", [0x00]);
        await _pessoaRepository.InserirAsync(_tenantB, "Nome Repetido", [0x00]);

        var pessoaA = await _pessoaRepository.ObterPorNomeAsync(_tenantA, "Nome Repetido");
        var pessoaB = await _pessoaRepository.ObterPorNomeAsync(_tenantB, "Nome Repetido");

        Assert.NotNull(pessoaA);
        Assert.NotNull(pessoaB);
    }

    [Fact]
    public async Task MesmoNome_MesmoTenant_LancaViolacaoDeConstraint()
    {
        await _pessoaRepository.InserirAsync(_tenantA, "Nome Duplicado", [0x00]);

        await Assert.ThrowsAsync<Npgsql.PostgresException>(
            () => _pessoaRepository.InserirAsync(_tenantA, "Nome Duplicado", [0x00]));
    }

    private async Task<int> CriarTenantAsync(string prefixo)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            "INSERT INTO tenants (nome, codigo_ativacao, status) VALUES (@nome, @codigo, 'ativo') RETURNING id",
            new { nome = $"Tenant {prefixo}", codigo = $"{prefixo}-{Guid.NewGuid():N}" });
    }

    private async Task RemoverTenantAsync(int tenantId)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await fixture.ConnectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM presencas WHERE tenant_id = @tenantId", new { tenantId });
        await connection.ExecuteAsync("DELETE FROM pessoas WHERE tenant_id = @tenantId", new { tenantId });
        await connection.ExecuteAsync("DELETE FROM tenant_chat_telegram WHERE tenant_id = @tenantId", new { tenantId });
        await connection.ExecuteAsync("DELETE FROM tenants WHERE id = @tenantId", new { tenantId });
    }
}
