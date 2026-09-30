using Zalek360.Domain.Common;

namespace Zalek360.Domain.Services;

public readonly record struct LinhaCalculo(int Quantidade, decimal ValorUnitario, decimal ValorPersonalizacaoUnitario);

public sealed record ResultadoLinha(decimal SubtotalProduto, decimal TotalPersonalizacao, decimal ValorTotal);

public sealed record ResultadoCalculo(
    IReadOnlyList<ResultadoLinha> Linhas,
    int QuantidadeItens,
    int TotalUnidades,
    decimal SubtotalProdutos,
    decimal ValorPersonalizacao,
    decimal Desconto,
    decimal ValorBruto,
    decimal ValorTotal);

/// <summary>
/// RN4 / RF 4.1.2.3 — cálculo automático (fonte única de verdade, usada na criação, edição e pré-visualização):
/// Subtotal produto = quantidade × valor unitário; Personalização = quantidade × personalização/un.;
/// Total item = subtotal + personalização; Total = subtotal produtos + personalização − desconto.
/// </summary>
public static class CalculadoraOrcamento
{
    public static ResultadoCalculo Calcular(IEnumerable<LinhaCalculo> linhas, decimal desconto)
    {
        var resultados = new List<ResultadoLinha>();
        var unidades = 0;
        foreach (var l in linhas)
        {
            var qtd = Math.Max(0, l.Quantidade);
            var subtotal = Dinheiro.Arredondar(qtd * Math.Max(0, l.ValorUnitario));
            var personalizacao = Dinheiro.Arredondar(qtd * Math.Max(0, l.ValorPersonalizacaoUnitario));
            resultados.Add(new ResultadoLinha(subtotal, personalizacao, subtotal + personalizacao));
            unidades += qtd;
        }

        var subtotalProdutos = resultados.Sum(r => r.SubtotalProduto);
        var totalPersonalizacao = resultados.Sum(r => r.TotalPersonalizacao);
        var bruto = subtotalProdutos + totalPersonalizacao;
        var descontoAplicado = Dinheiro.Arredondar(Math.Max(0, desconto));
        return new ResultadoCalculo(
            resultados, resultados.Count, unidades, subtotalProdutos, totalPersonalizacao,
            descontoAplicado, bruto, bruto - descontoAplicado);
    }
}
