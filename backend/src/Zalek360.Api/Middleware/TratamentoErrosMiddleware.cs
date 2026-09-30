using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zalek360.Application.Common;
using Zalek360.Domain.Common;

namespace Zalek360.Api.Middleware;

/// <summary>Formato único de erro da API (RNF 5.2.2 / 5.3.1).</summary>
public sealed record ErroResposta(
    int Status,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]> Errors,
    object? Details = null,
    string? TraceId = null);

/// <summary>Tratamento centralizado: nenhuma exceção vaza detalhes internos para o cliente.</summary>
public sealed class TratamentoErrosMiddleware
{
    private static readonly IReadOnlyDictionary<string, string[]> SemErros = new Dictionary<string, string[]>();
    private readonly RequestDelegate _next;
    private readonly ILogger<TratamentoErrosMiddleware> _logger;

    public TratamentoErrosMiddleware(RequestDelegate next, ILogger<TratamentoErrosMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _next(contexto);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            // Cliente cancelou a requisição; nada a responder.
        }
        catch (Exception ex)
        {
            if (contexto.Response.HasStarted) throw;
            var erro = Mapear(ex, contexto);
            if (erro.Status >= 500)
                _logger.LogError(ex, "Erro não tratado ({TraceId}) em {Metodo} {Caminho}", erro.TraceId, contexto.Request.Method, contexto.Request.Path);
            else
                _logger.LogInformation("Requisição rejeitada {Status} {Codigo}: {Mensagem}", erro.Status, erro.Code, erro.Message);

            contexto.Response.Clear();
            contexto.Response.StatusCode = erro.Status;
            contexto.Response.ContentType = "application/json; charset=utf-8";
            var json = contexto.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>();
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(erro, json.Value.SerializerOptions));
        }
    }

    private static ErroResposta Mapear(Exception ex, HttpContext contexto)
    {
        var traceId = Activity.Current?.Id ?? contexto.TraceIdentifier;
        return ex switch
        {
            AppException app => new ErroResposta(app.StatusCode, app.Codigo, app.Message, app.Erros, app.Detalhes, traceId),
            DomainException dom => new ErroResposta(dom.Categoria switch
            {
                CategoriaErro.Validacao => StatusCodes.Status400BadRequest,
                CategoriaErro.Conflito => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity
            }, dom.Codigo, dom.Message, dom.Erros, null, traceId),
            DbUpdateConcurrencyException => new ErroResposta(StatusCodes.Status409Conflict, CodigosErro.ConflitoConcorrencia,
                Concorrencia.Mensagem, SemErros, null, traceId),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg } =>
                pg.ConstraintName == "ux_clientes_cpf_cnpj"
                    ? new ErroResposta(StatusCodes.Status409Conflict, CodigosErro.ClienteDocumentoDuplicado,
                        "Já existe um cliente cadastrado com este CPF/CNPJ.",
                        new Dictionary<string, string[]> { ["documento"] = new[] { "Este CPF/CNPJ já está cadastrado." } }, null, traceId)
                    : new ErroResposta(StatusCodes.Status409Conflict, CodigosErro.RegistroDuplicado,
                        "Já existe um registro com estes dados.", SemErros, null, traceId),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                new ErroResposta(StatusCodes.Status409Conflict, CodigosErro.RegistroRelacionado,
                    "Operação não permitida: existe um registro relacionado.", SemErros, null, traceId),
            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge =>
                new ErroResposta(StatusCodes.Status413PayloadTooLarge, CodigosErro.AnexoTamanhoExcedido,
                    "O arquivo excede o tamanho máximo de 20 MB.", SemErros, null, traceId),
            BadHttpRequestException bad => new ErroResposta(bad.StatusCode, CodigosErro.Validacao,
                "Requisição inválida.", SemErros, null, traceId),
            _ => new ErroResposta(StatusCodes.Status500InternalServerError, CodigosErro.ErroInterno,
                "Ocorreu um erro inesperado. Tente novamente em instantes.", SemErros, null, traceId)
        };
    }
}
