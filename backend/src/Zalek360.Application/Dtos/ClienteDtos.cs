using Zalek360.Domain.Enums;

namespace Zalek360.Application.Dtos;

public sealed record ClienteRequest(
    TipoPessoa? TipoPessoa,
    string? Documento,
    string? NomeRazaoSocial,
    string? NomeFantasia,
    string? ContatoNome,
    string? ContatoCargo,
    string? Telefone,
    string? WhatsApp,
    string? Email,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    /// <summary>Apenas no cadastro: vira a primeira observação interna do cliente.</summary>
    string? ObservacaoInicial,
    int? Versao);

public sealed record ClienteFiltro(string? Busca, TipoPessoa? Tipo, string? Cidade, int? Pagina, int? TamanhoPagina);

public sealed record UltimoOrcamentoResumoDto(Guid Id, int Numero, DateOnly Data);

public sealed record ClienteListaItemDto(
    Guid Id, TipoPessoa TipoPessoa, string Nome, string? RazaoSocial, string Documento,
    string? Telefone, string? WhatsApp, string? Email, string? Cidade, UltimoOrcamentoResumoDto? UltimoOrcamento);

public sealed record EnderecoDto(
    string? Cep, string? Logradouro, string? Numero, string? Complemento, string? Bairro, string? Cidade, string? Uf);

public sealed record ClienteDetalheDto(
    Guid Id, TipoPessoa TipoPessoa, string Documento, string NomeRazaoSocial, string? NomeFantasia, string NomeExibicao,
    string? ContatoNome, string? ContatoCargo, string? Telefone, string? WhatsApp, string? Email,
    EnderecoDto Endereco, DateTime CriadoEm, int TotalOrcamentos, int TotalPedidos, int Versao);

public sealed record ClienteExistenteDto(
    Guid Id, string Nome, TipoPessoa TipoPessoa, string Documento, string? Cidade, int? UltimoOrcamentoNumero);

public sealed record VerificacaoDocumentoDto(bool Valido, bool Disponivel, string? Mensagem, ClienteExistenteDto? ClienteExistente);

public sealed record OrcamentoResumoDto(
    Guid Id, int Numero, DateOnly Data, DateOnly Validade, SituacaoOrcamento Situacao, decimal ValorTotal,
    int QuantidadeItens, int TotalUnidades, string Responsavel);

public sealed record PedidoResumoDto(
    Guid Id, int Numero, DateOnly Data, DateOnly PrazoEntrega, SituacaoPedido Situacao, decimal ValorTotal,
    Guid OrcamentoId, int OrcamentoNumero, string Responsavel);

public sealed record ClienteVisaoGeralDto(
    int TotalOrcamentos, int OrcamentosAprovados, int TotalPedidos, decimal TotalEmPedidos,
    OrcamentoResumoDto? UltimoOrcamento, PedidoResumoDto? UltimoPedido, IReadOnlyList<HistoricoEventoDto> UltimasInteracoes);

public sealed record FiltroOrcamentosCliente(DateOnly? De, DateOnly? Ate, SituacaoOrcamento? Situacao);

public sealed record FiltroPedidosCliente(DateOnly? De, DateOnly? Ate, SituacaoPedido? Situacao);

/// <summary>Tipo: "orcamentos", "pedidos", "contatos", "decisoes" ou nulo (todos).</summary>
public sealed record FiltroHistoricoCliente(DateOnly? De, DateOnly? Ate, string? Tipo, SituacaoOrcamento? SituacaoOrcamento, SituacaoPedido? SituacaoPedido, int? Limite);

public sealed record ObservacaoRequest(string? Texto);

public sealed record ObservacaoClienteDto(Guid Id, string Texto, DateTime CriadoEm, string Autor, string AutorIniciais);
