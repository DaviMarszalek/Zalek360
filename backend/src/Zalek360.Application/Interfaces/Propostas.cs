using Zalek360.Application.Dtos;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Interfaces;

public interface IPropostaRepository
{
    Task<IReadOnlyList<ModeloMensagemProposta>> ModelosAsync(CancellationToken ct);
    Task<ModeloMensagemProposta?> ModeloAsync(Guid id, CancellationToken ct);
    Task<EnvioProposta?> EnvioAsync(Guid chave, CancellationToken ct);
    Task<EnvioProposta?> UltimoEnvioAsync(Guid orcamentoId, CancellationToken ct);
    void Adicionar(EnvioProposta envio);
    Task BloquearOrcamentoAsync(Guid id, CancellationToken ct);
}

public interface IEmailPropostaSender
{
    bool Disponivel { get; }
    Task EnviarAsync(EmailProposta email, CancellationToken ct);
}
