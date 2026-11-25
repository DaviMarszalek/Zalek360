using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;

namespace Zalek360.Api.Controllers;

[ApiController, Route("api/orcamentos/{id:guid}/proposta"), Authorize(Policy = Politicas.Operacao)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PropostasController(PropostaService service, OrcamentoDocumentoService documentos) : ControllerBase
{
    [HttpGet] public Task<PreparacaoPropostaDto> Preparar(Guid id, CancellationToken ct) => service.PrepararAsync(id, ct);
    [HttpPost("email")] public Task<ResultadoEnvioPropostaDto> Enviar(Guid id, EnviarPropostaEmailRequest r, CancellationToken ct) => service.EnviarAsync(id, r, ct);
    [HttpGet("email/{chave:guid}")] public Task<EnvioPropostaDto> Consultar(Guid id, Guid chave, CancellationToken ct) => service.ConsultarEnvioAsync(id, chave, ct);
    [HttpPost("confirmacao")] public Task<OrcamentoDetalheDto> Confirmar(Guid id, ConfirmarPropostaRequest r, CancellationToken ct) => service.ConfirmarExternoAsync(id, r, ct);
    [HttpPost("pdf")]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        await service.PrepararAsync(id, ct);
        var pdf = await documentos.GerarAsync(id, true, ct, paraApresentacao: true);
        return File(pdf.Conteudo, "application/pdf", pdf.NomeArquivo);
    }
}

[ApiController, Route("api/configuracoes/modelos-proposta"), Authorize(Policy = Politicas.Administracao)]
public sealed class ModelosPropostaController(PropostaService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Listar(CancellationToken ct) => Ok(new { emailDisponivel = service.EmailDisponivel, modelos = await service.ModelosAsync(ct) });
    [HttpPut("{id:guid}")] public Task<ModeloMensagemDto> Atualizar(Guid id, ModeloMensagemRequest r, CancellationToken ct) => service.AtualizarModeloAsync(id, r, ct);
}
