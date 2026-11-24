using Zalek360.Domain.Entities;

namespace Zalek360.Application.Dtos;

public sealed record SituacaoProdutoRequest(bool Ativo, int? Versao);
public sealed record DuplicarProdutoRequest(string? Nome);
public sealed record OpcaoCatalogoDto(Guid Id, TipoOpcaoCatalogo Tipo, string Nome, bool Ativo, int Ordem, int Versao);
public sealed record OpcaoCatalogoRequest(TipoOpcaoCatalogo Tipo, string? Nome, bool Ativo = true, int Ordem = 0, int? Versao = null);
