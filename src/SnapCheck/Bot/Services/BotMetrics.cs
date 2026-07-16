namespace SnapCheck.Bot.Services;

public sealed class BotMetrics
{
    public bool Online { get; set; }
    public DateTime? IniciadoEm { get; set; }
    public int MensagensProcessadas { get; set; }
    public int Erros { get; set; }
    public int FotosProcessadasHoje { get; set; }
    public DateTime? UltimaAtividade { get; set; }
    public string? UltimoErro { get; set; }

    public TimeSpan? Uptime => IniciadoEm.HasValue && Online
        ? DateTime.UtcNow - IniciadoEm.Value
        : null;
}

public interface IBotMetricsService
{
    BotMetrics Metrics { get; }
    void SetOnline(bool online);
    void RegistrarMensagemProcessada();
    void RegistrarFotoProcessada();
    void RegistrarErro(string mensagem);
    void ResetContadoresDiarios();
}

public sealed class BotMetricsService : IBotMetricsService
{
    private readonly object _lock = new();
    private DateTime _ultimoReset = DateTime.UtcNow.Date;

    public BotMetrics Metrics { get; } = new();

    public void SetOnline(bool online)
    {
        lock (_lock)
        {
            Metrics.Online = online;
            if (online)
            {
                Metrics.IniciadoEm = DateTime.UtcNow;
            }
            else
            {
                Metrics.IniciadoEm = null;
            }
        }
    }

    public void RegistrarMensagemProcessada()
    {
        lock (_lock)
        {
            VerificarResetDiario();
            Metrics.MensagensProcessadas++;
            Metrics.UltimaAtividade = DateTime.UtcNow;
        }
    }

    public void RegistrarFotoProcessada()
    {
        lock (_lock)
        {
            VerificarResetDiario();
            Metrics.FotosProcessadasHoje++;
            Metrics.UltimaAtividade = DateTime.UtcNow;
        }
    }

    public void RegistrarErro(string mensagem)
    {
        lock (_lock)
        {
            Metrics.Erros++;
            Metrics.UltimoErro = mensagem;
            Metrics.UltimaAtividade = DateTime.UtcNow;
        }
    }

    public void ResetContadoresDiarios()
    {
        lock (_lock)
        {
            Metrics.FotosProcessadasHoje = 0;
            _ultimoReset = DateTime.UtcNow.Date;
        }
    }

    private void VerificarResetDiario()
    {
        if (DateTime.UtcNow.Date > _ultimoReset)
        {
            Metrics.FotosProcessadasHoje = 0;
            _ultimoReset = DateTime.UtcNow.Date;
        }
    }
}
