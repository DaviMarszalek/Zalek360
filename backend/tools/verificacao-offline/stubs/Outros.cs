// STUBS DE ASSINATURA — somente para compilação offline. Reproduzem a API pública real dos pacotes.
#nullable enable
using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Npgsql
{
    public class NpgsqlException : DbException { }
    public sealed class PostgresException : NpgsqlException
    {
        public override string SqlState => throw null!;
        public string? ConstraintName => throw null!;
        public string? TableName => throw null!;
        public string MessageText => throw null!;
    }
    public static class PostgresErrorCodes
    {
        public const string UniqueViolation = "23505";
        public const string ForeignKeyViolation = "23503";
        public const string CheckViolation = "23514";
    }
}

namespace BCrypt.Net
{
    public enum HashType { None = -1, SHA256 = 0, SHA384 = 1, SHA512 = 2, Legacy384 = 3 }
    public sealed class BCrypt
    {
        public static string HashPassword(string inputKey) => throw null!;
        public static string HashPassword(string inputKey, int workFactor, bool enhancedEntropy = false) => throw null!;
        public static bool Verify(string text, string hash, bool enhancedEntropy = false, HashType hashType = HashType.SHA384) => throw null!;
    }
}

namespace Microsoft.IdentityModel.Tokens
{
    public abstract class SecurityKey { }
    public class SymmetricSecurityKey : SecurityKey { public SymmetricSecurityKey(byte[] key) { } }
    public class SigningCredentials { public SigningCredentials(SecurityKey key, string algorithm) { } }
    public static class SecurityAlgorithms { public const string HmacSha256 = "HS256"; }
    public abstract class SecurityToken { }
    public class TokenValidationParameters
    {
        public bool ValidateIssuer { get; set; }
        public string? ValidIssuer { get; set; }
        public bool ValidateAudience { get; set; }
        public string? ValidAudience { get; set; }
        public bool ValidateIssuerSigningKey { get; set; }
        public SecurityKey? IssuerSigningKey { get; set; }
        public bool ValidateLifetime { get; set; }
        public TimeSpan ClockSkew { get; set; }
        public string NameClaimType { get; set; } = "";
        public string RoleClaimType { get; set; } = "";
        public bool RequireExpirationTime { get; set; }
    }
}

