using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Zalek360.Api.Seguranca;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Application.Services;

namespace Zalek360.Api.Controllers;

/// <summary>Autenticação dos colaboradores (clientes externos não acessam o sistema).</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly IUsuarioAtual _usuario;
    private readonly OpcoesCookieAuth _cookie;

    public AuthController(AuthService auth, IUsuarioAtual usuario, IOptions<OpcoesCookieAuth> cookie)
    {
        _auth = auth;
        _usuario = usuario;
        _cookie = cookie.Value;
    }

    /// <summary>Entra no sistema. Define o cookie httpOnly de sessão e também retorna o token (uso no Swagger).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var resposta = await _auth.EntrarAsync(request, ct);
        Response.Cookies.Append(_cookie.NomeCookie, resposta.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookie.CookieSegura,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            // "Manter conectado": cookie persistente; caso contrário, cookie de sessão do navegador.
            Expires = resposta.ManterConectado ? new DateTimeOffset(resposta.ExpiraEm, TimeSpan.Zero) : null
        });
        return Ok(resposta);
    }

    /// <summary>Encerra a sessão (remove o cookie).</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(_cookie.NomeCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookie.CookieSegura,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
        return NoContent();
    }

    /// <summary>Usuário autenticado.</summary>
    [HttpGet("me")]
    public Task<UsuarioDto> Me(CancellationToken ct) => _auth.ObterAsync(_usuario.Id, ct);
}
