using SnapCheck.Bot.Services;
using SnapCheck.Data.Repositories;
using SnapCheck.Data.Tenancy;

namespace SnapCheck.Bot.Handlers;

public sealed class VincularTurmaHandler(
    ITurmaRepository turmaRepository,
    ITenantContext tenantContext,
    IActivityLog activityLog)
{
    public async Task<string> HandleAsync(long chatId, string texto, CancellationToken cancellationToken = default)
    {
        var codigo = ExtrairArgumento(texto, "/turma");
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return "Use: /turma CODIGO";
        }

        var tenantId = tenantContext.TenantId!.Value;
        var resultado = await turmaRepository.VincularChatAsync(tenantId, chatId, codigo, cancellationToken);

        if (resultado == TurmaVinculacaoResultado.Sucesso)
        {
            activityLog.Info($"Chat {chatId} vinculado à turma via código de vinculação (tenant {tenantId}).");
        }

        return resultado switch
        {
            TurmaVinculacaoResultado.Sucesso =>
                "✅ Turma vinculada com sucesso! As presenças deste chat agora respeitam a janela de horário configurada.",
            TurmaVinculacaoResultado.ChatJaVinculado =>
                "⚠️ Este chat já está vinculado a uma turma.",
            TurmaVinculacaoResultado.CodigoInvalido =>
                "❌ Código de turma inválido.",
            _ => "❌ Não foi possível vincular a turma."
        };
    }

    private static string ExtrairArgumento(string texto, string comando) =>
        texto.Length <= comando.Length ? string.Empty : texto[comando.Length..].Trim();
}
