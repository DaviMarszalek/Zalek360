using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;

namespace Zalek360.Api.Controllers;

[ApiController]
[Route("api/orcamentos")]
[Authorize(Policy = Politicas.Operacao)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AnaliseOrcamentoController(AnaliseOrcamentoService service) : ControllerBase
{
    [HttpPost("analise-interna")]
    public Task<AnaliseOrcamentoDto> Calcular(AnaliseOrcamentoRequest r, CancellationToken ct) => service.CalcularAsync(r, ct);
    [HttpGet("{id:guid}/analise-interna")]
    public Task<AnaliseOrcamentoDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);
}
