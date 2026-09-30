using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;

namespace Zalek360.Api.Controllers;

/// <summary>Pedidos — sempre originados de orçamentos aprovados (RN8/RN9).</summary>
[ApiController]
[Route("api/pedidos")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class PedidosController : ControllerBase
{
    private readonly PedidoService _servico;
    private readonly IPedidoQueries _consultas;

    public PedidosController(PedidoService servico, IPedidoQueries consultas)
    {
        _servico = servico;
        _consultas = consultas;
    }

    [HttpGet]
    public Task<ListaComContagemDto<PedidoListaItemDto>> Listar(
        [FromQuery] string? busca, [FromQuery] SituacaoPedido? status, [FromQuery] Guid? responsavelId,
        [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] int? pagina, [FromQuery] int? tamanhoPagina, CancellationToken ct) =>
        _consultas.ListarAsync(new PedidoFiltro(busca, status, responsavelId, de, ate, pagina, tamanhoPagina), ct);

    /// <summary>Criação manual NÃO é permitida: sempre retorna 422 PEDIDO_CRIACAO_DIRETA_NAO_PERMITIDA.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult CriarDiretamente()
    {
        PedidoService.RejeitarCriacaoDireta();
        return StatusCode(StatusCodes.Status422UnprocessableEntity);
    }

    [HttpGet("{id:guid}")]
    public Task<PedidoDetalheDto> Obter(Guid id, CancellationToken ct) => _consultas.ObterDetalheAsync(id, ct);

    [HttpGet("{id:guid}/historico")]
    public Task<IReadOnlyList<HistoricoEventoDto>> Historico(Guid id, CancellationToken ct) => _consultas.ObterHistoricoAsync(id, ct);

    /// <summary>Evolui o status (Aberto → Em produção → Pronto → Entregue) ou cancela (motivo obrigatório).</summary>
    [HttpPatch("{id:guid}/status")]
    public Task<PedidoDetalheDto> AtualizarStatus(Guid id, [FromBody] AtualizarSituacaoPedidoRequest request, CancellationToken ct) =>
        _servico.AtualizarSituacaoAsync(id, request, ct);
}
