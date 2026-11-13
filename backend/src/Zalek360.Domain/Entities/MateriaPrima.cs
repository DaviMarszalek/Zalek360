using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

public sealed class MateriaPrima : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public string Unidade { get; private set; } = string.Empty;
    public decimal QuantidadeDisponivel { get; private set; }
    /// <summary>Saldo fisico comprometido por reservas ativas. QuantidadeDisponivel continua sendo o fisico legado.</summary>
    public decimal SaldoReservado { get; private set; }
    public decimal SaldoDisponivel => QuantidadeDisponivel - SaldoReservado;
    public decimal EstoqueMinimo { get; private set; }
    public bool Ativo { get; private set; } = true;
    public int Versao { get; private set; }

    private MateriaPrima() { }
    public static MateriaPrima Criar(string nome, string unidade, decimal quantidade, decimal minimo) => new()
    { Nome = nome.Trim(), Unidade = unidade.Trim(), QuantidadeDisponivel = quantidade, EstoqueMinimo = minimo };
    public void Atualizar(string nome, string unidade, decimal quantidade, decimal minimo)
    {
        GarantirSaldo(quantidade);
        Nome = nome.Trim(); Unidade = unidade.Trim(); QuantidadeDisponivel = quantidade; EstoqueMinimo = minimo; Versao++;
    }
    public void AlterarQuantidade(decimal variacao)
    {
        GarantirSaldo(QuantidadeDisponivel + variacao);
        QuantidadeDisponivel += variacao;
        Versao++;
    }
    public void AlterarReserva(decimal variacao)
    {
        if (SaldoReservado + variacao < 0 || SaldoReservado + variacao > QuantidadeDisponivel)
            throw new DomainException(CodigosErro.RegistroRelacionado, "A reserva deve respeitar o saldo disponível.", CategoriaErro.Conflito);
        SaldoReservado += variacao;
        if (variacao != 0) Versao++;
    }
    public void ConsumirReserva(decimal quantidade)
    {
        if (quantidade < 0 || quantidade > SaldoReservado)
            throw new DomainException(CodigosErro.RegistroRelacionado, "A baixa deve usar somente a reserva do pedido.", CategoriaErro.Conflito);
        SaldoReservado -= quantidade;
        QuantidadeDisponivel -= quantidade;
        Versao++;
    }
    private void GarantirSaldo(decimal fisico)
    {
        if (fisico < SaldoReservado || fisico < 0)
            throw new DomainException(CodigosErro.RegistroRelacionado, "O saldo físico não pode ser menor que o saldo reservado.", CategoriaErro.Conflito);
    }
    public void DefinirAtivo(bool ativo)
    {
        if (!ativo && SaldoReservado > 0)
            throw new DomainException(CodigosErro.RegistroRelacionado, "Libere as reservas antes de desativar o material.", CategoriaErro.Conflito);
        Ativo = ativo;
        Versao++;
    }
}
