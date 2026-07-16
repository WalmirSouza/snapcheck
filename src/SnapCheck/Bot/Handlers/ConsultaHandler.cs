using SnapCheck.Bot.Services;
using SnapCheck.Data.Repositories;
using Telegram.Bot.Types;

namespace SnapCheck.Bot.Handlers;

public sealed class ConsultaHandler(
    IPessoaRepository pessoaRepository,
    IPresencaRepository presencaRepository,
    IActivityLog activityLog)
{
    public async Task<string?> HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var texto = update.Message?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (texto.StartsWith("/listar", StringComparison.OrdinalIgnoreCase))
        {
            var pessoas = await pessoaRepository.ListarAtivasAsync(cancellationToken);
            if (pessoas.Count == 0)
            {
                return "Nenhuma pessoa cadastrada.";
            }

            var nomes = string.Join("\n", pessoas.Select(p => $"• {p.Nome}"));
            return $"👥 *Pessoas cadastradas ({pessoas.Count}):*\n{nomes}";
        }

        if (texto.StartsWith("/frequencia", StringComparison.OrdinalIgnoreCase))
        {
            var nome = ExtrairArgumento(texto, "/frequencia");
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "Use: /frequencia Nome";
            }

            var pessoa = await pessoaRepository.ObterPorNomeAsync(nome, cancellationToken);
            if (pessoa is null)
            {
                return $"❌ Pessoa *{nome}* não encontrada.";
            }

            var presencas = await presencaRepository.ListarPorPessoaAsync(nome, cancellationToken);
            if (presencas.Count == 0)
            {
                return $"📋 *{nome}* ainda não possui presenças registradas.";
            }

            var linhas = presencas.Select(p =>
                $"• {p.DataHora.ToLocalTime():dd/MM/yyyy HH:mm}" +
                (string.IsNullOrWhiteSpace(p.Turma) ? "" : $" ({p.Turma})"));

            return $"📋 *Frequência de {nome}:*\n{string.Join("\n", linhas)}";
        }

        if (texto.StartsWith("/sumidos", StringComparison.OrdinalIgnoreCase))
        {
            var diasTexto = ExtrairArgumento(texto, "/sumidos");
            if (!int.TryParse(diasTexto, out var dias) || dias <= 0)
            {
                return "Use: /sumidos 7";
            }

            var sumidos = await pessoaRepository.ListarSumidosAsync(dias, cancellationToken);
            if (sumidos.Count == 0)
            {
                return $"✅ Todos apareceram nos últimos {dias} dias.";
            }

            var nomes = string.Join("\n", sumidos.Select(p => $"• {p.Nome}"));
            activityLog.Info($"Consulta /sumidos {dias}: {sumidos.Count} pessoa(s)");
            return $"😴 *Sumidos há {dias}+ dias ({sumidos.Count}):*\n{nomes}";
        }

        if (texto.StartsWith("/remover", StringComparison.OrdinalIgnoreCase))
        {
            var nome = ExtrairArgumento(texto, "/remover");
            if (string.IsNullOrWhiteSpace(nome))
            {
                return "Use: /remover Nome";
            }

            var removido = await pessoaRepository.RemoverPorNomeAsync(nome, cancellationToken);
            if (!removido)
            {
                return $"❌ Pessoa *{nome}* não encontrada.";
            }

            activityLog.Info($"Pessoa removida: {nome}");
            return $"🗑️ *{nome}* foi descadastrado(a).";
        }

        return null;
    }

    private static string ExtrairArgumento(string texto, string comando)
    {
        return texto.Length <= comando.Length
            ? string.Empty
            : texto[comando.Length..].Trim();
    }
}
