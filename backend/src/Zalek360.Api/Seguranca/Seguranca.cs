using Zalek360.Application.Common;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Api.Seguranca;

public static class Politicas
{
    public const string Consulta = "Consulta";
    public const string Operacao = "Operacao";
    public const string Administracao = "Administracao";
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
