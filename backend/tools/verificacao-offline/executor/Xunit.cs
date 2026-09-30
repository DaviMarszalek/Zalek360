// Executor mínimo compatível com a API do xUnit usada nos testes (somente para verificação offline).
namespace Xunit;
[AttributeUsage(AttributeTargets.Method)] public sealed class FactAttribute : Attribute { }
public sealed class XunitException : Exception { public XunitException(string m) : base(m) { } }
public static class Assert
{
    static void F(string m) => throw new XunitException(m);
    public static void Equal<T>(T esperado, T atual) { if (!EqualityComparer<T>.Default.Equals(esperado, atual)) F($"Equal: esperado <{esperado}>, obtido <{atual}>"); }
    public static void NotEqual<T>(T a, T b) { if (EqualityComparer<T>.Default.Equals(a, b)) F($"NotEqual: <{a}>"); }
    public static void True(bool c) { if (!c) F("True falhou"); }
    public static void False(bool c) { if (c) F("False falhou"); }
    public static void Null(object? o) { if (o is not null) F($"Null: obtido {o}"); }
    public static void NotNull(object? o) { if (o is null) F("NotNull falhou"); }
    public static void Empty<T>(IEnumerable<T> c) { if (c.Any()) F($"Empty: {c.Count()} itens"); }
    public static T Single<T>(IEnumerable<T> c) { var l = c.ToList(); if (l.Count != 1) F($"Single: {l.Count} itens"); return l[0]; }
    public static void Single<T>(IEnumerable<T> c, Predicate<T> p) { var n = c.Count(x => p(x)); if (n != 1) F($"Single(pred): {n} itens"); }
    public static void Contains<T>(IEnumerable<T> c, Predicate<T> p) { if (!c.Any(x => p(x))) F("Contains(pred) falhou"); }
    public static void All<T>(IEnumerable<T> c, Action<T> a) { foreach (var x in c) a(x); }
    public static T Throws<T>(Action a) where T : Exception
    {
        try { a(); } catch (Exception e) { if (e.GetType() == typeof(T)) return (T)e; F($"Throws<{typeof(T).Name}>: lançou {e.GetType().Name}: {e.Message}"); }
        F($"Throws<{typeof(T).Name}>: nada lançado"); return null!;
    }
    public static async Task<T> ThrowsAsync<T>(Func<Task> a) where T : Exception
    {
        try { await a(); } catch (Exception e) { if (e.GetType() == typeof(T)) return (T)e; F($"ThrowsAsync<{typeof(T).Name}>: lançou {e.GetType().Name}: {e.Message}"); }
        F($"ThrowsAsync<{typeof(T).Name}>: nada lançado"); return null!;
    }
}
