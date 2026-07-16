using System.Threading.Channels;
using SnapCheck.Bot.Models;

namespace SnapCheck.Bot.Services;

public interface IMessageChannel
{
    ChannelWriter<MensagemProcessamento> Writer { get; }
    ChannelReader<MensagemProcessamento> Reader { get; }
    ValueTask EnfileirarAsync(MensagemProcessamento mensagem, CancellationToken cancellationToken = default);
}

public sealed class MessageChannel : IMessageChannel
{
    private readonly Channel<MensagemProcessamento> _channel = Channel.CreateUnbounded<MensagemProcessamento>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelWriter<MensagemProcessamento> Writer => _channel.Writer;
    public ChannelReader<MensagemProcessamento> Reader => _channel.Reader;

    public ValueTask EnfileirarAsync(MensagemProcessamento mensagem, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(mensagem, cancellationToken);
}
