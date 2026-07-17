using System.Collections.Concurrent;
using SnapCheck.Bot.Services;
using SnapCheck.Data;
using SnapCheck.Data.Repositories;
using SnapCheck.Data.Tenancy;
using SnapCheck.Face;
using Telegram.Bot.Types;

namespace SnapCheck.Bot.Handlers;

public enum CadastroEtapa
{
    AguardandoNome,
    AguardandoFoto
}

public sealed class CadastroState
{
    public CadastroEtapa Etapa { get; set; }
    public string? Nome { get; set; }
}

public sealed class CadastroHandler(
    IFaceService faceService,
    IPessoaRepository pessoaRepository,
    ITenantContext tenantContext,
    IActivityLog activityLog)
{
    private readonly ConcurrentDictionary<long, CadastroState> _estados = new();

    public async Task<string?> HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = update.Message!.Chat.Id;
        var texto = update.Message.Text?.Trim() ?? string.Empty;

        if (texto.Equals("/cadastrar", StringComparison.OrdinalIgnoreCase))
        {
            _estados[chatId] = new CadastroState { Etapa = CadastroEtapa.AguardandoNome };
            return "📝 Informe o *nome* da pessoa para cadastro:";
        }

        if (!_estados.TryGetValue(chatId, out var estado))
        {
            return null;
        }

        if (estado.Etapa == CadastroEtapa.AguardandoNome)
        {
            if (BotKeyboard.EhBotaoMenu(texto))
            {
                return "Por favor, digite o *nome* da pessoa (texto), não use os botões do menu.";
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return "Por favor, envie um nome válido.";
            }

            var existente = await pessoaRepository.ObterPorNomeAsync(tenantContext.TenantId!.Value, texto, cancellationToken);
            if (existente is not null)
            {
                _estados.TryRemove(chatId, out _);
                return $"⚠️ A pessoa *{texto}* já está cadastrada.";
            }

            estado.Nome = texto;
            estado.Etapa = CadastroEtapa.AguardandoFoto;
            return $"📸 Agora envie uma *foto de rosto* de *{texto}*:";
        }

        return null;
    }

    public async Task<string?> HandleFotoAsync(
        Update update,
        byte[] fotoBytes,
        CancellationToken cancellationToken = default)
    {
        var chatId = update.Message!.Chat.Id;

        if (!_estados.TryGetValue(chatId, out var estado) || estado.Etapa != CadastroEtapa.AguardandoFoto)
        {
            return null;
        }

        var embedding = faceService.ExtrairEmbedding(fotoBytes);
        if (embedding.Length == 0)
        {
            return "❌ Não consegui detectar um rosto na foto. Tente novamente com uma imagem mais nítida.";
        }

        var id = await pessoaRepository.InserirAsync(
            tenantContext.TenantId!.Value,
            estado.Nome!,
            EmbeddingHelper.ToBytes(embedding),
            cancellationToken);

        _estados.TryRemove(chatId, out _);
        activityLog.Info($"Pessoa cadastrada: {estado.Nome} (id {id})");

        return $"✅ *{estado.Nome}* cadastrado(a) com sucesso!";
    }

    public bool EstaEmCadastro(long chatId) =>
        _estados.TryGetValue(chatId, out var estado) && estado.Etapa == CadastroEtapa.AguardandoFoto;
}
