using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Zalek360.Api.Configuracao;
using Zalek360.Api.Ferramentas;
using Zalek360.Api.Middleware;
using Zalek360.Infrastructure;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Infrastructure.Seguranca;

// Ferramenta administrativa executada no container (ex.: redefinir senha); não inicia o servidor web.
if (FerramentaRedefinirSenha.Solicitada(args))
    return await FerramentaRedefinirSenha.ExecutarAsync(args);

var builder = WebApplication.CreateBuilder(args);

// Limite de upload: arquivos até 20 MB + margem para o multipart.
const long LimiteRequisicao = 25L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = LimiteRequisicao);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = LimiteRequisicao);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddZalekApplication()
    .AddZalekApi(builder.Configuration);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Fora de Development, segredos ausentes, curtos ou publicados no repositório impedem a subida.
var banco = app.Services.GetRequiredService<IOptions<BancoOptions>>().Value;
ValidacaoImplantacao.Garantir(app.Configuration["Jwt:Chave"], app.Configuration.GetConnectionString("Zalek360"),
    banco.ExecutarSeed, banco.SenhaUsuariosDemo, app.Environment.IsDevelopment());

app.UseForwardedHeaders();
app.UseMiddleware<TratamentoErrosMiddleware>();

// Documentação interativa somente em Development (pode ser desligada também ali com Swagger:Habilitado=false).
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Zalek360 API v1");
        o.DocumentTitle = "Zalek360 API";
    });
}

if (app.Configuration.GetValue("Https:Redirecionar", false))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();

// Migrations + verificação de esquema + seed antes de aceitar requisições.
await InicializadorBanco.InicializarAsync(app.Services);

app.Run();
return 0;

/// <summary>Exposto para testes de integração (WebApplicationFactory).</summary>
public partial class Program { }
