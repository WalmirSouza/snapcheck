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

public sealed class CriarRevisaoPresencaRequest
{
    public int TenantId { get; set; }
    public string? Turma { get; set; }
    public long? ChatId { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public string Origem { get; set; } = "telegram";
    public string ReferenciaArquivo { get; set; } = string.Empty;
    public List<CriarRevisaoPresencaItemRequest> Itens { get; set; } = [];
}

public sealed class CriarRevisaoPresencaItemRequest
{
    public int? PessoaSugeridaId { get; set; }
    public string? NomeSugerido { get; set; }
    public float? Confianca { get; set; }
}

public sealed class ConfirmarRevisaoPresencaRequest
{
    public int TenantId { get; set; }
    public string ConfirmadoPor { get; set; } = string.Empty;
    public string PerfilConfirmador { get; set; } = string.Empty;
    public List<ConfirmarRevisaoPresencaItemRequest> Itens { get; set; } = [];
}

public sealed class ConfirmarRevisaoPresencaItemRequest
{
    public int ItemId { get; set; }
    public string Decisao { get; set; } = string.Empty;
    public int? PessoaFinalId { get; set; }
    public string? Motivo { get; set; }
}

public sealed class CriarRevisaoPresencaResponse
{
    public int RevisaoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiraEm { get; set; }
    public int TotalItens { get; set; }
}

public sealed class ConfirmarRevisaoPresencaResponse
{
    public bool Sucesso { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public int PresencasEfetivadas { get; set; }
}

public sealed class CriarTurmaRequest
{
    public int TenantId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CodigoVinculacao { get; set; } = string.Empty;
}

public sealed class CriarTurmaResponse
{
    public bool Sucesso { get; set; }
    public int? TurmaId { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public sealed class AdicionarJanelaRequest
{
    public int TenantId { get; set; }
    public short[] DiasSemana { get; set; } = [];
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFim { get; set; } = string.Empty;
    public int ToleranciaAtrasoMinutos { get; set; }
    public short CortePresencaParcialPercentual { get; set; } = 100;
}

public sealed class TurmaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CodigoVinculacao { get; set; } = string.Empty;
    public bool Ativa { get; set; }
}
