using Zalek360.Application.Interfaces;

namespace Zalek360.Application.Services;

/// <summary>
/// UC03 A2 — "Prazo de validade vence sem decisão → Expirado (automático, sem ação do atendente)".
/// A regra está no agregado (Orcamento.DeveExpirar / ExpirarAutomaticamente); este serviço apenas a aplica
/// em lote. É executado por um BackgroundService (na inicialização e periodicamente).
/// O momento registrado é 00:00 do dia seguinte ao fim da validade (fuso de negócio), independente da hora do job.
/// </summary>
public sealed class ExpiracaoOrcamentoService
{
    private readonly IOrcamentoRepository _orcamentos;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public ExpiracaoOrcamentoService(IOrcamentoRepository orcamentos, IUnitOfWork uow, IClock clock)
    {
        _orcamentos = orcamentos;
        _uow = uow;
        _clock = clock;
    }

    public async Task<int> ExpirarVencidosAsync(CancellationToken ct)
    {
        var hoje = _clock.HojeLocal;
        var agora = _clock.AgoraUtc;
        var candidatos = await _orcamentos.ListarParaExpiracaoAsync(hoje, ct);
        var expirados = 0;
        foreach (var orcamento in candidatos)
        {
            var momento = _clock.InicioDoDiaLocalEmUtc(orcamento.Validade.AddDays(1));
            if (orcamento.ExpirarAutomaticamente(hoje, momento, agora)) expirados++;
        }
        if (expirados > 0) await _uow.SaveChangesAsync(ct);
        return expirados;
    }
}
