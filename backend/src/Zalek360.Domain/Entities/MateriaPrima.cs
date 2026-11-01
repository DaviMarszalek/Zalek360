using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

public sealed class MateriaPrima : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public string Unidade { get; private set; } = string.Empty;
    public decimal QuantidadeDisponivel { get; private set; }
    public decimal EstoqueMinimo { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int Versao { get; private set; }

    private MateriaPrima() { }
    public static MateriaPrima Criar(string nome, string unidade, decimal quantidade, decimal minimo) => new()
    { Nome = nome.Trim(), Unidade = unidade.Trim(), QuantidadeDisponivel = quantidade, EstoqueMinimo = minimo };
    public void Atualizar(string nome, string unidade, decimal quantidade, decimal minimo)
    { Nome = nome.Trim(); Unidade = unidade.Trim(); QuantidadeDisponivel = quantidade; EstoqueMinimo = minimo; Versao++; }
    public void AlterarQuantidade(decimal variacao)
    {
        if (QuantidadeDisponivel + variacao < 0) throw new InvalidOperationException("A quantidade disponível não pode ficar negativa.");
        QuantidadeDisponivel += variacao;
        Versao++;
    }
    public void DefinirAtivo(bool ativo) => Ativo = ativo;
}
