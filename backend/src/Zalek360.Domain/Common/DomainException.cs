namespace Zalek360.Domain.Common;

public enum CategoriaErro
{
    /// <summary>Dados de entrada inválidos (HTTP 400).</summary>
    Validacao,
    /// <summary>Violação de regra de negócio (HTTP 422).</summary>
    RegraNegocio,
    /// <summary>Conflito com o estado atual do recurso (HTTP 409).</summary>
    Conflito
}

public class DomainException : Exception
{
    public string Codigo { get; }
    public CategoriaErro Categoria { get; }
    public IReadOnlyDictionary<string, string[]> Erros { get; }

    public DomainException(
        string codigo,
        string mensagem,
        CategoriaErro categoria = CategoriaErro.RegraNegocio,
        IReadOnlyDictionary<string, string[]>? erros = null) : base(mensagem)
    {
        Codigo = codigo;
        Categoria = categoria;
        Erros = erros ?? new Dictionary<string, string[]>();
    }
}

/// <summary>Acumula erros por campo para devolver todos de uma vez (como nos protótipos).</summary>
public sealed class ColetorErros
{
    private readonly Dictionary<string, List<string>> _erros = new();

    public bool PossuiErros => _erros.Count > 0;

    public bool Contem(string campo) => _erros.ContainsKey(campo);

    public void Adicionar(string campo, string mensagem)
    {
        if (!_erros.TryGetValue(campo, out var lista))
        {
            lista = new List<string>();
            _erros[campo] = lista;
        }
        if (!lista.Contains(mensagem)) lista.Add(mensagem);
    }

    public void Mesclar(IReadOnlyDictionary<string, string[]> outros, string prefixo = "")
    {
        foreach (var (campo, mensagens) in outros)
            foreach (var m in mensagens) Adicionar(prefixo + campo, m);
    }

    public IReadOnlyDictionary<string, string[]> ParaDicionario() =>
        _erros.ToDictionary(k => k.Key, v => v.Value.ToArray());

    public void LancarSePossuirErros(string codigo, string mensagem)
    {
        if (PossuiErros)
            throw new DomainException(codigo, mensagem, CategoriaErro.Validacao, ParaDicionario());
    }
}
