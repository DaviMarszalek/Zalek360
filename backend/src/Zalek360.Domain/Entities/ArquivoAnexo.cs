using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

/// <summary>
/// Arte/referência anexada a um item. O upload acontece antes de salvar o orçamento (anexo "pendente",
/// sem item) e o vínculo é feito ao salvar. Na geração do pedido, cada anexo é copiado (novo registro,
/// mesma chave de armazenamento) para o item do pedido, mantendo rastreabilidade via AnexoOrigemId.
/// </summary>
public class ArquivoAnexo : Entidade
{
    public const long TamanhoMaximoBytes = 20L * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> TiposPermitidos = new Dictionary<string, string>
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".ai"] = "application/postscript",
        [".cdr"] = "application/vnd.corel-draw"
    };

    public Guid? ItemOrcamentoId { get; private set; }
    public Guid? ItemPedidoId { get; private set; }
    public Guid? AnexoOrigemId { get; private set; }
    public string NomeOriginal { get; private set; } = string.Empty;
    public string Extensao { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long TamanhoBytes { get; private set; }
    /// <summary>Identificador no provedor de armazenamento (local, S3, Azure Blob...). Nunca o nome enviado pelo usuário.</summary>
    public string ChaveArmazenamento { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public Guid? CriadoPorId { get; private set; }

    private ArquivoAnexo() { }

    public bool Pendente => ItemOrcamentoId is null && ItemPedidoId is null;

    public bool EhImagem => Extensao is ".png" or ".jpg" or ".jpeg";

    public static string ExtensaoDe(string nomeArquivo) => Path.GetExtension(nomeArquivo ?? string.Empty).ToLowerInvariant();

    /// <summary>Valida extensão e tamanho (PDF, PNG, JPG, AI, CDR até 20 MB, conforme a tela).</summary>
    public static void ValidarArquivo(string nomeArquivo, long tamanhoBytes)
    {
        var ext = ExtensaoDe(nomeArquivo);
        if (!TiposPermitidos.ContainsKey(ext))
            throw new DomainException(CodigosErro.AnexoTipoNaoPermitido,
                "Formato não aceito. Envie PDF, PNG, JPG, AI ou CDR.", CategoriaErro.Validacao);
        if (tamanhoBytes <= 0)
            throw new DomainException(CodigosErro.AnexoVazio, "O arquivo enviado está vazio.", CategoriaErro.Validacao);
        if (tamanhoBytes > TamanhoMaximoBytes)
            throw new DomainException(CodigosErro.AnexoTamanhoExcedido,
                "O arquivo ultrapassa o limite de 20 MB.", CategoriaErro.Validacao);
    }

    /// <summary>Confere a assinatura do arquivo (magic bytes) para evitar conteúdo disfarçado pela extensão.</summary>
    public static bool AssinaturaCompativel(string extensao, byte[] cabecalho)
    {
        static bool Comeca(byte[] dados, byte[] assinatura) =>
            dados.Length >= assinatura.Length && dados.AsSpan(0, assinatura.Length).SequenceEqual(assinatura);
        var pdf = "%PDF"u8.ToArray();
        var ps = "%!PS"u8.ToArray();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var jpg = new byte[] { 0xFF, 0xD8, 0xFF };
        var riff = "RIFF"u8.ToArray();
        var zip = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        return extensao switch
        {
            ".pdf" => Comeca(cabecalho, pdf),
            ".png" => Comeca(cabecalho, png),
            ".jpg" or ".jpeg" => Comeca(cabecalho, jpg),
            ".ai" => Comeca(cabecalho, pdf) || Comeca(cabecalho, ps),
            ".cdr" => Comeca(cabecalho, riff) || Comeca(cabecalho, zip),
            _ => false
        };
    }

    public static ArquivoAnexo CriarPendente(string nomeOriginal, long tamanhoBytes, string chaveArmazenamento, DateTime agoraUtc, Guid usuarioId)
    {
        ValidarArquivo(nomeOriginal, tamanhoBytes);
        var ext = ExtensaoDe(nomeOriginal);
        var nome = Path.GetFileName(nomeOriginal).Trim();
        if (nome.Length > 255) nome = nome[^255..];
        return new ArquivoAnexo
        {
            NomeOriginal = nome,
            Extensao = ext,
            ContentType = TiposPermitidos[ext],
            TamanhoBytes = tamanhoBytes,
            ChaveArmazenamento = chaveArmazenamento,
            CriadoEm = agoraUtc,
            CriadoPorId = usuarioId
        };
    }

    internal void VincularItemOrcamento(Guid itemOrcamentoId) => ItemOrcamentoId = itemOrcamentoId;

    internal ArquivoAnexo CopiarParaItemPedido(Guid itemPedidoId, DateTime agoraUtc) => new()
    {
        ItemPedidoId = itemPedidoId,
        AnexoOrigemId = Id,
        NomeOriginal = NomeOriginal,
        Extensao = Extensao,
        ContentType = ContentType,
        TamanhoBytes = TamanhoBytes,
        ChaveArmazenamento = ChaveArmazenamento,
        CriadoEm = agoraUtc,
        CriadoPorId = CriadoPorId
    };
}
