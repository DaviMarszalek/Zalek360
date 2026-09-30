namespace Zalek360.Application.Dtos;

public sealed record LoginRequest(string? Email, string? Senha, bool ManterConectado);

public sealed record LoginResponse(UsuarioDto Usuario, string AccessToken, DateTime ExpiraEm, bool ManterConectado);
