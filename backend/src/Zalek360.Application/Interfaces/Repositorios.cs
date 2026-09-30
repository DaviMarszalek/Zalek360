using Zalek360.Domain.Entities;

namespace Zalek360.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct);
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct);
}

public interface IClienteRepository
{
    Task<Cliente?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct);
    Task<bool> ExisteAsync(Guid id, CancellationToken ct);
    void Adicionar(Cliente cliente);
    void AdicionarObservacao(ObservacaoCliente observacao);
}

public interface IProdutoRepository
{
    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

public interface IOrcamentoRepository
{
    /// <summary>Carrega o agregado rastreado com itens, anexos e decisão.</summary>
    Task<Orcamento?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Orcamento>> ListarParaExpiracaoAsync(DateOnly hojeLocal, CancellationToken ct);
    void Adicionar(Orcamento orcamento);
}

public interface IPedidoRepository
{
    Task<Pedido?> ObterParaAlteracaoAsync(Guid id, CancellationToken ct);
    void Adicionar(Pedido pedido);
}

public interface IAnexoRepository
{
    Task<ArquivoAnexo?> ObterPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ArquivoAnexo>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyList<ArquivoAnexo>> ListarPendentesCriadosAntesDeAsync(DateTime limiteUtc, CancellationToken ct);
    Task<bool> ChaveUsadaPorOutroAnexoAsync(string chave, Guid excetoId, CancellationToken ct);
    void Adicionar(ArquivoAnexo anexo);
    void Remover(ArquivoAnexo anexo);
}
