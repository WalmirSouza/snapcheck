using SnapCheck.Bot.Models;
using SnapCheck.Bot.Pipeline.Etapas;
using SnapCheck.Data.Repositories;
using SnapCheck.Face.Models;

namespace SnapCheck.Tests;

/// <summary>
/// Cobre o gatilho de revisão unificado (ADR 0005, Decisão 1) com fakes — não
/// precisa de banco, só verifica se CriarAsync é chamado ou não conforme a
/// combinação de quantidade de rostos e confiança.
/// </summary>
public sealed class RegistrarPresencaEtapaTests
{
    [Fact]
    public async Task UmRostoComConfiancaNaFaixaIncerta_CriaRevisao()
    {
        var revisaoFake = new FakeRevisaoRepository();
        var etapa = new RegistrarPresencaEtapa(new FakePresencaRepository(), revisaoFake);
        var contexto = CriarContexto([NaoReconhecido(similaridade: 0.35f)]);

        await etapa.ExecutarAsync(contexto);

        Assert.True(revisaoFake.Chamado);
        Assert.True(contexto.RegistroPorRevisao);
    }

    [Fact]
    public async Task UmRostoComConfiancaMuitoBaixa_NaoCriaRevisaoNemRegistraPresenca()
    {
        var revisaoFake = new FakeRevisaoRepository();
        var presencaFake = new FakePresencaRepository();
        var etapa = new RegistrarPresencaEtapa(presencaFake, revisaoFake);
        var contexto = CriarContexto([NaoReconhecido(similaridade: 0.10f)]);

        await etapa.ExecutarAsync(contexto);

        Assert.False(revisaoFake.Chamado);
        Assert.False(contexto.RegistroPorRevisao);
        Assert.Empty(presencaFake.Chamadas);
    }

    [Fact]
    public async Task MultiplosRostos_CriaRevisaoIndependenteDaConfianca()
    {
        var revisaoFake = new FakeRevisaoRepository();
        var etapa = new RegistrarPresencaEtapa(new FakePresencaRepository(), revisaoFake);
        var contexto = CriarContexto([Reconhecido(pessoaId: 1, similaridade: 0.95f), NaoReconhecido(similaridade: 0.10f)]);

        await etapa.ExecutarAsync(contexto);

        Assert.True(revisaoFake.Chamado);
    }

    [Fact]
    public async Task UmRostoReconhecidoComAltaConfianca_RegistraPresencaDireto()
    {
        var revisaoFake = new FakeRevisaoRepository();
        var presencaFake = new FakePresencaRepository();
        var etapa = new RegistrarPresencaEtapa(presencaFake, revisaoFake);
        var contexto = CriarContexto([Reconhecido(pessoaId: 42, similaridade: 0.95f)]);

        await etapa.ExecutarAsync(contexto);

        Assert.False(revisaoFake.Chamado);
        Assert.Single(presencaFake.Chamadas);
        Assert.Equal(42, presencaFake.Chamadas[0]);
    }

    private static PipelineContext CriarContexto(IReadOnlyList<FaceMatch> matches) => new()
    {
        Mensagem = new MensagemProcessamento { TenantId = 1, ChatId = 999, FileId = "file1", Turma = "Turma X" },
        Matches = matches
    };

    private static FaceMatch Reconhecido(int pessoaId, float similaridade) => new()
    {
        Face = new FaceResult { Embedding = [1f, 2f, 3f] },
        PessoaId = pessoaId,
        Nome = $"Pessoa {pessoaId}",
        Similaridade = similaridade
    };

    private static FaceMatch NaoReconhecido(float similaridade) => new()
    {
        Face = new FaceResult { Embedding = [1f, 2f, 3f] },
        Similaridade = similaridade
    };

    private sealed class FakePresencaRepository : IPresencaRepository
    {
        public List<int> Chamadas { get; } = [];

        public Task<RegistroPresencaResultado> RegistrarAsync(
            int tenantId, int pessoaId, string? turma, string statusPresenca = "completa",
            CancellationToken cancellationToken = default)
        {
            Chamadas.Add(pessoaId);
            return Task.FromResult(RegistroPresencaResultado.Registrada);
        }

        public Task<RegistroPresencaResultado> RegistrarManualAsync(
            int tenantId, int pessoaId, string? turma, string responsavel, string motivo,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<SnapCheck.Data.Models.Presenca>> ListarPorPessoaAsync(
            int tenantId, string nome, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> ContarHojeAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeRevisaoRepository : IRevisaoPresencaRepository
    {
        public bool Chamado { get; private set; }

        public Task<int> CriarAsync(RevisaoCriacaoInput input, CancellationToken cancellationToken = default)
        {
            Chamado = true;
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<RevisaoResumo>> ListarPendentesAsync(
            int tenantId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<RevisaoDetalhe?> ObterDetalheAsync(
            int tenantId, int revisaoId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ConfirmacaoRevisaoResultado> ConfirmarAsync(
            RevisaoConfirmacaoInput input, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
