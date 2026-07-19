using Microsoft.Extensions.Logging;
using SnapCheck.Data.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Security;

/// <summary>
/// Enforcement layer temporária para permitir/negarl acesso às rotas
/// administrativas com base em um papel informado via header.
/// É um degrau intermediário até o login/RBAC completo do módulo 04.
/// </summary>
public sealed class AcessoPermissionMiddleware(RequestDelegate next, ILogger<AcessoPermissionMiddleware> logger)
{
    private static readonly string[] RotasPublicas = ["/api/acessos/login"];
    private const string CabecalhoTenant = "X-SnapCheck-Tenant-Id";
    private const string CabecalhoPapel = "X-SnapCheck-Role";
    private const string CabecalhoApiKey = "X-Admin-Api-Key";
    private const string CabecalhoAutorizacao = "Authorization";
    private const string PrefixoBearer = "Bearer ";

    private static readonly IReadOnlyDictionary<string, string[]> PermissoesPorPrefixoRota = new Dictionary<string, string[]>
    {
        ["/api/acessos"] = ["papel.read", "papel.write", "usuario.read", "usuario.write", "usuario.delete"],
        ["/api/turmas"] = ["turma.read", "turma.write", "turno.read", "turno.write"],
        ["/api/presencas"] = ["presenca.read", "presenca.write", "presenca.manual"],
        ["/api/revisoes-presenca"] = ["revisao.read", "revisao.write", "revisao.approve"],
        ["/api/bot"] = ["tenant.read", "tenant.write"]
    };

    public async Task InvokeAsync(HttpContext context, IAcessoRepository acessoRepository, IConfiguration configuration)
    {
        if (RotasPublicas.Any(rota => context.Request.Path.StartsWithSegments(rota)))
        {
            await next(context);
            return;
        }

        var rota = PermissoesPorPrefixoRota.Keys.FirstOrDefault(prefixo => context.Request.Path.StartsWithSegments(prefixo));
        if (rota is null)
        {
            await next(context);
            return;
        }

        var usuarioAutenticado = await TentarAutenticacaoBearerAsync(context, acessoRepository);
        if (usuarioAutenticado is not null)
        {
            var permissoesNecessariasDaRota = PermissoesPorPrefixoRota[rota];
            var permissoesDoUsuario = new HashSet<string>(
                usuarioAutenticado.PermissoesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                StringComparer.OrdinalIgnoreCase);

            if (!permissoesNecessariasDaRota.Any(permissao => permissoesDoUsuario.Contains(permissao)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Permissão insuficiente para esta operação.");
                return;
            }

            context.Items["SnapCheck.User"] = usuarioAutenticado;
            await next(context);
            return;
        }

        var chaveEsperada = configuration["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(chaveEsperada))
        {
            logger.LogError("Admin:ApiKey não configurada — enforcement administrativo bloqueado.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Configuração de segurança ausente. Defina Admin:ApiKey no ambiente.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(CabecalhoApiKey, out var chaveRecebida) ||
            chaveRecebida.Count != 1 ||
            chaveRecebida[0] != chaveEsperada)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Chave de API administrativa inválida ou ausente.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(CabecalhoPapel, out var papelHeader) || string.IsNullOrWhiteSpace(papelHeader.FirstOrDefault()))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Papel administrativo ausente.");
            return;
        }

        int? tenantId = null;
        if (context.Request.Headers.TryGetValue(CabecalhoTenant, out var tenantHeader) &&
            int.TryParse(tenantHeader.FirstOrDefault(), out var tenantParsed) &&
            tenantParsed > 0)
        {
            tenantId = tenantParsed;
        }

        var papel = papelHeader.First()!.Trim();
        var permissoesNecessarias = PermissoesPorPrefixoRota[rota];
        var papeis = await acessoRepository.ListarPapeisAsync(tenantId, context.RequestAborted);
        var papelEncontrado = papeis.FirstOrDefault(p => string.Equals(p.Codigo, papel, StringComparison.OrdinalIgnoreCase));

        if (papelEncontrado is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Papel não autorizado.");
            return;
        }

        var permissoesDoPapel = new HashSet<string>(
            papelEncontrado.PermissoesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        var autorizado = permissoesNecessarias.Any(permissao => permissoesDoPapel.Contains(permissao));
        if (!autorizado)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Permissão insuficiente para esta operação.");
            return;
        }

        await next(context);
    }

    private static async Task<UsuarioAutenticado?> TentarAutenticacaoBearerAsync(
        HttpContext context,
        IAcessoRepository acessoRepository)
    {
        if (!context.Request.Headers.TryGetValue(CabecalhoAutorizacao, out var authHeader))
        {
            return null;
        }

        var valor = authHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(valor) || !valor.StartsWith(PrefixoBearer, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = valor[PrefixoBearer.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await acessoRepository.ObterUsuarioPorTokenAsync(token, context.RequestAborted);
    }
}