using Zalek360.Domain.Enums;

namespace Zalek360.Application.Dtos;

public sealed record PaginaResultado<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina)
{
    public int TotalPaginas => TamanhoPagina <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(Total / (double)TamanhoPagina));
}

/// <summary>Lista paginada com a contagem por situação (abas "Todos 124 · Em Elaboração 7 ..." dos protótipos).</summary>
public sealed record ListaComContagemDto<T>(
    IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina, int TotalPaginas,
    IReadOnlyDictionary<string, int> ContagemPorSituacao);

public sealed record ListaFiltradaDto<T>(IReadOnlyList<T> Itens, int Total, int TotalSemFiltro);

public static class Paginacao
{
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    public static (int Pagina, int Tamanho) Normalizar(int? pagina, int? tamanho) =>
        (Math.Max(1, pagina ?? 1), Math.Clamp(tamanho ?? TamanhoPadrao, 1, TamanhoMaximo));
}

public sealed record UsuarioDto(Guid Id, string Nome, string Email, PerfilUsuario Perfil, string Iniciais);

public sealed record ProdutoDto(Guid Id, string Nome, string Categoria, string? DescricaoBase);

public sealed record AnexoDto(Guid Id, string Nome, string Extensao, string ContentType, long TamanhoBytes, bool EhImagem);

public sealed record ClienteCardDto(
    Guid Id, string Nome, TipoPessoa TipoPessoa, string Documento, string? ContatoNome, string? ContatoCargo,
    string? Telefone, string? WhatsApp, string? Email, string? Cidade);

public sealed record PersonalizacaoDto(
    string TipoPersonalizacao, string CorPeca, string? CoresArte, string? Medidas,
    string? LocalAplicacao, string? ReferenciaArte, string? ObservacoesTecnicas);

public sealed record ItemDetalheDto(
    Guid Id, int Ordem, Guid ProdutoId, string ProdutoNome, string? Descricao, int Quantidade,
    decimal ValorUnitario, decimal ValorPersonalizacaoUnitario, decimal SubtotalProduto,
    decimal TotalPersonalizacao, decimal ValorTotal, PersonalizacaoDto Personalizacao, IReadOnlyList<AnexoDto> Anexos);

public sealed record ResumoFinanceiroDto(
    int QuantidadeItens, int TotalUnidades, decimal SubtotalProdutos, decimal ValorPersonalizacao,
    decimal Desconto, decimal ValorTotal);

/// <summary>Evento do histórico. Categoria: "alteracao", "contato", "decisao" ou "pedido".</summary>
public sealed record HistoricoEventoDto(
    Guid Id, TipoEventoHistorico Tipo, string Categoria, string Titulo, string? Descricao, MeioContato? MeioContato,
    DateTime OcorridoEm, string Autor, bool Automatico,
    Guid? OrcamentoId, int? OrcamentoNumero, Guid? PedidoId, int? PedidoNumero, string? SituacaoNova);

public static class CategoriasHistorico
{
    public const string Alteracao = "alteracao";
    public const string Contato = "contato";
    public const string Decisao = "decisao";
    public const string Pedido = "pedido";

    public static string De(TipoEventoHistorico tipo, EscopoHistorico escopo) => tipo switch
    {
        TipoEventoHistorico.ContatoRegistrado => Contato,
        TipoEventoHistorico.DecisaoRegistrada or TipoEventoHistorico.OrcamentoExpirado => Decisao,
        TipoEventoHistorico.PedidoGerado => Pedido,
        _ when escopo == EscopoHistorico.Pedido => Pedido,
        _ => Alteracao
    };
}
