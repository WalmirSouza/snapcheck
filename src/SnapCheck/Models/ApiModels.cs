namespace SnapCheck.Web.Models;

public sealed class ConfiguracaoRequest
{
    public string TelegramToken { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class StatusResponse
{
    public bool BotOnline { get; set; }
    public bool BancoConectado { get; set; }
    public int TotalTenants { get; set; }
    public int TotalPessoas { get; set; }
    public int PresencasHoje { get; set; }
    public int FotosProcessadasHoje { get; set; }
    public int MensagensProcessadas { get; set; }
    public int Erros { get; set; }
    public string? Uptime { get; set; }
    public string? UltimaAtividade { get; set; }
    public IReadOnlyList<LogItem> Logs { get; set; } = [];
}

public sealed class LogItem
{
    public string Timestamp { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Nivel { get; set; } = "info";
}

public sealed class OperacaoResponse
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
