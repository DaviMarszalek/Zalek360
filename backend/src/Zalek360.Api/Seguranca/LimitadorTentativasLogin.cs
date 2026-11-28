using System.Collections.Concurrent;

namespace Zalek360.Api.Seguranca;

/// <summary>
/// Limita tentativas de login FALHAS por conta (e-mail normalizado), em memória: após <see cref="MaximoFalhas"/>
/// falhas dentro da janela, novas tentativas para essa conta recebem 429 até a janela acabar — inclusive com a senha
/// certa, para não revelar o acerto durante um ataque. Login bem-sucedido zera o contador.
/// Por conta (e não por IP), porque a API fica atrás do proxy do frontend e todos os acessos chegam pelo mesmo IP.
/// </summary>
public sealed class LimitadorTentativasLogin
{
    public const int MaximoFalhas = 5;
    public static readonly TimeSpan Janela = TimeSpan.FromMinutes(15);
    private const int LimiteEntradas = 10_000;

    private readonly ConcurrentDictionary<string, (int Falhas, DateTimeOffset Inicio)> _falhas = new(StringComparer.Ordinal);
    private readonly TimeProvider _tempo;

    public LimitadorTentativasLogin(TimeProvider tempo) => _tempo = tempo;

    /// <summary>Tempo restante de bloqueio da conta, ou null se pode tentar.</summary>
    public TimeSpan? Bloqueio(string conta)
    {
        if (string.IsNullOrEmpty(conta) || !_falhas.TryGetValue(conta, out var f)) return null;
        var fim = f.Inicio + Janela;
        var agora = _tempo.GetUtcNow();
        if (agora >= fim) { _falhas.TryRemove(conta, out _); return null; }
        return f.Falhas >= MaximoFalhas ? fim - agora : null;
    }

    public void RegistrarFalha(string conta)
    {
        if (string.IsNullOrEmpty(conta)) return;
        var agora = _tempo.GetUtcNow();
        if (_falhas.Count >= LimiteEntradas) RemoverVencidas(agora);
        _falhas.AddOrUpdate(conta, _ => (1, agora), (_, f) => agora >= f.Inicio + Janela ? (1, agora) : (f.Falhas + 1, f.Inicio));
    }

    public void Limpar(string conta)
    {
        if (!string.IsNullOrEmpty(conta)) _falhas.TryRemove(conta, out _);
    }

    private void RemoverVencidas(DateTimeOffset agora)
    {
        foreach (var (conta, f) in _falhas)
            if (agora >= f.Inicio + Janela) _falhas.TryRemove(conta, out _);
    }

    /// <summary>E-mail mascarado para log (ex.: a***@empresa.com.br).</summary>
    public static string Mascarar(string email)
    {
        var arroba = email.IndexOf('@');
        if (arroba <= 0) return "***";
        return email[0] + "***" + email[arroba..];
    }
}
