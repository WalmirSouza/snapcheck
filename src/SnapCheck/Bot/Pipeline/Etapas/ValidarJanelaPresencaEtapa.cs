using SnapCheck.Bot.Models;
using SnapCheck.Data.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Bot.Pipeline.Etapas;

/// <summary>
/// Calcula se o horário do recebimento da foto conta como presença válida,
/// conforme a(s) janela(s) da Turma vinculada ao chat (ADR 0002). Roda antes de
/// RegistrarPresencaEtapa — não decide "quem é" (isso é CompararRostosEtapa/
/// revisão em grupo do item 02.8), só "o horário conta".
/// </summary>
public sealed class ValidarJanelaPresencaEtapa(ITurmaRepository turmaRepository) : IPipelineEtapa
{
    public string Nome => "ValidarJanelaPresenca";

    public async Task ExecutarAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        var turmaId = await turmaRepository.ObterTurmaIdPorChatAsync(context.Mensagem.ChatId, cancellationToken);
        if (turmaId is null)
        {
            context.StatusJanela = StatusJanelaPresenca.SemTurmaVinculada;
            context.MensagemJanela =
                "🔒 Este chat ainda não está vinculado a uma turma. Use */turma CODIGO* para vincular.";
            return;
        }

        var janelas = await turmaRepository.ObterJanelasAtivasAsync(turmaId.Value, cancellationToken);
        if (janelas.Count == 0)
        {
            // Turma vinculada mas sem janela configurada ainda: default sensato é
            // permitir sem restrição (compatível com o fluxo atual), conforme
            // critério de aceite do item 02.4.
            context.StatusJanela = StatusJanelaPresenca.Completa;
            return;
        }

        var agora = DateTime.Now;
        var diaIso = ((int)agora.DayOfWeek + 6) % 7 + 1; // DayOfWeek: Dom=0..Sáb=6 -> ISO: Seg=1..Dom=7
        var horaAtual = agora.TimeOfDay;

        var janelaVigente = janelas.FirstOrDefault(j =>
            j.DiasSemana.Contains((short)diaIso) &&
            horaAtual >= j.HoraInicio &&
            horaAtual <= j.HoraFim);

        if (janelaVigente is null)
        {
            context.StatusJanela = StatusJanelaPresenca.ForaDaJanela;
            context.MensagemJanela = "⏰ Fora do horário da turma — presença não registrada.";
            return;
        }

        context.StatusJanela = CalcularStatus(janelaVigente, horaAtual);
    }

    private static StatusJanelaPresenca CalcularStatus(TurmaJanela janela, TimeSpan horaAtual)
    {
        var decorrido = horaAtual - janela.HoraInicio;
        var duracaoTotal = janela.HoraFim - janela.HoraInicio;
        var tolerancia = TimeSpan.FromMinutes(janela.ToleranciaAtrasoMinutos);
        var corte = duracaoTotal * (janela.CortePresencaParcialPercentual / 100.0);

        if (decorrido <= tolerancia)
        {
            return StatusJanelaPresenca.Completa;
        }

        return decorrido <= corte ? StatusJanelaPresenca.Atrasado : StatusJanelaPresenca.Parcial;
    }
}
