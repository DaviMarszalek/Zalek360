using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed record ArquivoParaDownload(Stream Conteudo, string ContentType, string NomeArquivo, bool PodeExibirInline);

/// <summary>Upload/download de artes e referências (PDF, PNG, JPG, AI, CDR até 20 MB).</summary>
public sealed class AnexoService
{
    private readonly IAnexoRepository _anexos;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IUsuarioAtual _usuario;

    public AnexoService(IAnexoRepository anexos, IFileStorage storage, IUnitOfWork uow, IClock clock, IUsuarioAtual usuario)
    {
        _anexos = anexos;
        _storage = storage;
        _uow = uow;
        _clock = clock;
        _usuario = usuario;
    }

    public static AnexoDto Mapear(ArquivoAnexo a) => new(a.Id, a.NomeOriginal, a.Extensao, a.ContentType, a.TamanhoBytes, a.EhImagem);

    public async Task<AnexoDto> EnviarAsync(Stream conteudo, string nomeArquivo, long tamanhoBytes, CancellationToken ct)
    {
        ArquivoAnexo.ValidarArquivo(nomeArquivo, tamanhoBytes);
        var extensao = ArquivoAnexo.ExtensaoDe(nomeArquivo);

        if (!conteudo.CanSeek)
            throw new InvalidOperationException("O fluxo do arquivo precisa permitir reposicionamento.");
        var cabecalho = new byte[8];
        var lidos = await conteudo.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, ct);
        if (!ArquivoAnexo.AssinaturaCompativel(extensao, cabecalho[..lidos]))
            throw new DomainException(CodigosErro.AnexoConteudoInvalido,
                "O conteúdo do arquivo não corresponde ao formato informado.", CategoriaErro.Validacao);
        conteudo.Seek(0, SeekOrigin.Begin);

        var chave = await _storage.SalvarAsync(conteudo, extensao, ct);
        try
        {
            var anexo = ArquivoAnexo.CriarPendente(nomeArquivo, tamanhoBytes, chave, _clock.AgoraUtc, _usuario.Id);
            _anexos.Adicionar(anexo);
            await _uow.SaveChangesAsync(ct);
            return Mapear(anexo);
        }
        catch
        {
            await _storage.ExcluirAsync(chave, CancellationToken.None);
            throw;
        }
    }

    public async Task<ArquivoParaDownload> AbrirAsync(Guid id, CancellationToken ct)
    {
        var anexo = await _anexos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Arquivo");
        if (anexo.Pendente && anexo.CriadoPorId != _usuario.Id) throw new NaoEncontradoException("Arquivo");
        Stream stream;
        try
        {
            stream = await _storage.AbrirLeituraAsync(anexo.ChaveArmazenamento, ct);
        }
        catch (FileNotFoundException)
        {
            throw new AppException(CodigosErro.AnexoIndisponivel, "O arquivo não está mais disponível no armazenamento.", 410);
        }
        return new ArquivoParaDownload(stream, anexo.ContentType, anexo.NomeOriginal, anexo.EhImagem || anexo.Extensao == ".pdf");
    }

    /// <summary>Remove um upload ainda não vinculado a nenhum item (o usuário desistiu do arquivo no formulário).</summary>
    public async Task RemoverPendenteAsync(Guid id, CancellationToken ct)
    {
        var anexo = await _anexos.ObterPorIdAsync(id, ct) ?? throw new NaoEncontradoException("Arquivo");
        if (!anexo.Pendente || anexo.CriadoPorId != _usuario.Id)
            throw new ConflitoException(CodigosErro.AnexoIndisponivel,
                "Este arquivo já está vinculado a um orçamento. Remova-o editando o item.");
        _anexos.Remover(anexo);
        await _uow.SaveChangesAsync(ct);
        await _storage.ExcluirAsync(anexo.ChaveArmazenamento, ct);
    }

    /// <summary>Limpeza de uploads órfãos (enviados e nunca vinculados, ou desvinculados na edição).</summary>
    public async Task<int> LimparPendentesAsync(TimeSpan idadeMinima, CancellationToken ct)
    {
        var limite = _clock.AgoraUtc - idadeMinima;
        var pendentes = await _anexos.ListarPendentesCriadosAntesDeAsync(limite, ct);
        var chavesParaExcluir = new List<string>();
        foreach (var anexo in pendentes)
        {
            if (!await _anexos.ChaveUsadaPorOutroAnexoAsync(anexo.ChaveArmazenamento, anexo.Id, ct))
                chavesParaExcluir.Add(anexo.ChaveArmazenamento);
            _anexos.Remover(anexo);
        }
        await _uow.SaveChangesAsync(ct);
        foreach (var chave in chavesParaExcluir) await _storage.ExcluirAsync(chave, ct);
        return pendentes.Count;
    }
}
