using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zalek360.Application.Services;
using Zalek360.Infrastructure.Options;

namespace Zalek360.Infrastructure.BackgroundJobs;

/// <summary>UC03 A2 — expira automaticamente orçamentos "Aguardando Retorno" com validade vencida.</summary>
internal sealed class ExpiracaoOrcamentosWorker : BackgroundService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<ExpiracaoOrcamentosWorker> _logger;
    private readonly RotinasOptions _opcoes;

    public ExpiracaoOrcamentosWorker(IServiceScopeFactory escopos, ILogger<ExpiracaoOrcamentosWorker> logger, IOptions<RotinasOptions> opcoes)
    {
        _escopos = escopos;
        _logger = logger;
        _opcoes = opcoes.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opcoes.ExpiracaoHabilitada) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _opcoes.ExpiracaoIntervaloMinutos)));
        do
        {
            try
            {
                await using var escopo = _escopos.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<ExpiracaoOrcamentoService>();
                var expirados = await servico.ExpirarVencidosAsync(stoppingToken);
                if (expirados > 0) _logger.LogInformation("{Quantidade} orçamento(s) expirado(s) automaticamente.", expirados);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na rotina de expiração de orçamentos.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

/// <summary>Remove uploads nunca vinculados a um item (e seus arquivos) após o período configurado.</summary>
internal sealed class LimpezaAnexosWorker : BackgroundService
{
    private readonly IServiceScopeFactory _escopos;
    private readonly ILogger<LimpezaAnexosWorker> _logger;
    private readonly RotinasOptions _opcoes;

    public LimpezaAnexosWorker(IServiceScopeFactory escopos, ILogger<LimpezaAnexosWorker> logger, IOptions<RotinasOptions> opcoes)
    {
        _escopos = escopos;
        _logger = logger;
        _opcoes = opcoes.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opcoes.LimpezaAnexosHabilitada) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, _opcoes.LimpezaAnexosIntervaloHoras)));
        do
        {
            try
            {
                await using var escopo = _escopos.CreateAsyncScope();
                var servico = escopo.ServiceProvider.GetRequiredService<AnexoService>();
                var removidos = await servico.LimparPendentesAsync(TimeSpan.FromHours(Math.Max(1, _opcoes.AnexoPendenteIdadeHoras)), stoppingToken);
                if (removidos > 0) _logger.LogInformation("{Quantidade} anexo(s) pendente(s) removido(s).", removidos);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na rotina de limpeza de anexos.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
