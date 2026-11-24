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
[Authorize(Policy = Politicas.Operacao)]
public sealed class AnexosController : ControllerBase
{
    private readonly AnexoService _servico;

    public AnexosController(AnexoService servico) => _servico = servico;

    /// <summary>Envia um arquivo. O anexo fica pendente até ser vinculado a um item ao salvar o orçamento.</summary>
    [HttpPost]
    [Authorize(Policy = Politicas.Operacao)]
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
    [Authorize(Policy = Politicas.Operacao)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _servico.RemoverPendenteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Politicas.Operacao)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardQueries _consultas;
    public DashboardController(IDashboardQueries consultas) => _consultas = consultas;

    [HttpGet]
    public Task<DashboardDto> Obter(CancellationToken ct) => _consultas.ObterAsync(ct);
}

[ApiController]
[Route("api/produtos")]
[Authorize(Policy = Politicas.Operacao)]
public sealed class ProdutosController : ControllerBase
{
    private readonly ICatalogoQueries _consultas;
    private readonly ProdutoService _service;
    private readonly ConfiguracaoCatalogoService _configuracoes;
    public ProdutosController(ICatalogoQueries consultas, ProdutoService service, ConfiguracaoCatalogoService configuracoes)
    { _consultas = consultas; _service = service; _configuracoes = configuracoes; }

    [HttpGet]
    public Task<IReadOnlyList<ProdutoDto>> Listar(CancellationToken ct) => _consultas.ListarProdutosAsync(ct);

    [HttpPost]
    [Authorize(Policy = Politicas.Operacao)]
    public async Task<ActionResult<ProdutoDto>> Criar([FromBody] ProdutoRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _service.CriarAsync(request, ct));

    /// <summary>Tipos de personalização e condições de pagamento sugeridos no formulário.</summary>
    [HttpGet("opcoes")]
    public async Task<OpcoesOrcamentoDto> Opcoes(CancellationToken ct) => new(ArquivoAnexo.TiposPermitidos.Keys.ToList(), ArquivoAnexo.TamanhoMaximoBytes,
        await _configuracoes.NomesAsync(TipoOpcaoCatalogo.TipoPersonalizacao, ct), await _configuracoes.NomesAsync(TipoOpcaoCatalogo.CondicaoPagamento, ct));

    [HttpGet("catalogo")]
    public Task<IReadOnlyList<ProdutoDto>> Catalogo([FromQuery] string? busca, [FromQuery] bool incluirInativos, CancellationToken ct) => _service.ListarAsync(busca, incluirInativos, ct);
    [HttpGet("{id:guid}")]
    public Task<ProdutoDto> Obter(Guid id, CancellationToken ct) => _service.ObterAsync(id, ct);
    [HttpPut("{id:guid}")]
    public Task<ProdutoDto> Atualizar(Guid id, ProdutoRequest r, CancellationToken ct) => _service.AtualizarAsync(id, r, ct);
    [HttpPatch("{id:guid}/status")]
    public Task<ProdutoDto> Situacao(Guid id, SituacaoProdutoRequest r, CancellationToken ct) => _service.SituacaoAsync(id, r, ct);
    [HttpPost("{id:guid}/duplicar")]
    public async Task<ActionResult<ProdutoDto>> Duplicar(Guid id, DuplicarProdutoRequest r, CancellationToken ct) => StatusCode(201, await _service.DuplicarAsync(id, r, ct));
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Administracao)]
    public async Task<IActionResult> Remover(Guid id, [FromQuery] int? versao, CancellationToken ct)
    { await _service.RemoverAsync(id, versao, ct); return NoContent(); }
}

[ApiController]
[Route("api/usuarios")]
[Authorize(Policy = Politicas.Operacao)]
public sealed class UsuariosController : ControllerBase
{
    private readonly ICatalogoQueries _consultas;
    private readonly UsuarioService _usuarios;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        ICatalogoQueries consultas,
        UsuarioService usuarios,
        IUsuarioAtual usuarioAtual,
        ILogger<UsuariosController> logger)
    {
        _consultas = consultas;
        _usuarios = usuarios;
        _usuarioAtual = usuarioAtual;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = Politicas.Administracao)]
    public Task<IReadOnlyList<UsuarioAcessoDto>> Listar(CancellationToken ct) => _usuarios.ListarAsync(ct);

    [HttpPost]
    [Authorize(Policy = Politicas.Administracao)]
    [ProducesResponseType(typeof(UsuarioAcessoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAcessoDto>> CriarGestor([FromBody] CriarGestorRequest request, CancellationToken ct)
    {
        var usuario = await _usuarios.CriarGestorAsync(request, ct);
        _logger.LogInformation("Administrador {AdministradorId} criou a conta Gestor {GestorId}.",
            _usuarioAtual.Id, usuario.Id);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    /// <summary>Colaboradores que podem ser responsáveis por orçamentos.</summary>
    [HttpGet("responsaveis")]
    public Task<IReadOnlyList<UsuarioDto>> Responsaveis(CancellationToken ct) => _consultas.ListarResponsaveisAsync(ct);
}
