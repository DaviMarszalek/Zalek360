namespace Zalek360.Application.Dtos;

// Contrato próprio e público: não carrega decisões, contatos internos, histórico ou IDs técnicos.
public sealed record CampoDocumentoDto(string Rotulo, string Valor);
public sealed record ItemDocumentoDto(
    string Numero, string Produto, string? Descricao, string Quantidade, string ValorUnitario,
    string PersonalizacaoUnitaria, string Total, IReadOnlyList<CampoDocumentoDto> Personalizacao,
    IReadOnlyList<string> Artes);
public sealed record OrcamentoDocumentoDto(
    string NomeArquivo, string Numero, string Emissao, string GeradoEm, string Empresa,
    string? CaminhoLogo, IReadOnlyList<string> ContatosEmpresa, IReadOnlyList<string> Rodape,
    string Cliente, IReadOnlyList<CampoDocumentoDto> DadosCliente, string Responsavel,
    string? Selo, bool Rascunho, string Validade, string PrazoDestaque,
    IReadOnlyList<string> TextosPrazo, string NotaPrazo, string CondicaoPagamento,
    string? ObservacoesComerciais, IReadOnlyList<ItemDocumentoDto> Itens,
    string SubtotalProdutos, string Personalizacao, string? Desconto, string Total,
    IReadOnlyList<string> PassosAprovacao, string ParaAprovar, bool PodeAprovar);

/// <summary>Rótulos definidos pela aplicação; infraestrutura apenas os posiciona.</summary>
public static class RotulosDocumento
{
    public const string Titulo = "Proposta Comercial";
    public const string Numero = "Orçamento nº";
    public const string Total = "VALOR TOTAL";
    public const string Validade = "VALIDADE DA PROPOSTA";
    public const string ValidadeCondicoes = "Validade da proposta";
    public const string Prazo = "PRAZO DE ENTREGA";
    public const string Cliente = "Dados do cliente";
    public const string Responsavel = "Atendente responsável";
    public const string Itens = "Itens do orçamento";
    public static readonly string[] Colunas = { "Nº", "Produto / descrição", "Qtd.", "Unitário", "Pers./un.", "Total" };
    public const string Personalizacao = "Personalização";
    public const string Artes = "Artes anexadas";
    public const string Resumo = "Resumo financeiro";
    public const string Produtos = "Subtotal dos produtos";
    public const string Desconto = "Desconto";
    public const string Condicoes = "Condições";
    public const string Pagamento = "Pagamento";
    public const string AposAprovacao = "Como funciona depois da aprovação";
    public const string ParaAprovar = "Para aprovar";
    public const string Pagina = "Página ";
    public const string DePaginas = " de ";
}
