using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;

namespace Zalek360.Application.Services;

public sealed class PedidoService
{
    public const string MensagemCriacaoDireta = "Pedido deve ser originado de um orçamento aprovado.";

    private readonly IPedidoRepository _pedidos;
    private readonly IPedidoQueries _consultas;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IUsuarioAtual _usuario;
    private readonly EstoqueService _estoque;

    public PedidoService(IPedidoRepository pedidos, IPedidoQueries consultas, IUnitOfWork uow, IClock clock, IUsuarioAtual usuario, EstoqueService estoque)
    {
        _pedidos = pedidos;
        _consultas = consultas;
        _uow = uow;
        _clock = clock;
        _usuario = usuario;
        _estoque = estoque;
    }

    /// <summary>RN9 / RF 4.1.3.2 — criação direta é sempre rejeitada (BDD-4, cenário 2).</summary>
    public static void RejeitarCriacaoDireta() =>
        throw new DomainException(CodigosErro.PedidoCriacaoDiretaNaoPermitida, MensagemCriacaoDireta);

    public async Task<PedidoDetalheDto> AtualizarSituacaoAsync(Guid id, AtualizarSituacaoPedidoRequest request, CancellationToken ct)
    {
        var pedido = await _pedidos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Pedido");
        Concorrencia.VerificarVersao(pedido.Versao, request.Versao);
        pedido.AtualizarSituacao(request.NovaSituacao, request.Observacao, _clock.AgoraUtc, _usuario.Id);
        if (pedido.Situacao == Zalek360.Domain.Enums.SituacaoPedido.EmProducao)
            await _estoque.ConsumirReservasPedidoAsync(id, ct);
        else if (pedido.Situacao == Zalek360.Domain.Enums.SituacaoPedido.Cancelado && request.NovaSituacao == Zalek360.Domain.Enums.SituacaoPedido.Cancelado)
            await _estoque.LiberarReservasPedidoAsync(id, ct);
        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(id, ct);
    }
}
