using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;

namespace Zalek360.Api.Controllers;

/// <summary>Artes e referências anexadas aos itens (PDF, PNG, JPG, AI, CDR — até 20 MB).</summary>
[ApiController]
[Route("api/anexos")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class AnexosController : ControllerBase
{
    private readonly AnexoService _servico;

    public AnexosController(AnexoService servico) => _servico = servico;

    /// <summary>Envia um arquivo. O anexo fica pendente até ser vinculado a um item ao salvar o orçamento.</summary>
    [HttpPost]
    [RequestSizeLimit(25L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25L * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AnexoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnexoDto>> Enviar(IFormFile? arquivo, CancellationToken ct)
    {
        if (arquivo is null)
            throw new Zalek360.Domain.Common.DomainException(Zalek360.Domain.Common.CodigosErro.AnexoVazio,
                "Selecione um arquivo para enviar.", Zalek360.Domain.Common.CategoriaErro.Validacao);
        await using var stream = arquivo.OpenReadStream();
        var anexo = await _servico.EnviarAsync(stream, arquivo.FileName, arquivo.Length, ct);
        return StatusCode(StatusCodes.Status201Created, anexo);
    }

    /// <summary>Baixa (ou exibe, com inline=true) o arquivo.</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] bool inline, CancellationToken ct)
    {
        var arquivo = await _servico.AbrirAsync(id, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (inline && arquivo.PodeExibirInline)
        {
            Response.Headers.ContentDisposition = $"inline; filename*=UTF-8''{Uri.EscapeDataString(arquivo.NomeArquivo)}";
            return File(arquivo.Conteudo, arquivo.ContentType, enableRangeProcessing: true);
        }
        return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeArquivo, enableRangeProcessing: true);
    }

    /// <summary>Remove um upload ainda não vinculado.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _servico.RemoverPendenteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardQueries _consultas;
    public DashboardController(IDashboardQueries consultas) => _consultas = consultas;

    [HttpGet]
    public Task<DashboardDto> Obter(CancellationToken ct) => _consultas.ObterAsync(ct);
}

[ApiController]
[Route("api/produtos")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class ProdutosController : ControllerBase
{
    private readonly ICatalogoQueries _consultas;
    public ProdutosController(ICatalogoQueries consultas) => _consultas = consultas;

    [HttpGet]
    public Task<IReadOnlyList<ProdutoDto>> Listar(CancellationToken ct) => _consultas.ListarProdutosAsync(ct);

    /// <summary>Tipos de personalização e condições de pagamento sugeridos no formulário.</summary>
    [HttpGet("opcoes")]
    public OpcoesOrcamentoDto Opcoes() => new(ArquivoAnexo.TiposPermitidos.Keys.ToList(), ArquivoAnexo.TamanhoMaximoBytes,
        OpcoesOrcamento.TiposPersonalizacao, OpcoesOrcamento.CondicoesPagamento);
}

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class UsuariosController : ControllerBase
{
    private readonly ICatalogoQueries _consultas;
    public UsuariosController(ICatalogoQueries consultas) => _consultas = consultas;

    /// <summary>Colaboradores que podem ser responsáveis por orçamentos.</summary>
    [HttpGet("responsaveis")]
    public Task<IReadOnlyList<UsuarioDto>> Responsaveis(CancellationToken ct) => _consultas.ListarResponsaveisAsync(ct);
}
