using System.Reflection;
var testes = typeof(Zalek360.Tests.Suporte.Dados).Assembly.GetTypes()
    .SelectMany(t => t.GetMethods().Where(m => m.GetCustomAttribute<Xunit.FactAttribute>() != null).Select(m => (t, m)))
    .OrderBy(x => x.t.FullName).ThenBy(x => x.m.Name).ToList();
int ok = 0, falhas = 0;
foreach (var (t, m) in testes)
{
    try
    {
        var r = m.Invoke(Activator.CreateInstance(t), null);
        if (r is Task task) await task;
        ok++; Console.WriteLine($"  [OK]    {t.Name}.{m.Name}");
    }
    catch (Exception e)
    {
        var ex = e is TargetInvocationException tie ? tie.InnerException! : e;
        falhas++; Console.WriteLine($"  [FALHA] {t.Name}.{m.Name}\n          {ex.GetType().Name}: {ex.Message}");
    }
}
Console.WriteLine($"\n{ok} aprovados, {falhas} falhas, {testes.Count} testes");
return falhas == 0 ? 0 : 1;
