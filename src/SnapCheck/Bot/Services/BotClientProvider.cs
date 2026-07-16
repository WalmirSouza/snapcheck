using Telegram.Bot;

namespace SnapCheck.Bot.Services;

public interface IBotClientProvider
{
    ITelegramBotClient? Client { get; }
    void SetClient(ITelegramBotClient? client);
}

public sealed class BotClientProvider : IBotClientProvider
{
    public ITelegramBotClient? Client { get; private set; }

    public void SetClient(ITelegramBotClient? client) => Client = client;
}
