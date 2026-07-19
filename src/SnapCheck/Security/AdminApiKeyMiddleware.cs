using Microsoft.Extensions.Logging;

namespace SnapCheck.Security;

/// <summary>
/// Autenticação mínima e temporária para as rotas administrativas de /api/bot
/// (ADR 0001, Decisão 2). Fail-closed de propósito: sem "Admin:ApiKey"
/// configurada no ambiente, as rotas ficam bloqueadas em vez de abertas.
/// Não usada pelo fluxo do bot no Telegram — lá a identidade é resolvida por
/// tenant_chat_telegram (ver ITenantContext/ITenantRepository).
/// Este middleware é para ser substituído (não estendido) pelo RBAC completo
/// do módulo 04.
/// </summary>
public sealed class AdminApiKeyMiddleware(RequestDelegate next, ILogger<AdminApiKeyMiddleware> logger)
{
    private static readonly string[] RotasProtegidas = ["/api/bot", "/api/revisoes-presenca"];
    private const string CabecalhoChave = "X-Admin-Api-Key";

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (!RotasProtegidas.Any(rota => context.Request.Path.StartsWithSegments(rota)))
        {
            await next(context);
            return;
        }

        var chaveEsperada = configuration["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(chaveEsperada))
        {
            logger.LogError("Admin:ApiKey não configurada — rotas administrativas permanecem bloqueadas.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Configuração de segurança ausente. Defina Admin:ApiKey no ambiente.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(CabecalhoChave, out var chaveRecebida) ||
            chaveRecebida.Count != 1 ||
            chaveRecebida[0] != chaveEsperada)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Chave de API administrativa inválida ou ausente.");
            return;
        }

        await next(context);
    }
}
