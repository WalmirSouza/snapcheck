namespace SnapCheck.Bot.Services;

public sealed class ActivityLogEntry
{
    public DateTime Timestamp { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public string Nivel { get; init; } = "info";
}

public interface IActivityLog
{
    void Info(string mensagem);
    void Warn(string mensagem);
    void Error(string mensagem);
    IReadOnlyList<ActivityLogEntry> ObterUltimas(int quantidade = 10);
}

public sealed class ActivityLog : IActivityLog
{
    private readonly object _lock = new();
    private readonly LinkedList<ActivityLogEntry> _entries = new();
    private const int MaxEntries = 50;

    public void Info(string mensagem) => Add("info", mensagem);
    public void Warn(string mensagem) => Add("warn", mensagem);
    public void Error(string mensagem) => Add("error", mensagem);

    public IReadOnlyList<ActivityLogEntry> ObterUltimas(int quantidade = 10)
    {
        lock (_lock)
        {
            return _entries.Take(quantidade).ToList();
        }
    }

    private void Add(string nivel, string mensagem)
    {
        lock (_lock)
        {
            _entries.AddFirst(new ActivityLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Mensagem = mensagem,
                Nivel = nivel
            });

            while (_entries.Count > MaxEntries)
            {
                _entries.RemoveLast();
            }
        }
    }
}
