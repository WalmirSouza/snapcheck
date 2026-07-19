using SnapCheck.Bot.Models;
using SnapCheck.Bot.Pipeline.Etapas;
using SnapCheck.Data.Models;
using SnapCheck.Data.Repositories;

namespace SnapCheck.Tests;

/// <summary>
/// Teste unitário puro (sem banco) do item 02.4 — usa um fake de
/// ITurmaRepository em vez de mock de biblioteca (decisão do item 10.1).
/// </summary>
public sealed class ValidarJanelaPresencaEtapaTests
{
    private static int DiaIsoDe(DateTime data) => ((int)data.DayOfWeek + 6) % 7 + 1;

    [Fact]
    public async Task ChatSemTurmaVinculada_RetornaSemTurmaVinculadaENaoSilencia()
    {
        var fake = new FakeTurmaRepository { TurmaId = null };
        var etapa = new ValidarJanelaPresencaEtapa(fake);
        var contexto = new PipelineContext { Mensagem = new MensagemProcessamento { TenantId = 1, ChatId = 999 } };

        await etapa.ExecutarAsync(contexto);

        Assert.Equal(StatusJanelaPresenca.SemTurmaVinculada, contexto.StatusJanela);
        Assert.False(string.IsNullOrWhiteSpace(contexto.MensagemJanela));
    }

    [Fact]
    public async Task TurmaVinculadaSemJanelaConfigurada_PermiteComoCompleta()
    {
        var fake = new FakeTurmaRepository { TurmaId = 10, Janelas = [] };
        var etapa = new ValidarJanelaPresencaEtapa(fake);
        var contexto = new PipelineContext { Mensagem = new MensagemProcessamento { TenantId = 1, ChatId = 999 } };

        await etapa.ExecutarAsync(contexto);

        Assert.Equal(StatusJanelaPresenca.Completa, contexto.StatusJanela);
    }

    [Fact]
    public async Task DentroDaJanelaEDaTolerancia_RetornaCompleta()
    {
        var agora = DateTime.Now;
        var janela = new TurmaJanela
        {
            TurmaId = 10,
            DiasSemana = [(short)DiaIsoDe(agora)],
            HoraInicio = agora.TimeOfDay,
            HoraFim = agora.TimeOfDay + TimeSpan.FromMinutes(10),
            ToleranciaAtrasoMinutos = 5,
            CortePresencaParcialPercentual = 100,
            Ativa = true
        };

        var fake = new FakeTurmaRepository { TurmaId = 10, Janelas = [janela] };
        var etapa = new ValidarJanelaPresencaEtapa(fake);
        var contexto = new PipelineContext { Mensagem = new MensagemProcessamento { TenantId = 1, ChatId = 999 } };

        await etapa.ExecutarAsync(contexto);

        Assert.Equal(StatusJanelaPresenca.Completa, contexto.StatusJanela);
    }

    [Fact]
    public async Task ForaDoDiaDaSemanaConfigurado_RetornaForaDaJanela()
    {
        var agora = DateTime.Now;
        var diaDiferente = (short)(DiaIsoDe(agora) % 7 + 1); // garante um dia ISO diferente de hoje

        var janela = new TurmaJanela
        {
            TurmaId = 10,
            DiasSemana = [diaDiferente],
            HoraInicio = agora.TimeOfDay,
            HoraFim = agora.TimeOfDay + TimeSpan.FromMinutes(10),
            ToleranciaAtrasoMinutos = 5,
            CortePresencaParcialPercentual = 100,
            Ativa = true
        };

        var fake = new FakeTurmaRepository { TurmaId = 10, Janelas = [janela] };
        var etapa = new ValidarJanelaPresencaEtapa(fake);
        var contexto = new PipelineContext { Mensagem = new MensagemProcessamento { TenantId = 1, ChatId = 999 } };

        await etapa.ExecutarAsync(contexto);

        Assert.Equal(StatusJanelaPresenca.ForaDaJanela, contexto.StatusJanela);
        Assert.False(string.IsNullOrWhiteSpace(contexto.MensagemJanela));
    }

    private sealed class FakeTurmaRepository : ITurmaRepository
    {
        public int? TurmaId { get; init; }
        public IReadOnlyList<TurmaJanela> Janelas { get; init; } = [];

        public Task<(CriarTurmaResultado Resultado, int? TurmaId)> CriarAsync(
            int tenantId, string nome, string codigoVinculacao, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AdicionarJanelaAsync(
            int turmaId, short[] diasSemana, TimeSpan horaInicio, TimeSpan horaFim,
            int toleranciaAtrasoMinutos, short cortePresencaParcialPercentual,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Turma>> ListarPorTenantAsync(int tenantId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<TurmaJanela>> ObterJanelasAtivasAsync(
            int turmaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Janelas);

        public Task<int?> ObterTurmaIdPorChatAsync(long chatId, CancellationToken cancellationToken = default) =>
            Task.FromResult(TurmaId);

        public Task<TurmaVinculacaoResultado> VincularChatAsync(
            int tenantId, long chatId, string codigoVinculacao, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
