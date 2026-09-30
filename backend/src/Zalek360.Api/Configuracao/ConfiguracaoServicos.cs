using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Zalek360.Api.Middleware;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;
using Zalek360.Domain.Common;
using Zalek360.Infrastructure.Options;

namespace Zalek360.Api.Configuracao;

public static class ConfiguracaoServicos
{
    /// <summary>Casos de uso da camada Application.</summary>
    public static IServiceCollection AddZalekApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<OrcamentoService>();
        services.AddScoped<PedidoService>();
        services.AddScoped<AnexoService>();
        services.AddScoped<ExpiracaoOrcamentoService>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();
        return services;
    }

    public static IServiceCollection AddZalekApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            })
            .ConfigureApiBehaviorOptions(o =>
            {
                // Erros de binding/JSON no mesmo formato de erro da API.
                o.InvalidModelStateResponseFactory = contexto =>
                {
                    var erros = contexto.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            e => NormalizarCampo(e.Key),
                            e => e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Valor inválido." : TraduzirMensagem(x.ErrorMessage)).ToArray());
                    var resposta = new ErroResposta(StatusCodes.Status400BadRequest, CodigosErro.Validacao,
                        "Alguns campos estão com formato inválido.", erros, null, contexto.HttpContext.TraceIdentifier);
                    return new BadRequestObjectResult(resposta);
                };
            });
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.Configure<OpcoesCookieAuth>(configuration.GetSection(OpcoesCookieAuth.Secao));
        var jwt = configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>() ?? new JwtOptions();
        var nomeCookie = configuration.GetSection(OpcoesCookieAuth.Secao).Get<OpcoesCookieAuth>()?.NomeCookie ?? "zalek360_token";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave.PadRight(32, '_'))),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
                o.Events = new JwtBearerEvents
                {
                    // Frontend: token no cookie httpOnly. Swagger/integrações: header Authorization: Bearer.
                    OnMessageReceived = contexto =>
                    {
                        if (string.IsNullOrEmpty(contexto.Token) && contexto.Request.Cookies.TryGetValue(nomeCookie, out var token))
                            contexto.Token = token;
                        return Task.CompletedTask;
                    },
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await contexto.Response.WriteAsJsonAsync(new ErroResposta(401, CodigosErro.NaoAutenticado,
                            "Sua sessão expirou ou você não está autenticado. Entre novamente.",
                            new Dictionary<string, string[]>(), null, contexto.HttpContext.TraceIdentifier));
                    },
                    OnForbidden = async contexto =>
                    {
                        contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await contexto.Response.WriteAsJsonAsync(new ErroResposta(403, CodigosErro.AcessoNegado,
                            "Seu perfil não tem permissão para esta operação.",
                            new Dictionary<string, string[]>(), null, contexto.HttpContext.TraceIdentifier));
                    }
                };
            });

        services.AddAuthorization(o =>
        {
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            o.AddPolicy(Politicas.Comercial, p => p.RequireAuthenticatedUser().RequireRole(Politicas.PerfisComerciais));
        });

        var origens = configuration.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(o => o.AddDefaultPolicy(p =>
        {
            if (origens.Length > 0) p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Zalek360 API",
                Version = "v1",
                Description = "Sistema comercial interno da Zalek Personalizados — MVP 1 (clientes, orçamentos, decisões e pedidos). " +
                              "Autentique em POST /api/auth/login: o cookie de sessão é definido automaticamente, " +
                              "ou use o accessToken retornado no botão Authorize (Bearer)."
            });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Cole apenas o token JWT retornado pelo login."
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
            var xml = Path.Combine(AppContext.BaseDirectory, "Zalek360.Api.xml");
            if (File.Exists(xml)) o.IncludeXmlComments(xml);
            o.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    private static string NormalizarCampo(string chave)
    {
        var campo = chave.StartsWith("$.") ? chave[2..] : chave;
        if (campo.Length == 0 || campo == "$" || campo.Equals("request", StringComparison.OrdinalIgnoreCase)) return "corpo";
        return char.ToLowerInvariant(campo[0]) + campo[1..];
    }

    private static string TraduzirMensagem(string mensagem) =>
        mensagem.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ? "Valor em formato inválido." :
        mensagem.Contains("is required", StringComparison.OrdinalIgnoreCase) ? "Campo obrigatório." :
        mensagem.Contains("is not valid", StringComparison.OrdinalIgnoreCase) ? "Valor inválido." : mensagem;
}
