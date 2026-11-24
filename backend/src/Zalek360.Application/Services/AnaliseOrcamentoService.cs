using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed class AnaliseOrcamentoService(IProdutoRepository produtos, IEstoqueRepository estoque, IAnaliseOrcamentoQueries consultas)
{
    public async Task<AnaliseOrcamentoDto> ObterAsync(Guid id, CancellationToken ct)
    {
        var o = await consultas.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
        return await CalcularAsync(new(id, o.Itens.Select(x => new ItemAnaliseRequest(x.Id, x.ProdutoId, x.Quantidade, x.ValorUnitario, x.ValorPersonalizacaoUnitario)).ToList(), o.Desconto), ct);
    }

    public async Task<AnaliseOrcamentoDto> CalcularAsync(AnaliseOrcamentoRequest r, CancellationToken ct)
    {
        var linhas = r.Itens ?? Array.Empty<ItemAnaliseRequest>();
        if (linhas.Count > 1000 || r.Desconto is < 0 or > ItemOrcamento.ValorMaximo || linhas.Any(x => x.Quantidade is < 0 or > 1000000 ||
                x.ValorUnitario is < 0 or > ItemOrcamento.ValorMaximo || x.ValorPersonalizacaoUnitario is < 0 or > ItemOrcamento.ValorMaximo))
            throw new ValidacaoException(CodigosErro.Validacao, "Revise as quantidades e os valores para consultar a análise.", new Dictionary<string, string[]>());
        var registrado = r.OrcamentoId.HasValue ? await consultas.ObterAsync(r.OrcamentoId.Value, ct) ?? throw new NaoEncontradoException("Orçamento") : null;
        var anteriores = registrado?.Itens.ToDictionary(x => x.Id) ?? new Dictionary<Guid, ItemAnaliseRegistrado>();
        if (linhas.Any(x => x.Id.HasValue && !anteriores.ContainsKey(x.Id.Value)))
            throw new ValidacaoException(CodigosErro.Validacao, "Um item não pertence a este orçamento.", new Dictionary<string, string[]>());
        var ids = linhas.Where(x => x.ProdutoId.HasValue).Select(x => x.ProdutoId!.Value).Distinct().ToArray();
        var catalogo = (await produtos.ObterPorIdsAsync(ids, ct)).ToDictionary(x => x.Id);
        var fichas = (await estoque.ListarFichasPorProdutosAsync(ids, ct)).ToLookup(x => x.ProdutoId);
        var materiais = (await estoque.ListarMateriasAsync(ct)).ToDictionary(x => x.Id);
        var livres = materiais.ToDictionary(x => x.Key, x => x.Value.Ativo ? Math.Max(0, x.Value.SaldoDisponivel) : 0);
        if (registrado?.PedidoId is { } pedidoId)
            foreach (var reserva in await estoque.ListarReservasPedidoAsync(pedidoId, ct))
                if (reserva.Situacao == SituacaoReservaMaterial.Reservada && livres.ContainsKey(reserva.MateriaPrimaId) && materiais[reserva.MateriaPrimaId].Ativo)
                    livres[reserva.MateriaPrimaId] += reserva.QuantidadeReservada;
        var itens = new List<ItemAnaliseDto>();
        var completa = linhas.Count > 0;
        for (var i = 0; i < linhas.Count; i++)
        {
            var l = linhas[i];
            var p = l.ProdutoId.HasValue ? catalogo.GetValueOrDefault(l.ProdutoId.Value) : null;
            var anterior = l.Id.HasValue ? anteriores.GetValueOrDefault(l.Id.Value) : null;
            var custo = anterior is not null && anterior.ProdutoId == l.ProdutoId ? anterior.CustoUnitarioInterno : p?.Custo;
            var qtd = l.Quantidade ?? 0;
            var custoTotal = custo.HasValue && qtd > 0 ? Dinheiro.Arredondar(custo.Value * qtd) : (decimal?)null;
            completa &= p is not null && custoTotal.HasValue && l.ValorUnitario > 0 && l.ValorPersonalizacaoUnitario is >= 0;
            var necessidades = new List<MaterialDisponibilidadeDto>();
            var ficha = p is null ? [] : fichas[p.Id].ToArray();
            foreach (var f in ficha)
            {
                if (!materiais.TryGetValue(f.MateriaPrimaId, out var m)) continue;
                var necessario = qtd * f.QuantidadePorUnidade;
                var disponivel = livres[m.Id];
                necessidades.Add(new(m.Id, m.Nome, m.Unidade, necessario, disponivel, Math.Max(0, necessario - disponivel), m.Ativo));
                livres[m.Id] = Math.Max(0, disponivel - necessario);
            }
            itens.Add(new(i, l.Id, l.ProdutoId, custo, custoTotal, p is not null && ficha.Length == 0, necessidades));
        }
        var calculo = OrcamentoService.Calcular(new(linhas.Select(x => new CalculoItemRequest(x.Quantidade, x.ValorUnitario, x.ValorPersonalizacaoUnitario)).ToList(), r.Desconto));
        completa &= !calculo.DescontoExcedeValor;
        decimal? totalCusto = completa ? itens.Sum(x => x.CustoTotal!.Value) : null;
        var margem = totalCusto.HasValue ? Dinheiro.Arredondar(calculo.ValorTotal - totalCusto.Value) : (decimal?)null;
        var percentual = margem.HasValue && calculo.ValorTotal > 0 ? decimal.Round(margem.Value / calculo.ValorTotal * 100, 2, MidpointRounding.AwayFromZero) : (decimal?)null;
        return new(calculo.ValorTotal, totalCusto, margem, percentual, completa, itens);
    }
}
