using Microsoft.EntityFrameworkCore;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Persistence;

namespace Zalek360.Infrastructure.Queries;

/// <summary>Mapeamentos compartilhados entre as consultas (feitos em memória, após a projeção SQL).</summary>
internal static class Projecoes
{
    public static string NomeCliente(TipoPessoa tipo, string razao, string? fantasia) =>
        tipo == TipoPessoa.PessoaJuridica && !string.IsNullOrWhiteSpace(fantasia) ? fantasia! : razao;

    public static ClienteCardDto Card(Cliente c) => new(
        c.Id, c.NomeExibicao, c.TipoPessoa, c.Documento, c.ContatoNome, c.ContatoCargo, c.Telefone, c.WhatsApp, c.Email, c.CidadeUf);

    public static PersonalizacaoDto Personalizacao(ItemOrcamento i) => new(
        i.TipoPersonalizacao, i.CorPeca, i.CoresArte, i.Medidas, i.LocalAplicacao, i.ReferenciaArte, i.ObservacoesTecnicas);

    public static ItemDetalheDto Item(ItemOrcamento i) => new(
        i.Id, i.Ordem, i.ProdutoId, i.ProdutoNome, i.Descricao, i.Quantidade, i.ValorUnitario, i.ValorPersonalizacaoUnitario,
        i.SubtotalProduto, i.TotalPersonalizacao, i.ValorTotal, Personalizacao(i),
        i.Anexos.OrderBy(a => a.CriadoEm).Select(AnexoService.Mapear).ToList());

    public static ItemDetalheDto Item(ItemPedido i) => new(
        i.Id, i.Ordem, i.ProdutoId, i.ProdutoNome, i.Descricao, i.Quantidade, i.ValorUnitario, i.ValorPersonalizacaoUnitario,
        i.SubtotalProduto, i.TotalPersonalizacao, i.ValorTotal,
        new PersonalizacaoDto(i.TipoPersonalizacao, i.CorPeca, i.CoresArte, i.Medidas, i.LocalAplicacao, i.ReferenciaArte, i.ObservacoesTecnicas),
        i.Anexos.OrderBy(a => a.CriadoEm).Select(AnexoService.Mapear).ToList());

    /// <summary>Materializa eventos de histórico e resolve números de orçamento/pedido em uma segunda consulta.</summary>
    public static async Task<IReadOnlyList<HistoricoEventoDto>> HistoricoAsync(
        ZalekDbContext db, IQueryable<HistoricoEvento> consulta, bool crescente, CancellationToken ct)
    {
        var linhas = await consulta
            .Select(h => new
            {
                h.Id, h.Tipo, h.Escopo, h.Titulo, h.Descricao, h.MeioContato, h.OcorridoEm, h.RegistradoEm,
                h.OrcamentoId, h.PedidoId, h.SituacaoNova, h.UsuarioId,
                Autor = h.Usuario != null ? h.Usuario.Nome : null
            })
            .ToListAsync(ct);

        var idsOrcamentos = linhas.Where(l => l.OrcamentoId != null).Select(l => l.OrcamentoId!.Value).Distinct().ToList();
        var idsPedidos = linhas.Where(l => l.PedidoId != null).Select(l => l.PedidoId!.Value).Distinct().ToList();
        var numerosOrcamentos = idsOrcamentos.Count == 0 ? new Dictionary<Guid, int>() :
            await db.Orcamentos.AsNoTracking().Where(o => idsOrcamentos.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Numero, ct);
        var numerosPedidos = idsPedidos.Count == 0 ? new Dictionary<Guid, int>() :
            await db.Pedidos.AsNoTracking().Where(p => idsPedidos.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Numero, ct);

        // Desempate estável para eventos gravados no mesmo instante (ex.: decisão → pedido gerado → pedido criado).
        var ordenadas = crescente
            ? linhas.OrderBy(l => l.OcorridoEm).ThenBy(l => l.RegistradoEm).ThenBy(l => Prioridade(l.Tipo))
            : linhas.OrderByDescending(l => l.OcorridoEm).ThenByDescending(l => l.RegistradoEm).ThenByDescending(l => Prioridade(l.Tipo));

        return ordenadas.Select(l => new HistoricoEventoDto(
            l.Id, l.Tipo, CategoriasHistorico.De(l.Tipo, l.Escopo), l.Titulo, l.Descricao, l.MeioContato, l.OcorridoEm,
            l.Autor ?? "Sistema", l.UsuarioId is null,
            l.OrcamentoId, l.OrcamentoId is { } oid && numerosOrcamentos.TryGetValue(oid, out var no) ? no : null,
            l.PedidoId, l.PedidoId is { } pid && numerosPedidos.TryGetValue(pid, out var np) ? np : null,
            l.SituacaoNova)).ToList();
    }

    private static int Prioridade(TipoEventoHistorico tipo) => tipo switch
    {
        TipoEventoHistorico.ClienteCadastrado => 0,
        TipoEventoHistorico.ClienteAtualizado => 1,
        TipoEventoHistorico.OrcamentoCriado => 2,
        TipoEventoHistorico.QuantidadeAlterada => 3,
        TipoEventoHistorico.OrcamentoAlterado => 4,
        TipoEventoHistorico.OrcamentoStatusAlterado => 5,
        TipoEventoHistorico.ContatoRegistrado => 6,
        TipoEventoHistorico.DecisaoRegistrada => 7,
        TipoEventoHistorico.OrcamentoExpirado => 7,
        TipoEventoHistorico.PedidoGerado => 8,
        TipoEventoHistorico.PedidoCriado => 9,
        TipoEventoHistorico.PedidoStatusInicial => 10,
        _ => 11
    };

    public static int? NumeroDaBusca(string busca)
    {
        var digitos = new string(busca.Where(char.IsAsciiDigit).ToArray());
        return digitos.Length is > 0 and <= 9 && (busca.TrimStart().StartsWith('#') || digitos.Length == busca.Trim().Length)
            ? int.Parse(digitos)
            : null;
    }
}
