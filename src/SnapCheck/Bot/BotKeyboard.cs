using Telegram.Bot.Types.ReplyMarkups;

namespace SnapCheck.Bot;

public static class BotKeyboard
{
    public const string RegistrarPresenca = "📷 Registrar Presença";
    public const string Cadastrar = "👤 Cadastrar Pessoa";
    public const string Listar = "📋 Listar Pessoas";
    public const string Sumidos = "😴 Sumidos (7 dias)";
    public const string Ajuda = "ℹ️ Ajuda";

    public static readonly HashSet<string> BotoesMenu =
    [
        RegistrarPresenca,
        Cadastrar,
        Listar,
        Sumidos,
        Ajuda
    ];

    public static bool EhBotaoMenu(string texto) => BotoesMenu.Contains(texto);

    public static ReplyKeyboardMarkup MenuPrincipal { get; } = new(
    [
        [RegistrarPresenca, Cadastrar],
        [Listar, Sumidos],
        [Ajuda]
    ])
    {
        ResizeKeyboard = true,
        IsPersistent = true
    };

    public static string? ParaComando(string texto) => texto switch
    {
        RegistrarPresenca => null,
        Cadastrar => "/cadastrar",
        Listar => "/listar",
        Sumidos => "/sumidos 7",
        Ajuda => "/start",
        _ => texto
    };

    public const string InstrucaoEnviarFoto =
        """
        📸 *Envie a foto do grupo agora.*

        Toque no 📎 (clipe) → *Foto* → escolha ou tire a foto da turma.

        Eu identifico os rostos cadastrados, registro a presença e devolvo a imagem com os nomes.
        """;
}
