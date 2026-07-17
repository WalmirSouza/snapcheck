using SnapCheck.Bot.Services;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Bot.Handlers;

public sealed class VincularHandler(ITenantRepository tenantRepository, IActivityLog activityLog)
{
    public async Task<string> HandleAsync(long chatId, string texto, CancellationToken cancellationToken = default)
    {
        var codigo = ExtrairArgumento(texto, "/vincular");
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return "Use: /vincular CODIGO";
        }

        var resultado = await tenantRepository.VincularChatAsync(chatId, codigo, cancellationToken);

        if (resultado == TenantVinculacaoResultado.Sucesso)
        {
            activityLog.Info($"Chat {chatId} vinculado via código de ativação.");
        }

        return resultado switch
        {
            TenantVinculacaoResultado.Sucesso =>
                "✅ Chat vinculado com sucesso! Use /start para ver as opções.",
            TenantVinculacaoResultado.ChatJaVinculado =>
                "⚠️ Este chat já está vinculado a um cliente.",
            TenantVinculacaoResultado.CodigoInvalido =>
                "❌ Código de ativação inválido.",
            _ => "❌ Não foi possível vincular o chat."
        };
    }

    private static string ExtrairArgumento(string texto, string comando) =>
        texto.Length <= comando.Length ? string.Empty : texto[comando.Length..].Trim();
}
