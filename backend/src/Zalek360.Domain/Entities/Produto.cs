using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

/// <summary>Catálogo base de produtos personalizáveis (camiseta, caneca, copo térmico, squeeze, ecobag, boné...).</summary>
public class Produto : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public string Categoria { get; private set; } = string.Empty;
    public string? DescricaoBase { get; private set; }
    public bool Ativo { get; private set; }
    public decimal? PrecoVenda { get; private set; }
    public decimal? Custo { get; private set; }
    public int Versao { get; private set; } = 1;

    public void Atualizar(string nome, string categoria, string? descricao, decimal? precoVenda, decimal? custo)
    {
        Nome = nome.Trim(); Categoria = categoria.Trim(); DescricaoBase = TextoOpcional.Limpar(descricao);
        PrecoVenda = precoVenda; Custo = custo; Versao++;
    }

    public void DefinirAtivo(bool ativo) { Ativo = ativo; Versao++; }

    private Produto() { }

    public static Produto Criar(string nome, string categoria, string? descricaoBase, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Nome = nome.Trim(),
        Categoria = categoria.Trim(),
        DescricaoBase = TextoOpcional.Limpar(descricaoBase),
        Ativo = true
    };
}
