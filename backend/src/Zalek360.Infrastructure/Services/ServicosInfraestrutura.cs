using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Services;

/// <summary>Numeração via sequências do PostgreSQL: atômica e segura sob concorrência (sem MAX()+1).</summary>
internal sealed class NumeradorPostgres : INumeradorDocumentos
{
    private readonly ZalekDbContext _db;
    public NumeradorPostgres(ZalekDbContext db) => _db = db;

    public Task<int> ProximoNumeroOrcamentoAsync(CancellationToken ct) => ProximoAsync(Sequencias.Orcamento, ct);
    public Task<int> ProximoNumeroPedidoAsync(CancellationToken ct) => ProximoAsync(Sequencias.Pedido, ct);

    private async Task<int> ProximoAsync(string sequencia, CancellationToken ct)
    {
        // O nome da sequência é uma constante interna (não há entrada do usuário no SQL).
        var valores = await _db.Database.SqlQueryRaw<long>("SELECT nextval('" + sequencia + "') AS \"Value\"").ToListAsync(ct);
        return checked((int)valores[0]);
    }
}

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int CustoTrabalho = 11;

    public string Gerar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, CustoTrabalho);

    public bool Verificar(string senha, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(senha, hash); }
        catch (Exception) { return false; }
    }
}

internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opcoes;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> opcoes, IClock clock)
    {
        _opcoes = opcoes.Value;
        _clock = clock;
    }

    public TokenGerado Gerar(Usuario usuario, bool manterConectado)
    {
        var agora = _clock.AgoraUtc;
        var expira = manterConectado
            ? agora.AddDays(_opcoes.DiasManterConectado)
            : agora.AddMinutes(_opcoes.MinutosExpiracao);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("name", usuario.Nome),
            new Claim("role", usuario.Perfil.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Chave));
        var token = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            notBefore: agora,
            expires: expira,
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256));
        return new TokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

/// <summary>
/// Armazenamento em disco local (volume Docker). Chaves aleatórias "aaaa/mm/guid.ext": o nome original nunca
/// vira caminho, evitando path traversal. Para nuvem, implemente IFileStorage com S3/Azure Blob.
/// </summary>
internal sealed partial class ArmazenamentoLocal : IFileStorage
{
    private readonly string _raiz;

    public ArmazenamentoLocal(IOptions<ArmazenamentoOptions> opcoes)
    {
        _raiz = Path.GetFullPath(opcoes.Value.CaminhoLocal);
        Directory.CreateDirectory(_raiz);
    }

    public async Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct)
    {
        if (!ExtensaoSegura().IsMatch(extensao)) throw new ArgumentException("Extensão inválida.", nameof(extensao));
        var agora = DateTime.UtcNow;
        var chave = $"{agora:yyyy}/{agora:MM}/{Guid.NewGuid():N}{extensao}";
        var caminho = Resolver(chave);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        await using var arquivo = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await conteudo.CopyToAsync(arquivo, ct);
        return chave;
    }

    public Task<Stream> AbrirLeituraAsync(string chave, CancellationToken ct)
    {
        var caminho = Resolver(chave);
        if (!File.Exists(caminho)) throw new FileNotFoundException("Arquivo não encontrado no armazenamento.", chave);
        Stream stream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task ExcluirAsync(string chave, CancellationToken ct)
    {
        var caminho = Resolver(chave);
        if (File.Exists(caminho)) File.Delete(caminho);
        return Task.CompletedTask;
    }

    private string Resolver(string chave)
    {
        var caminho = Path.GetFullPath(Path.Combine(_raiz, chave));
        if (!caminho.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Caminho de arquivo inválido.");
        return caminho;
    }

    [GeneratedRegex("^\\.[a-z0-9]{1,5}$")]
    private static partial Regex ExtensaoSegura();
}
