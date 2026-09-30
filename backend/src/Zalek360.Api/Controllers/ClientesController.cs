using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Enums;

namespace Zalek360.Api.Controllers;

/// <summary>UC01 — Cadastrar Cliente · UC05 — Consultar Histórico do Cliente.</summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Policy = Politicas.Comercial)]
public sealed class ClientesController : ControllerBase
{
    private readonly ClienteService _servico;
    private readonly IClienteQueries _consultas;

    public ClientesController(ClienteService servico, IClienteQueries consultas)
    {
        _servico = servico;
        _consultas = consultas;
    }

    /// <summary>Lista/pesquisa clientes por nome, razão social, CPF/CNPJ, telefone, WhatsApp ou e-mail.</summary>
    [HttpGet]
    public Task<PaginaResultado<ClienteListaItemDto>> Listar(
        [FromQuery] string? busca, [FromQuery] TipoPessoa? tipo, [FromQuery] string? cidade,
        [FromQuery] int? pagina, [FromQuery] int? tamanhoPagina, CancellationToken ct) =>
        _consultas.ListarAsync(new ClienteFiltro(busca, tipo, cidade, pagina, tamanhoPagina), ct);

    /// <summary>Cidades existentes no cadastro (filtro da lista).</summary>
    [HttpGet("cidades")]
    public Task<IReadOnlyList<string>> Cidades(CancellationToken ct) => _consultas.ListarCidadesAsync(ct);

    /// <summary>Valida CPF/CNPJ (dígitos verificadores) e verifica se já está cadastrado (RN2).</summary>
    [HttpGet("verificar-documento")]
    public Task<VerificacaoDocumentoDto> VerificarDocumento(
        [FromQuery] string? documento, [FromQuery] TipoPessoa? tipo, [FromQuery] Guid? excetoId, CancellationToken ct) =>
        _servico.VerificarDocumentoAsync(documento, tipo, excetoId, ct);

    /// <summary>Cadastra cliente PF/PJ. CPF/CNPJ duplicado retorna 409 com o cliente existente em details.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteDetalheDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ClienteDetalheDto>> Criar([FromBody] ClienteRequest request, CancellationToken ct)
    {
        var cliente = await _servico.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    [HttpGet("{id:guid}")]
    public Task<ClienteDetalheDto> Obter(Guid id, CancellationToken ct) => _consultas.ObterDetalheAsync(id, ct);

    /// <summary>Atualiza o cadastro (envie "versao" para controle de concorrência).</summary>
    [HttpPut("{id:guid}")]
    public Task<ClienteDetalheDto> Atualizar(Guid id, [FromBody] ClienteRequest request, CancellationToken ct) =>
        _servico.AtualizarAsync(id, request, ct);

    /// <summary>Aba "Visão geral": indicadores, último orçamento/pedido e últimas interações.</summary>
    [HttpGet("{id:guid}/visao-geral")]
    public Task<ClienteVisaoGeralDto> VisaoGeral(Guid id, CancellationToken ct) => _consultas.ObterVisaoGeralAsync(id, ct);

    [HttpGet("{id:guid}/orcamentos")]
    public Task<ListaFiltradaDto<OrcamentoResumoDto>> Orcamentos(
        Guid id, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] SituacaoOrcamento? status, CancellationToken ct) =>
        _consultas.ListarOrcamentosAsync(id, new FiltroOrcamentosCliente(de, ate, status), ct);

    [HttpGet("{id:guid}/pedidos")]
    public Task<ListaFiltradaDto<PedidoResumoDto>> Pedidos(
        Guid id, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] SituacaoPedido? status, CancellationToken ct) =>
        _consultas.ListarPedidosAsync(id, new FiltroPedidosCliente(de, ate, status), ct);

    /// <summary>UC05 — histórico consolidado. tipo: orcamentos | pedidos | contatos | decisoes.</summary>
    [HttpGet("{id:guid}/historico")]
    public Task<IReadOnlyList<HistoricoEventoDto>> Historico(
        Guid id, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, [FromQuery] string? tipo,
        [FromQuery] SituacaoOrcamento? statusOrcamento, [FromQuery] SituacaoPedido? statusPedido, [FromQuery] int? limite,
        CancellationToken ct) =>
        _consultas.ObterHistoricoAsync(id, new FiltroHistoricoCliente(de, ate, tipo, statusOrcamento, statusPedido, limite), ct);

    [HttpGet("{id:guid}/observacoes")]
    public Task<IReadOnlyList<ObservacaoClienteDto>> Observacoes(Guid id, CancellationToken ct) =>
        _consultas.ListarObservacoesAsync(id, ct);

    [HttpPost("{id:guid}/observacoes")]
    [ProducesResponseType(typeof(ObservacaoClienteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ObservacaoClienteDto>> AdicionarObservacao(Guid id, [FromBody] ObservacaoRequest request, CancellationToken ct)
    {
        var observacao = await _servico.AdicionarObservacaoAsync(id, request, ct);
        return StatusCode(StatusCodes.Status201Created, observacao);
    }
}
