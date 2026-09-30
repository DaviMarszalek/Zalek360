using Zalek360.Application.Common;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;

namespace Zalek360.Api.Seguranca;

public static class Politicas
{
    /// <summary>Perfis que operam o fluxo comercial. Preparado para perfis futuros (Operador, Gestor etc.).</summary>
    public const string Comercial = "Comercial";
    public static readonly string[] PerfisComerciais = { "Atendente", "Gestor", "Administrador" };
}

public sealed class OpcoesCookieAuth
{
    public const string Secao = "Autenticacao";
    public string NomeCookie { get; set; } = "zalek360_token";
    /// <summary>Em produção com HTTPS, mantenha true.</summary>
    public bool CookieSegura { get; set; } = true;
}

/// <summary>Usuário autenticado a partir do JWT (claim "sub").</summary>
public sealed class UsuarioAtualHttp : IUsuarioAtual
{
    private readonly IHttpContextAccessor _acessor;

    public UsuarioAtualHttp(IHttpContextAccessor acessor) => _acessor = acessor;

    public bool Autenticado => _acessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid Id =>
        Guid.TryParse(_acessor.HttpContext?.User.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new NaoAutenticadoException(CodigosErro.NaoAutenticado, "Sua sessão expirou. Entre novamente.");
}
