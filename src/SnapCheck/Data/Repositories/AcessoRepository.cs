using Dapper;
using SnapCheck.Data.Models;
using SnapCheck.Data.Security;

namespace SnapCheck.Data.Repositories;

public enum LoginAcessoResultado
{
    Sucesso,
    UsuarioNaoEncontrado,
    SenhaInvalida,
    UsuarioInativo
}

public enum CriarUsuarioAcessoResultado
{
    Sucesso,
    EmailJaExisteNoEscopo
}

public enum CriarPapelAcessoResultado
{
    Sucesso,
    CodigoJaExisteNoEscopo
}

public enum VincularPapelUsuarioResultado
{
    Sucesso,
    UsuarioNaoEncontrado,
    PapelNaoEncontrado,
    JaVinculado
}

public enum ConcederPermissaoPapelResultado
{
    Sucesso,
    PapelNaoEncontrado,
    PermissaoNaoEncontrada,
    JaConcedida
}

public interface IAcessoRepository
{
    Task<IReadOnlyList<UsuarioAcesso>> ListarUsuariosAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<(CriarUsuarioAcessoResultado Resultado, int? UsuarioId)> CriarUsuarioAsync(
        int? tenantId,
        string nome,
        string email,
        string? senha = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PapelAcesso>> ListarPapeisAsync(int? tenantId, CancellationToken cancellationToken = default);
    Task<(CriarPapelAcessoResultado Resultado, int? PapelId)> CriarPapelAsync(
        int? tenantId,
        string codigo,
        string nome,
        string? descricao,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissaoAcesso>> ListarPermissoesAsync(CancellationToken cancellationToken = default);
    Task<VincularPapelUsuarioResultado> VincularPapelAoUsuarioAsync(
        int usuarioId,
        int papelId,
        CancellationToken cancellationToken = default);
    Task<ConcederPermissaoPapelResultado> ConcederPermissaoAoPapelAsync(
        int papelId,
        string codigoPermissao,
        CancellationToken cancellationToken = default);

    Task<(LoginAcessoResultado Resultado, UsuarioAutenticado? Usuario, string? Token, DateTime? ExpiraEm)> AutenticarAsync(
        int? tenantId,
        string email,
        string senha,
        CancellationToken cancellationToken = default);

    Task<UsuarioAutenticado?> ObterUsuarioPorTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> InvalidarTokenAsync(
        string token,
        CancellationToken cancellationToken = default);
}

public sealed class AcessoRepository(IDbConnectionFactory connectionFactory) : IAcessoRepository
{
    public async Task<IReadOnlyList<UsuarioAcesso>> ListarUsuariosAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id,
                   u.tenant_id AS TenantId,
                   u.nome,
                   u.email,
                   u.ativo AS Ativo,
                   COALESCE(STRING_AGG(DISTINCT p.nome, ', ' ORDER BY p.nome), '') AS PapeisCsv
            FROM usuarios u
            LEFT JOIN usuario_papeis up ON up.usuario_id = u.id
            LEFT JOIN papeis p ON p.id = up.papel_id AND p.ativo = TRUE
            WHERE (@tenantId IS NULL OR u.tenant_id IS NULL OR u.tenant_id = @tenantId)
            GROUP BY u.id, u.tenant_id, u.nome, u.email, u.ativo
            ORDER BY u.nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<UsuarioAcesso>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<(CriarUsuarioAcessoResultado Resultado, int? UsuarioId)> CriarUsuarioAsync(
        int? tenantId,
        string nome,
        string email,
        string? senha = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existente = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM usuarios WHERE ((tenant_id IS NULL AND @tenantId IS NULL) OR tenant_id = @tenantId) AND LOWER(email) = LOWER(@email) AND ativo = TRUE LIMIT 1",
                new { tenantId, email },
                cancellationToken: cancellationToken));

        if (existente is not null)
        {
            return (CriarUsuarioAcessoResultado.EmailJaExisteNoEscopo, null);
        }

        string? senhaSalt = null;
        string? senhaHash = null;
        if (!string.IsNullOrWhiteSpace(senha))
        {
            (senhaSalt, senhaHash) = AcessoCrypto.GerarHashSenha(senha);
        }

        const string sql = """
            INSERT INTO usuarios (tenant_id, nome, email, senha_salt, senha_hash, ativo)
            VALUES (@tenantId, @nome, @email, @senhaSalt, @senhaHash, TRUE)
            RETURNING id
            """;

        var usuarioId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { tenantId, nome, email, senhaSalt, senhaHash }, cancellationToken: cancellationToken));

        return (CriarUsuarioAcessoResultado.Sucesso, usuarioId);
    }

    public async Task<IReadOnlyList<PapelAcesso>> ListarPapeisAsync(int? tenantId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.id,
                   p.tenant_id AS TenantId,
                   p.codigo,
                   p.nome,
                   p.descricao,
                   p.ativo AS Ativo,
                   COALESCE(STRING_AGG(DISTINCT pr.codigo, ', ' ORDER BY pr.codigo), '') AS PermissoesCsv
            FROM papeis p
            LEFT JOIN papel_permissoes pp ON pp.papel_id = p.id
            LEFT JOIN permissoes pr ON pr.id = pp.permissao_id
            WHERE (@tenantId IS NULL OR p.tenant_id IS NULL OR p.tenant_id = @tenantId)
            GROUP BY p.id, p.tenant_id, p.codigo, p.nome, p.descricao, p.ativo
            ORDER BY p.nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<PapelAcesso>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<(CriarPapelAcessoResultado Resultado, int? PapelId)> CriarPapelAsync(
        int? tenantId,
        string codigo,
        string nome,
        string? descricao,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existente = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM papeis WHERE ((tenant_id IS NULL AND @tenantId IS NULL) OR tenant_id = @tenantId) AND LOWER(codigo) = LOWER(@codigo) AND ativo = TRUE LIMIT 1",
                new { tenantId, codigo },
                cancellationToken: cancellationToken));

        if (existente is not null)
        {
            return (CriarPapelAcessoResultado.CodigoJaExisteNoEscopo, null);
        }

        const string sql = """
            INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
            VALUES (@tenantId, @codigo, @nome, @descricao, TRUE)
            RETURNING id
            """;

        var papelId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { tenantId, codigo, nome, descricao }, cancellationToken: cancellationToken));

        return (CriarPapelAcessoResultado.Sucesso, papelId);
    }

    public async Task<IReadOnlyList<PermissaoAcesso>> ListarPermissoesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, codigo, nome, descricao
            FROM permissoes
            ORDER BY nome
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.QueryAsync<PermissaoAcesso>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return result.AsList();
    }

    public async Task<VincularPapelUsuarioResultado> VincularPapelAoUsuarioAsync(
        int usuarioId,
        int papelId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var usuarioExiste = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("SELECT id FROM usuarios WHERE id = @usuarioId AND ativo = TRUE LIMIT 1", new { usuarioId }, cancellationToken: cancellationToken));
        if (usuarioExiste is null)
        {
            return VincularPapelUsuarioResultado.UsuarioNaoEncontrado;
        }

        var papelExiste = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("SELECT id FROM papeis WHERE id = @papelId AND ativo = TRUE LIMIT 1", new { papelId }, cancellationToken: cancellationToken));
        if (papelExiste is null)
        {
            return VincularPapelUsuarioResultado.PapelNaoEncontrado;
        }

        var jaVinculado = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM usuario_papeis WHERE usuario_id = @usuarioId AND papel_id = @papelId LIMIT 1",
                new { usuarioId, papelId },
                cancellationToken: cancellationToken));

        if (jaVinculado is not null)
        {
            return VincularPapelUsuarioResultado.JaVinculado;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO usuario_papeis (usuario_id, papel_id) VALUES (@usuarioId, @papelId)",
                new { usuarioId, papelId },
                cancellationToken: cancellationToken));

        return VincularPapelUsuarioResultado.Sucesso;
    }

    public async Task<ConcederPermissaoPapelResultado> ConcederPermissaoAoPapelAsync(
        int papelId,
        string codigoPermissao,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);

        var papelExiste = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("SELECT id FROM papeis WHERE id = @papelId AND ativo = TRUE LIMIT 1", new { papelId }, cancellationToken: cancellationToken));
        if (papelExiste is null)
        {
            return ConcederPermissaoPapelResultado.PapelNaoEncontrado;
        }

        var permissaoId = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition("SELECT id FROM permissoes WHERE LOWER(codigo) = LOWER(@codigoPermissao) LIMIT 1", new { codigoPermissao }, cancellationToken: cancellationToken));
        if (permissaoId is null)
        {
            return ConcederPermissaoPapelResultado.PermissaoNaoEncontrada;
        }

        var jaConcedida = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT id FROM papel_permissoes WHERE papel_id = @papelId AND permissao_id = @permissaoId LIMIT 1",
                new { papelId, permissaoId },
                cancellationToken: cancellationToken));

        if (jaConcedida is not null)
        {
            return ConcederPermissaoPapelResultado.JaConcedida;
        }

        await connection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO papel_permissoes (papel_id, permissao_id) VALUES (@papelId, @permissaoId)",
                new { papelId, permissaoId },
                cancellationToken: cancellationToken));

        return ConcederPermissaoPapelResultado.Sucesso;
    }

    public async Task<(LoginAcessoResultado Resultado, UsuarioAutenticado? Usuario, string? Token, DateTime? ExpiraEm)> AutenticarAsync(
        int? tenantId,
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id,
                   u.tenant_id AS TenantId,
                   u.nome,
                   u.email,
                   u.senha_salt AS SenhaSalt,
                   u.senha_hash AS SenhaHash,
                   u.auth_token_hash AS AuthTokenHash,
                   u.auth_token_expira_em AS AuthTokenExpiraEm,
                   u.ativo AS Ativo,
                   COALESCE(STRING_AGG(DISTINCT p.nome, ', ' ORDER BY p.nome), '') AS PapeisCsv,
                   COALESCE(STRING_AGG(DISTINCT pr.codigo, ', ' ORDER BY pr.codigo), '') AS PermissoesCsv
            FROM usuarios u
            LEFT JOIN usuario_papeis up ON up.usuario_id = u.id
            LEFT JOIN papeis p ON p.id = up.papel_id AND p.ativo = TRUE
            LEFT JOIN papel_permissoes pp ON pp.papel_id = p.id
            LEFT JOIN permissoes pr ON pr.id = pp.permissao_id
            WHERE u.ativo = TRUE
              AND LOWER(u.email) = LOWER(@email)
              AND (@tenantId IS NULL OR u.tenant_id IS NULL OR u.tenant_id = @tenantId)
            GROUP BY u.id, u.tenant_id, u.nome, u.email, u.senha_salt, u.senha_hash, u.auth_token_hash, u.auth_token_expira_em, u.ativo
            LIMIT 1
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioAcessoComSegredos>(
            new CommandDefinition(sql, new { tenantId, email }, cancellationToken: cancellationToken));

        if (usuario is null)
        {
            return (LoginAcessoResultado.UsuarioNaoEncontrado, null, null, null);
        }

        if (!usuario.Ativo)
        {
            return (LoginAcessoResultado.UsuarioInativo, null, null, null);
        }

        if (string.IsNullOrWhiteSpace(usuario.SenhaSalt) || string.IsNullOrWhiteSpace(usuario.SenhaHash) ||
            !AcessoCrypto.VerificarSenha(senha, usuario.SenhaSalt, usuario.SenhaHash))
        {
            return (LoginAcessoResultado.SenhaInvalida, null, null, null);
        }

        var (token, tokenHash) = AcessoCrypto.GerarTokenSeguro();
        var expiraEm = DateTime.UtcNow.AddHours(8);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE usuarios
                SET auth_token_hash = @tokenHash,
                    auth_token_expira_em = @expiraEm,
                    ultimo_login_em = NOW()
                WHERE id = @usuarioId
                """,
                new { tokenHash, expiraEm, usuarioId = usuario.Id },
                cancellationToken: cancellationToken));

        return (
            LoginAcessoResultado.Sucesso,
            new UsuarioAutenticado
            {
                Id = usuario.Id,
                TenantId = usuario.TenantId,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Ativo = usuario.Ativo,
                PapeisCsv = usuario.PapeisCsv,
                PermissoesCsv = usuario.PermissoesCsv,
                AuthTokenExpiraEm = expiraEm
            },
            token,
            expiraEm);
    }

    public async Task<UsuarioAutenticado?> ObterUsuarioPorTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id,
                   u.tenant_id AS TenantId,
                   u.nome,
                   u.email,
                   u.ativo AS Ativo,
                   u.auth_token_expira_em AS AuthTokenExpiraEm,
                   COALESCE(STRING_AGG(DISTINCT p.nome, ', ' ORDER BY p.nome), '') AS PapeisCsv,
                   COALESCE(STRING_AGG(DISTINCT pr.codigo, ', ' ORDER BY pr.codigo), '') AS PermissoesCsv
            FROM usuarios u
            LEFT JOIN usuario_papeis up ON up.usuario_id = u.id
            LEFT JOIN papeis p ON p.id = up.papel_id AND p.ativo = TRUE
            LEFT JOIN papel_permissoes pp ON pp.papel_id = p.id
            LEFT JOIN permissoes pr ON pr.id = pp.permissao_id
            WHERE u.ativo = TRUE
              AND u.auth_token_expira_em > NOW()
              AND u.auth_token_hash = @tokenHash
            GROUP BY u.id, u.tenant_id, u.nome, u.email, u.ativo, u.auth_token_expira_em
            LIMIT 1
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioAutenticado>(
            new CommandDefinition(sql, new { tokenHash = AcessoCrypto.HashToken(token) }, cancellationToken: cancellationToken));

        return usuario;
    }

    public async Task<bool> InvalidarTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuarios
            SET auth_token_hash = NULL,
                auth_token_expira_em = NULL
            WHERE auth_token_hash = @tokenHash
            """;

        await using var connection = (Npgsql.NpgsqlConnection)await connectionFactory.CreateConnectionAsync(cancellationToken);
        var linhasAfetadas = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { tokenHash = AcessoCrypto.HashToken(token) }, cancellationToken: cancellationToken));

        return linhasAfetadas > 0;
    }
}

internal sealed class UsuarioAcessoComSegredos
{
    public int Id { get; set; }
    public int? TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SenhaSalt { get; set; }
    public string? SenhaHash { get; set; }
    public string? AuthTokenHash { get; set; }
    public DateTime? AuthTokenExpiraEm { get; set; }
    public bool Ativo { get; set; }
    public string PapeisCsv { get; set; } = string.Empty;
    public string PermissoesCsv { get; set; } = string.Empty;
}