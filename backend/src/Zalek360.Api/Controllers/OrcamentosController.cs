using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Api.Controllers;

/// <summary>UC02 — Criar Orçamento · UC03 — Registrar Decisão · UC04 — Converter em Pedido.</summary>
[ApiController]
[Route("api/orcamentos")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class OrcamentosController : ControllerBase
{
    private readonly OrcamentoService _servico;
    private readonly IOrcamentoQueries _consultas;

    public OrcamentosController(OrcamentoService servico, IOrcamentoQueries consultas)
    {
        _servico = servico;
        _consultas = consultas;
    }

    /// <summary>Lista com filtros e contagem por status (abas).</summary>
    [HttpGet]
    public Task<ListaComContagemDto<OrcamentoListaItemDto>> Listar(
        [FromQuery] string? busca, [FromQuery] SituacaoOrcamento? status, [FromQuery] Guid? responsavelId, [FromQuery] Guid? clienteId,
        [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] int? pagina, [FromQuery] int? tamanhoPagina, CancellationToken ct) =>
        _consultas.ListarAsync(new OrcamentoFiltro(busca, status, responsavelId, clienteId, de, ate, pagina, tamanhoPagina), ct);

    /// <summary>Cria orçamento (status inicial Em Elaboração; totais sempre recalculados no backend).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrcamentoDetalheDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrcamentoDetalheDto>> Criar([FromBody] OrcamentoRequest request, CancellationToken ct)
    {
        var orcamento = await _servico.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = orcamento.Id }, orcamento);
    }

    /// <summary>Pré-visualização do resumo financeiro (não grava).</summary>
    [HttpPost("calculo")]
    public ActionResult<CalculoDto> Calcular([FromBody] CalculoRequest request) => OrcamentoService.Calcular(request);

    [HttpGet("{id:guid}")]
    public Task<OrcamentoDetalheDto> Obter(Guid id, CancellationToken ct) => _consultas.ObterDetalheAsync(id, ct);

    /// <summary>Edita orçamento em Em Elaboração/Aguardando Retorno (RN6). Envie "versao".</summary>
    [HttpPut("{id:guid}")]
    public Task<OrcamentoDetalheDto> Atualizar(Guid id, [FromBody] OrcamentoRequest request, CancellationToken ct) =>
        _servico.AtualizarAsync(id, request, ct);

    [HttpGet("{id:guid}/itens")]
    public Task<IReadOnlyList<ItemDetalheDto>> Itens(Guid id, CancellationToken ct) => _consultas.ListarItensAsync(id, ct);

    /// <summary>Linha do tempo. categoria: alteracoes | contatos.</summary>
    [HttpGet("{id:guid}/historico")]
    public Task<IReadOnlyList<HistoricoEventoDto>> Historico(Guid id, [FromQuery] string? categoria, CancellationToken ct) =>
        _consultas.ObterHistoricoAsync(id, categoria, ct);

    [HttpGet("{id:guid}/contatos")]
    public Task<IReadOnlyList<ContatoDto>> Contatos(Guid id, CancellationToken ct) => _consultas.ListarContatosAsync(id, ct);

    /// <summary>Registra contato feito fora do sistema. Não altera o status do orçamento.</summary>
    [HttpPost("{id:guid}/contatos")]
    [ProducesResponseType(typeof(ContatoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ContatoDto>> RegistrarContato(Guid id, [FromBody] ContatoRequest request, CancellationToken ct)
    {
        var contato = await _servico.RegistrarContatoAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, contato);
    }

    /// <summary>Marca que a proposta foi apresentada ao cliente (Em Elaboração → Aguardando Retorno).</summary>
    [HttpPost("{id:guid}/aguardando-retorno")]
    public Task<OrcamentoDetalheDto> MarcarAguardandoRetorno(Guid id, [FromBody] AguardandoRetornoRequest request, CancellationToken ct) =>
        _servico.MarcarAguardandoRetornoAsync(id, request, ct);

    /// <summary>
    /// Registra a decisão informada pelo cliente (meio de contato e data/hora obrigatórios — RN7).
    /// Aprovado gera o pedido automaticamente na mesma transação (UC04); falha em qualquer etapa desfaz tudo.
    /// </summary>
    [HttpPost("{id:guid}/decisao")]
    public Task<DecisaoResultadoDto> RegistrarDecisao(Guid id, [FromBody] DecisaoRequest request, CancellationToken ct) =>
        _servico.RegistrarDecisaoAsync(id, request, ct);

    /// <summary>Motivos sugeridos para recusa/cancelamento.</summary>
    [HttpGet("motivos-decisao")]
    public IReadOnlyList<string> MotivosDecisao() => DecisaoOrcamento.MotivosSugeridos;
}
