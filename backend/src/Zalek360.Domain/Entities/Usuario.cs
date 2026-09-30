using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Entities;

/// <summary>Colaborador interno da Zalek (o cliente final não possui login no MVP 1).</summary>
public class Usuario : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Usuario() { }

    public static Usuario Criar(string nome, string email, string senhaHash, PerfilUsuario perfil, DateTime agoraUtc, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail obrigatório.", nameof(email));
        return new Usuario
        {
            Id = id ?? Guid.NewGuid(),
            Nome = nome.Trim(),
            Email = NormalizarEmail(email),
            SenhaHash = senhaHash,
            Perfil = perfil,
            Ativo = true,
            CriadoEm = agoraUtc
        };
    }

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

    public static string CalcularIniciais(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return "?";
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        return string.Concat(partes[0][0], partes[^1][0]).ToUpperInvariant();
    }
}
