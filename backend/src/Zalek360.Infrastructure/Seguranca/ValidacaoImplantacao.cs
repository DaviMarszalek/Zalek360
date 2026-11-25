using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Zalek360.Infrastructure.Seguranca;

/// <summary>
/// Impede a subida da API fora de Development com segredos ausentes, fracos ou já publicados no repositório.
/// As mensagens nunca incluem os valores avaliados.
/// </summary>
public static class ValidacaoImplantacao
{
    public const int TamanhoMinimoChaveJwt = 32;
    public const int TamanhoMinimoSenhaBanco = 16;

    // SHA-256 de valores que já estiveram versionados no repositório público e, portanto, não protegem nada:
    // chave JWT padrão antiga do docker-compose.yml, chave JWT de appsettings.Development.json e
    // senha padrão antiga do PostgreSQL no docker-compose.yml/README.
    private static readonly HashSet<string> SegredosPublicados = new(StringComparer.OrdinalIgnoreCase)
    {
        "124988cb57a0c0c90122276c2f3b523e80f0d31e806226aec75f76e0038b2e79",
        "43a9fbf82b8e3dfd111ca7049dd910f1f6e6c9b4c492d6b8c24c189f3ece4738",
        "5eff00fbff545fcd8139c1d154e8b739dcef5dcd1c11418131d800393053841d",
    };

    /// <summary>Lista os problemas de configuração; vazia quando a configuração pode ser usada.</summary>
    public static IReadOnlyList<string> Verificar(string? chaveJwt, string? conexaoBanco, bool executarSeed,
        string? senhaUsuariosDemo, bool desenvolvimento)
    {
        var problemas = new List<string>();
        if (executarSeed && string.IsNullOrWhiteSpace(senhaUsuariosDemo))
            problemas.Add("Banco:ExecutarSeed (EXECUTAR_SEED) está ligado, mas Banco:SenhaUsuariosDemo (SENHA_USUARIOS_DEMO) está vazio.");
        if (desenvolvimento) return problemas;

        if (string.IsNullOrWhiteSpace(chaveJwt) || chaveJwt.Length < TamanhoMinimoChaveJwt)
            problemas.Add($"Jwt:Chave (JWT_CHAVE) precisa ter pelo menos {TamanhoMinimoChaveJwt} caracteres.");
        else if (Publicado(chaveJwt))
            problemas.Add("Jwt:Chave (JWT_CHAVE) usa um valor publicado no repositório. Gere uma chave nova (docs/11-atualizar-ambiente-principal.md).");

        var senhaBanco = SenhaDaConexao(conexaoBanco);
        if (string.IsNullOrEmpty(senhaBanco) || senhaBanco.Length < TamanhoMinimoSenhaBanco)
            problemas.Add($"A senha do PostgreSQL (POSTGRES_PASSWORD) precisa ter pelo menos {TamanhoMinimoSenhaBanco} caracteres.");
        else if (Publicado(senhaBanco))
            problemas.Add("A senha do PostgreSQL (POSTGRES_PASSWORD) usa um valor publicado no repositório. Troque-a (docs/11-atualizar-ambiente-principal.md).");
        return problemas;
    }

    /// <summary>Lança uma exceção com mensagem clara (sem valores) quando a configuração é insegura.</summary>
    public static void Garantir(string? chaveJwt, string? conexaoBanco, bool executarSeed, string? senhaUsuariosDemo, bool desenvolvimento)
    {
        var problemas = Verificar(chaveJwt, conexaoBanco, executarSeed, senhaUsuariosDemo, desenvolvimento);
        if (problemas.Count > 0)
            throw new InvalidOperationException("Configuração insegura; a API não será iniciada. " + string.Join(" ", problemas));
    }

    public static bool Publicado(string valor) =>
        SegredosPublicados.Contains(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor))));

    private static string? SenhaDaConexao(string? conexao)
    {
        if (string.IsNullOrWhiteSpace(conexao)) return null;
        try { return new NpgsqlConnectionStringBuilder(conexao).Password; }
        catch (ArgumentException) { return null; }
    }
}
