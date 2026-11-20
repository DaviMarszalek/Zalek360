using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed class ProdutoService
{
    private readonly IProdutoRepository _produtos;
    private readonly IUnitOfWork _uow;

    public ProdutoService(IProdutoRepository produtos, IUnitOfWork uow)
    {
        _produtos = produtos;
        _uow = uow;
    }

    public async Task<ProdutoDto> CriarAsync(ProdutoRequest request, CancellationToken ct)
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Nome))
            erros["nome"] = ["Informe o nome do produto."];
        else if (request.Nome.Trim().Length > 100)
            erros["nome"] = ["Use até 100 caracteres."];

        if (string.IsNullOrWhiteSpace(request.Categoria))
            erros["categoria"] = ["Informe a categoria do produto."];
        else if (request.Categoria.Trim().Length > 50)
            erros["categoria"] = ["Use até 50 caracteres."];

        if (request.DescricaoBase?.Trim().Length > 500)
            erros["descricaoBase"] = ["Use até 500 caracteres."];

        if (erros.Count > 0)
            throw new ValidacaoException(CodigosErro.Validacao, "Corrija os campos destacados.", erros);

        var nome = request.Nome!.Trim();
        if (await _produtos.NomeProdutoExisteAsync(nome, ct))
            throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe um produto cadastrado com esse nome.",
                new Dictionary<string, string[]> { ["nome"] = ["Escolha um nome que ainda não esteja cadastrado."] });

        var produto = Produto.Criar(nome, request.Categoria!.Trim(), request.DescricaoBase);
        _produtos.Adicionar(produto);
        await _uow.SaveChangesAsync(ct);
        return new ProdutoDto(produto.Id, produto.Nome, produto.Categoria, produto.DescricaoBase);
    }
}
