using System.Net.Mail;
using System.Text;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Application.Services;

public sealed class UsuarioService
{
    private const int TamanhoMinimoSenha = 12;
    private const int TamanhoMaximoSenhaBytes = 72;

    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public UsuarioService(
        IUsuarioRepository usuarios,
        IPasswordHasher hasher,
        IUnitOfWork uow,
        IClock clock)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UsuarioAcessoDto>> ListarAsync(CancellationToken ct)
    {
        var usuarios = await _usuarios.ListarAsync(ct);
        return usuarios.Select(MapearAcesso).ToArray();
    }

    public async Task<UsuarioAcessoDto> CriarGestorAsync(CriarGestorRequest request, CancellationToken ct)
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Nome))
            erros["nome"] = ["Informe o nome."];
        else if (request.Nome.Trim().Length > 100)
            erros["nome"] = ["Use até 100 caracteres."];

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            erros["email"] = ["Informe o e-mail."];
        else if (email.Length > 150 || !EmailValido(email))
            erros["email"] = ["Informe um e-mail válido com até 150 caracteres."];

        if (string.IsNullOrEmpty(request.Senha))
            erros["senha"] = ["Informe a senha."];
        else if (request.Senha.Length < TamanhoMinimoSenha)
            erros["senha"] = [$"Use pelo menos {TamanhoMinimoSenha} caracteres."];
        else if (Encoding.UTF8.GetByteCount(request.Senha) > TamanhoMaximoSenhaBytes)
            erros["senha"] = [$"A senha deve ter até {TamanhoMaximoSenhaBytes} bytes em UTF-8."];

        if (erros.Count > 0)
            throw new ValidacaoException(CodigosErro.Validacao, "Corrija os campos destacados.", erros);

        var emailNormalizado = Usuario.NormalizarEmail(email!);
        if (await _usuarios.EmailExisteAsync(emailNormalizado, ct))
            throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe um usuário cadastrado com esse e-mail.",
                new Dictionary<string, string[]> { ["email"] = ["Escolha um e-mail que ainda não esteja cadastrado."] });

        var usuario = Usuario.Criar(request.Nome!.Trim(), emailNormalizado, _hasher.Gerar(request.Senha!),
            PerfilUsuario.Gestor, _clock.AgoraUtc);
        _usuarios.Adicionar(usuario);
        await _uow.SaveChangesAsync(ct);

        return MapearAcesso(usuario);
    }

    private static bool EmailValido(string email) =>
        MailAddress.TryCreate(email, out var endereco) &&
        string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase);

    private static UsuarioAcessoDto MapearAcesso(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil, Usuario.CalcularIniciais(usuario.Nome),
            usuario.Ativo, usuario.CriadoEm);
}
