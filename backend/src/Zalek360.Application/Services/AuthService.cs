using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed class AuthService
{
    // Hash BCrypt fictício: garante tempo de resposta semelhante quando o e-mail não existe (evita enumeração de usuários).
    private const string HashFicticio = "$2a$11$K3g6XpVdgWmS1nq7b8Zt1eCj3HkX9p1Qd8m4sZ9yYp0mH5Qm1xO9W";

    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUsuarioRepository usuarios, IPasswordHasher hasher, ITokenService tokens)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<LoginResponse> EntrarAsync(LoginRequest request, CancellationToken ct)
    {
        var erros = new ColetorErros();
        if (string.IsNullOrWhiteSpace(request.Email)) erros.Adicionar("email", "Informe o e-mail.");
        if (string.IsNullOrEmpty(request.Senha)) erros.Adicionar("senha", "Informe a senha.");
        if (erros.PossuiErros) throw new ValidacaoException(CodigosErro.Validacao, "Informe e-mail e senha.", erros.ParaDicionario());

        var usuario = await _usuarios.ObterPorEmailAsync(Usuario.NormalizarEmail(request.Email!), ct);
        var senhaOk = _hasher.Verificar(request.Senha!, usuario?.SenhaHash ?? HashFicticio);
        if (usuario is null || !usuario.Ativo || !senhaOk)
            throw new NaoAutenticadoException(CodigosErro.CredenciaisInvalidas, "E-mail ou senha inválidos.");

        var token = _tokens.Gerar(usuario, request.ManterConectado);
        return new LoginResponse(Mapear(usuario), token.Token, token.ExpiraEm, request.ManterConectado);
    }

    public async Task<UsuarioDto> ObterAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await _usuarios.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null || !usuario.Ativo)
            throw new NaoAutenticadoException(CodigosErro.NaoAutenticado, "Sua sessão expirou. Entre novamente.");
        return Mapear(usuario);
    }

    public static UsuarioDto Mapear(Usuario u) => new(u.Id, u.Nome, u.Email, u.Perfil, Usuario.CalcularIniciais(u.Nome));
}
