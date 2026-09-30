using Zalek360.Domain.Common;
using Zalek360.Domain.Services;

namespace Zalek360.Domain.Entities;

public sealed record DadosPersonalizacao(
    string? TipoPersonalizacao,
    string? CorPeca,
    string? CoresArte,
    string? Medidas,
    string? LocalAplicacao,
    string? ReferenciaArte,
    string? ObservacoesTecnicas);

public sealed record DadosItemOrcamento(
    Guid? Id,
    Guid ProdutoId,
    string ProdutoNome,
    string? Descricao,
    int Quantidade,
    decimal ValorUnitario,
    decimal ValorPersonalizacaoUnitario,
    DadosPersonalizacao Personalizacao,
    IReadOnlyList<ArquivoAnexo> Anexos);

/// <summary>RF 4.1.2.1 / 4.1.2.2 — item do orçamento com valores e especificação de personalização.</summary>
public class ItemOrcamento : Entidade
{
    public const decimal ValorMaximo = 99_999_999.99m;

    private readonly List<ArquivoAnexo> _anexos = new();

    public Guid OrcamentoId { get; private set; }
    public int Ordem { get; private set; }
    public Guid ProdutoId { get; private set; }
    /// <summary>Nome do produto no momento do orçamento (snapshot; preserva o histórico se o catálogo mudar).</summary>
    public string ProdutoNome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public int Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorPersonalizacaoUnitario { get; private set; }
    public decimal SubtotalProduto { get; private set; }
    public decimal TotalPersonalizacao { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string TipoPersonalizacao { get; private set; } = string.Empty;
    public string CorPeca { get; private set; } = string.Empty;
    public string? CoresArte { get; private set; }
    public string? Medidas { get; private set; }
    public string? LocalAplicacao { get; private set; }
    public string? ReferenciaArte { get; private set; }
    public string? ObservacoesTecnicas { get; private set; }

    public IReadOnlyCollection<ArquivoAnexo> Anexos => _anexos;

    private ItemOrcamento() { }

    public static IReadOnlyDictionary<string, string[]> Validar(DadosItemOrcamento d)
    {
        var e = new ColetorErros();
        if (d.ProdutoId == Guid.Empty) e.Adicionar("produtoId", "Selecione o produto.");
        if (d.Quantidade <= 0) e.Adicionar("quantidade", "Informe a quantidade.");
        else if (d.Quantidade > 1_000_000) e.Adicionar("quantidade", "Quantidade acima do limite permitido.");
        if (d.ValorUnitario <= 0) e.Adicionar("valorUnitario", "Informe o valor unitário.");
        if (d.ValorPersonalizacaoUnitario < 0) e.Adicionar("valorPersonalizacaoUnitario", "O valor de personalização não pode ser negativo.");
        if (d.Quantidade > 0 && d.Quantidade * (Math.Max(0, d.ValorUnitario) + Math.Max(0, d.ValorPersonalizacaoUnitario)) > ValorMaximo)
            e.Adicionar("quantidade", "O valor total do item excede o limite permitido.");
        if ((d.Descricao?.Trim().Length ?? 0) > 500) e.Adicionar("descricao", "Use no máximo 500 caracteres.");

        var p = d.Personalizacao;
        var tipo = TextoOpcional.Limpar(p.TipoPersonalizacao);
        if (tipo is null) e.Adicionar("tipoPersonalizacao", "Informe o tipo de personalização.");
        else if (tipo.Length > 100) e.Adicionar("tipoPersonalizacao", "Use no máximo 100 caracteres.");
        var cor = TextoOpcional.Limpar(p.CorPeca);
        if (cor is null) e.Adicionar("corPeca", "Informe a cor da peça.");
        else if (cor.Length > 50) e.Adicionar("corPeca", "Use no máximo 50 caracteres.");
        if ((TextoOpcional.Limpar(p.CoresArte)?.Length ?? 0) > 100) e.Adicionar("coresArte", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(p.Medidas)?.Length ?? 0) > 100) e.Adicionar("medidas", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(p.LocalAplicacao)?.Length ?? 0) > 100) e.Adicionar("localAplicacao", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(p.ReferenciaArte)?.Length ?? 0) > 500) e.Adicionar("referenciaArte", "Use no máximo 500 caracteres.");
        if ((TextoOpcional.Limpar(p.ObservacoesTecnicas)?.Length ?? 0) > 2000) e.Adicionar("observacoesTecnicas", "Use no máximo 2.000 caracteres.");
        if (d.Anexos.Count > 10) e.Adicionar("anexos", "Anexe no máximo 10 arquivos por item.");
        return e.ParaDicionario();
    }

    internal static ItemOrcamento Criar(Guid orcamentoId, int ordem, DadosItemOrcamento d)
    {
        var item = new ItemOrcamento { OrcamentoId = orcamentoId };
        item.Aplicar(ordem, d);
        item.SincronizarAnexos(d.Anexos);
        return item;
    }

    internal sealed record ResultadoAtualizacao(bool QuantidadeAlterada, int QuantidadeAnterior, decimal TotalAnterior, IReadOnlyList<string> OutrosCampos);

    internal ResultadoAtualizacao Atualizar(int ordem, DadosItemOrcamento d)
    {
        var anterior = (Quantidade, ValorTotal, ProdutoId, Descricao, ValorUnitario, ValorPersonalizacaoUnitario,
            TipoPersonalizacao, CorPeca, CoresArte, Medidas, LocalAplicacao, ReferenciaArte, ObservacoesTecnicas);
        Aplicar(ordem, d);
        var anexosAlterados = SincronizarAnexos(d.Anexos);

        var outros = new List<string>();
        if (anterior.ProdutoId != ProdutoId) outros.Add("produto");
        if (anterior.Descricao != Descricao) outros.Add("descrição");
        if (anterior.ValorUnitario != ValorUnitario) outros.Add("valor unitário");
        if (anterior.ValorPersonalizacaoUnitario != ValorPersonalizacaoUnitario) outros.Add("personalização/un.");
        if (anterior.TipoPersonalizacao != TipoPersonalizacao) outros.Add("tipo de personalização");
        if (anterior.CorPeca != CorPeca) outros.Add("cor da peça");
        if (anterior.CoresArte != CoresArte) outros.Add("cores da arte");
        if (anterior.Medidas != Medidas) outros.Add("medidas");
        if (anterior.LocalAplicacao != LocalAplicacao) outros.Add("local de aplicação");
        if (anterior.ReferenciaArte != ReferenciaArte) outros.Add("referência da arte");
        if (anterior.ObservacoesTecnicas != ObservacoesTecnicas) outros.Add("observações técnicas");
        if (anexosAlterados) outros.Add("artes anexadas");
        return new ResultadoAtualizacao(anterior.Quantidade != Quantidade, anterior.Quantidade, anterior.ValorTotal, outros);
    }

    internal void AplicarCalculo(ResultadoLinha r)
    {
        SubtotalProduto = r.SubtotalProduto;
        TotalPersonalizacao = r.TotalPersonalizacao;
        ValorTotal = r.ValorTotal;
    }

    internal LinhaCalculo ParaLinhaCalculo() => new(Quantidade, ValorUnitario, ValorPersonalizacaoUnitario);

    private void Aplicar(int ordem, DadosItemOrcamento d)
    {
        Ordem = ordem;
        ProdutoId = d.ProdutoId;
        ProdutoNome = d.ProdutoNome.Trim();
        Descricao = TextoOpcional.Limpar(d.Descricao);
        Quantidade = d.Quantidade;
        ValorUnitario = Dinheiro.Arredondar(d.ValorUnitario);
        ValorPersonalizacaoUnitario = Dinheiro.Arredondar(d.ValorPersonalizacaoUnitario);
        TipoPersonalizacao = d.Personalizacao.TipoPersonalizacao!.Trim();
        CorPeca = d.Personalizacao.CorPeca!.Trim();
        CoresArte = TextoOpcional.Limpar(d.Personalizacao.CoresArte);
        Medidas = TextoOpcional.Limpar(d.Personalizacao.Medidas);
        LocalAplicacao = TextoOpcional.Limpar(d.Personalizacao.LocalAplicacao);
        ReferenciaArte = TextoOpcional.Limpar(d.Personalizacao.ReferenciaArte);
        ObservacoesTecnicas = TextoOpcional.Limpar(d.Personalizacao.ObservacoesTecnicas);
    }

    /// <summary>Mantém somente os anexos informados. Anexos removidos voltam a ser pendentes e são limpos depois.</summary>
    private bool SincronizarAnexos(IReadOnlyList<ArquivoAnexo> anexos)
    {
        var ids = anexos.Select(a => a.Id).ToHashSet();
        var removidos = _anexos.Where(a => !ids.Contains(a.Id)).ToList();
        foreach (var r in removidos) _anexos.Remove(r);
        var alterou = removidos.Count > 0;
        foreach (var a in anexos)
        {
            if (_anexos.Any(x => x.Id == a.Id)) continue;
            a.VincularItemOrcamento(Id);
            _anexos.Add(a);
            alterou = true;
        }
        return alterou;
    }
}
