using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;

namespace Zalek360.Api.Controllers;

[ApiController]
[Route("api/estoque")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class EstoqueController : ControllerBase
{
    private readonly EstoqueService _service;
    public EstoqueController(EstoqueService service) => _service = service;

    [HttpGet("pendencias")]
    public Task<IReadOnlyList<PendenciaEstoquePedidoDto>> ListarPendencias(CancellationToken ct) => _service.ListarPendenciasAsync(ct);

    [HttpGet("materias-primas")]
    public Task<IReadOnlyList<MateriaPrimaDto>> ListarMaterias(CancellationToken ct) => _service.ListarMateriasAsync(ct);
    [HttpGet("materias-primas/{id:guid}/movimentos")]
    public Task<IReadOnlyList<MovimentoMateriaPrimaDto>> ListarMovimentos(Guid id, CancellationToken ct) => _service.ListarMovimentosAsync(id, ct);
    [HttpPost("materias-primas")]
    public async Task<ActionResult<MateriaPrimaDto>> CriarMateria([FromBody] MateriaPrimaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _service.CriarMateriaAsync(request, ct));
    [HttpPut("materias-primas/{id:guid}")]
    public Task<MateriaPrimaDto> AtualizarMateria(Guid id, [FromBody] MateriaPrimaRequest request, CancellationToken ct) => _service.AtualizarMateriaAsync(id, request, ct);
    [HttpPatch("materias-primas/{id:guid}/status")]
    public Task<MateriaPrimaDto> DefinirMateriaAtiva(Guid id, [FromBody] AtivoRequest request, CancellationToken ct) => _service.DefinirMateriaAtivaAsync(id, request.Ativo, ct);

    [HttpGet("fichas-tecnicas")]
    public Task<IReadOnlyList<FichaTecnicaDto>> ListarFichas(CancellationToken ct) => _service.ListarFichasAsync(ct);
    [HttpPut("fichas-tecnicas/{produtoId:guid}")]
    public Task<FichaTecnicaDto> SalvarFicha(Guid produtoId, [FromBody] FichaTecnicaRequest request, CancellationToken ct) =>
        _service.SalvarFichaAsync(request with { ProdutoId = produtoId }, ct);

    [HttpGet("compras")]
    public Task<IReadOnlyList<CompraMateriaPrimaDto>> ListarCompras(CancellationToken ct) => _service.ListarComprasAsync(ct);
    [HttpPost("compras")]
    public async Task<ActionResult<CompraMateriaPrimaDto>> CriarCompra([FromBody] CompraMateriaPrimaRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _service.CriarCompraAsync(request, ct));
    [HttpPatch("compras/{id:guid}/situacao")]
    public Task<CompraMateriaPrimaDto> AtualizarSituacaoCompra(Guid id, [FromBody] SituacaoCompraRequest request, CancellationToken ct) => _service.AtualizarSituacaoCompraAsync(id, request.Situacao, ct);
}

public sealed record AtivoRequest(bool Ativo);

[ApiController]
[Route("api/pedidos/{pedidoId:guid}/materiais")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class MateriaisPedidoController : ControllerBase
{
    private readonly EstoqueService _service;
    public MateriaisPedidoController(EstoqueService service) => _service = service;

    [HttpGet]
    public Task<MateriaisPedidoDto> Listar(Guid pedidoId, CancellationToken ct) => _service.ListarReservasPedidoAsync(pedidoId, ct);

    [HttpPost("ficha-tecnica")]
    public Task<MateriaisPedidoDto> AplicarFicha(Guid pedidoId, CancellationToken ct) => _service.AplicarFichaPedidoAsync(pedidoId, ct);

    [HttpPut]
    public Task<MateriaisPedidoDto> Atualizar(Guid pedidoId, [FromBody] IReadOnlyList<ReservaPedidoRequest> linhas, CancellationToken ct) =>
        _service.AtualizarReservasPedidoAsync(pedidoId, linhas, ct);
}
