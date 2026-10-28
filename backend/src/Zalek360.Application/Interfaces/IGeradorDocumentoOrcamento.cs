using Zalek360.Application.Dtos;

namespace Zalek360.Application.Interfaces;

public interface IGeradorDocumentoOrcamento
{
    Task<byte[]> GerarAsync(OrcamentoDocumentoDto documento, CancellationToken ct);
}

public sealed record ArquivoDocumentoOrcamento(byte[] Conteudo, string NomeArquivo);
