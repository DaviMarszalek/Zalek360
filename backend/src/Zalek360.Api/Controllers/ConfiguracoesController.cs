using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;

namespace Zalek360.Api.Controllers;

[ApiController]
[Route("api/configuracoes/opcoes")]
[Authorize(Policy = Politicas.Administracao)]
public sealed class ConfiguracoesController(ConfiguracaoCatalogoService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<OpcaoCatalogoDto>> Listar(CancellationToken ct) => service.ListarAsync(false, ct);
    [HttpPost]
    public async Task<ActionResult<OpcaoCatalogoDto>> Criar(OpcaoCatalogoRequest r, CancellationToken ct) =>
        StatusCode(201, await service.SalvarAsync(null, r, ct));
    [HttpPut("{id:guid}")]
    public Task<OpcaoCatalogoDto> Atualizar(Guid id, OpcaoCatalogoRequest r, CancellationToken ct) => service.SalvarAsync(id, r, ct);
}

[ApiController]
[Route("api/catalogo/opcoes")]
[Authorize(Policy = Politicas.Operacao)]
public sealed class OpcoesCatalogoController(ConfiguracaoCatalogoService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<OpcaoCatalogoDto>> Ativas(CancellationToken ct) => service.ListarAsync(true, ct);
}