namespace System.IdentityModel.Tokens.Jwt
{
    public class JwtSecurityToken : SecurityToken
    {
        public JwtSecurityToken(string? issuer = null, string? audience = null, IEnumerable<Claim>? claims = null, DateTime? notBefore = null,
            DateTime? expires = null, SigningCredentials? signingCredentials = null) { }
    }
    public class JwtSecurityTokenHandler { public virtual string WriteToken(SecurityToken token) => throw null!; }
    public struct JwtRegisteredClaimNames
    {
        public const string Sub = "sub";
        public const string Email = "email";
        public const string Jti = "jti";
        public const string Name = "name";
        public const string Iat = "iat";
    }
}

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    public static class JwtBearerDefaults { public const string AuthenticationScheme = "Bearer"; }

    public class JwtBearerOptions : AuthenticationSchemeOptions
    {
        public bool MapInboundClaims { get; set; } = true;
        public bool RequireHttpsMetadata { get; set; } = true;
        public bool SaveToken { get; set; } = true;
        public TokenValidationParameters TokenValidationParameters { get; set; } = new();
        public new JwtBearerEvents Events { get => (JwtBearerEvents)base.Events!; set => base.Events = value; }
    }

    public class JwtBearerEvents
    {
        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = _ => Task.CompletedTask;
        public Func<ForbiddenContext, Task> OnForbidden { get; set; } = _ => Task.CompletedTask;
        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;
        public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; } = _ => Task.CompletedTask;
        public Func<JwtBearerChallengeContext, Task> OnChallenge { get; set; } = _ => Task.CompletedTask;
    }

    public class MessageReceivedContext : ResultContext<JwtBearerOptions>
    {
        public MessageReceivedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options) : base(context, scheme, options) { }
        public string? Token { get; set; }
    }
    public class ForbiddenContext : ResultContext<JwtBearerOptions>
    {
        public ForbiddenContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options) : base(context, scheme, options) { }
    }
    public class AuthenticationFailedContext : ResultContext<JwtBearerOptions>
    {
        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options) : base(context, scheme, options) { }
        public Exception Exception { get; set; } = null!;
    }
    public class TokenValidatedContext : ResultContext<JwtBearerOptions>
    {
        public TokenValidatedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options) : base(context, scheme, options) { }
    }
    public class JwtBearerChallengeContext : PropertiesContext<JwtBearerOptions>
    {
        public JwtBearerChallengeContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options, AuthenticationProperties properties)
            : base(context, scheme, options, properties) { }
        public Exception? AuthenticateFailure { get; set; }
        public string? Error { get; set; }
        public bool Handled { get; private set; }
        public void HandleResponse() => Handled = true;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static class JwtBearerExtensions
    {
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder) => builder;
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions) => builder;
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions) => builder;
    }

    public static class SwaggerGenServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerGen(this IServiceCollection services, Action<SwaggerGenOptions>? setupAction = null) => services;
    }

    public static class SwaggerGenOptionsExtensions
    {
        public static void SwaggerDoc(this SwaggerGenOptions swaggerGenOptions, string name, Microsoft.OpenApi.Models.OpenApiInfo info) { }
        public static void AddSecurityDefinition(this SwaggerGenOptions swaggerGenOptions, string name, Microsoft.OpenApi.Models.OpenApiSecurityScheme securityScheme) { }
        public static void AddSecurityRequirement(this SwaggerGenOptions swaggerGenOptions, Microsoft.OpenApi.Models.OpenApiSecurityRequirement securityRequirement) { }
        public static void IncludeXmlComments(this SwaggerGenOptions swaggerGenOptions, string filePath, bool includeControllerXmlComments = false) { }
        public static void SupportNonNullableReferenceTypes(this SwaggerGenOptions swaggerGenOptions) { }
    }
}

namespace Swashbuckle.AspNetCore.SwaggerGen { public class SwaggerGenOptions { } }
namespace Swashbuckle.AspNetCore.SwaggerUI { public class SwaggerUIOptions { public string RoutePrefix { get; set; } = "swagger"; public string DocumentTitle { get; set; } = "Swagger UI"; } }
namespace Swashbuckle.AspNetCore.Swagger { public class SwaggerOptions { } }

namespace Microsoft.AspNetCore.Builder
{
    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerUI;

    public static class SwaggerBuilderExtensions
    {
        public static IApplicationBuilder UseSwagger(this IApplicationBuilder app, Action<SwaggerOptions>? setupAction = null) => app;
    }
    public static class SwaggerUIBuilderExtensions
    {
        public static IApplicationBuilder UseSwaggerUI(this IApplicationBuilder app, Action<SwaggerUIOptions>? setupAction = null) => app;
    }
    public static class SwaggerUIOptionsExtensions
    {
        public static void SwaggerEndpoint(this SwaggerUIOptions options, string url, string name) { }
    }
}

namespace Microsoft.OpenApi.Models
{
    public enum ReferenceType { Schema, Response, Parameter, Example, RequestBody, Header, SecurityScheme, Link, Callback, Tag }
    public enum SecuritySchemeType { ApiKey, Http, OAuth2, OpenIdConnect }
    public enum ParameterLocation { Query, Header, Path, Cookie }
    public class OpenApiInfo { public string? Title { get; set; } public string? Version { get; set; } public string? Description { get; set; } }
    public class OpenApiReference { public ReferenceType? Type { get; set; } public string? Id { get; set; } }
    public class OpenApiSecurityScheme
    {
        public string? Name { get; set; }
        public SecuritySchemeType Type { get; set; }
        public string? Scheme { get; set; }
        public string? BearerFormat { get; set; }
        public ParameterLocation In { get; set; }
        public string? Description { get; set; }
        public OpenApiReference? Reference { get; set; }
    }
    public class OpenApiSecurityRequirement : Dictionary<OpenApiSecurityScheme, IList<string>> { }
}
