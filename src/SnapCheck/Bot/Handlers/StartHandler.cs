using SnapCheck.Bot.Services;
using Telegram.Bot.Types;

namespace SnapCheck.Bot.Handlers;

public sealed class StartHandler(IActivityLog activityLog)
{
    public Task<string> HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        activityLog.Info($"Comando /start recebido do chat {update.Message?.Chat.Id}");
        return Task.FromResult(
            """
            👋 Olá! Eu sou o *SnapCheck*.

            Use os *botões abaixo* para navegar:

            📷 *Registrar Presença* — manda foto do grupo
            👤 *Cadastrar Pessoa* — inclui alguém novo
            📋 *Listar Pessoas* — vê quem está cadastrado
            😴 *Sumidos* — quem não aparece há 7 dias

            Para registrar presença: toque em *Registrar Presença* e envie a foto da turma.
            """);
    }
}
