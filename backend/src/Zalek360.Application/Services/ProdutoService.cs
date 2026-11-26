using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed class ProdutoService(IProdutoRepository produtos, IGestaoCatalogoRepository gestao,
    IEstoqueRepository estoque, IUnitOfWork uow, ConfiguracaoCatalogoService configuracoes)
{
    public Task<IReadOnlyList<ProdutoDto>> ListarAsync(string? busca, bool inativos, CancellationToken ct) => gestao.ListarAsync(busca, inativos, ct);
    public async Task<ProdutoDto> ObterAsync(Guid id, CancellationToken ct) =>
        (await gestao.ListarAsync(null, true, ct, id)).FirstOrDefault() ?? throw new NaoEncontradoException("Produto");

    /// <summary>Regra única de dinheiro: valores com mais de 2 casas são arredondados (ponto médio afastando de zero).</summary>
    private static ProdutoRequest ArredondarValores(ProdutoRequest r) => r with
    {
        PrecoVenda = r.PrecoVenda.HasValue ? Dinheiro.Arredondar(r.PrecoVenda.Value) : null,
        Custo = r.Custo.HasValue ? Dinheiro.Arredondar(r.Custo.Value) : null
    };

    public async Task<ProdutoDto> CriarAsync(ProdutoRequest r, CancellationToken ct)
    {
        r = ArredondarValores(r);
        await using var tx = await uow.IniciarTransacaoAsync(ct);
        await ValidarAsync(r, null, ct);
        var p = Produto.Criar(r.Nome!, r.Categoria!, r.DescricaoBase);
        p.Atualizar(r.Nome!, r.Categoria!, r.DescricaoBase, r.PrecoVenda, r.Custo);
        produtos.Adicionar(p);
        if (r.Materiais is not null) await ComposicaoAsync(p.Id, r.Materiais, ct);
        await uow.SaveChangesAsync(ct);
        await tx.ConfirmarAsync(ct);
        return await ObterAsync(p.Id, ct);
    }
    public async Task<ProdutoDto> AtualizarAsync(Guid id, ProdutoRequest r, CancellationToken ct)
    {
        r = ArredondarValores(r);
        await using var tx = await uow.IniciarTransacaoAsync(ct);
        var p = await gestao.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Produto");
        Concorrencia.VerificarVersao(p.Versao, r.Versao);
        await ValidarAsync(r, p, ct);
        p.Atualizar(r.Nome!, r.Categoria!, r.DescricaoBase, r.PrecoVenda, r.Custo);
        if (r.Materiais is not null) await ComposicaoAsync(id, r.Materiais, ct);
        await uow.SaveChangesAsync(ct);
        await tx.ConfirmarAsync(ct);
        return await ObterAsync(id, ct);
    }
    public async Task<ProdutoDto> SituacaoAsync(Guid id, SituacaoProdutoRequest r, CancellationToken ct)
    {
        var p = await gestao.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Produto");
        Concorrencia.VerificarVersao(p.Versao, r.Versao);
        p.DefinirAtivo(r.Ativo);
        await uow.SaveChangesAsync(ct);
        return await ObterAsync(id, ct);
    }
    public async Task<ProdutoDto> DuplicarAsync(Guid id, DuplicarProdutoRequest r, CancellationToken ct)
    {
        var origem = await ObterAsync(id, ct);
        return await CriarAsync(new ProdutoRequest(r.Nome, origem.Categoria, origem.DescricaoBase, origem.PrecoVenda, origem.Custo,
            origem.Materiais!.Select(x => new FichaTecnicaLinhaRequest(x.MateriaPrimaId, x.QuantidadePorUnidade)).ToList()), ct);
    }
    public async Task RemoverAsync(Guid id, int? versao, CancellationToken ct)
    {
        await using var tx = await uow.IniciarTransacaoAsync(ct);
        var p = await gestao.ObterAsync(id, ct) ?? throw new NaoEncontradoException("Produto");
        Concorrencia.VerificarVersao(p.Versao, versao);
        if (await gestao.UsadoAsync(id, ct))
            throw new ConflitoException(CodigosErro.RegistroRelacionado, "Este produto já foi usado. Inative-o para preservar o histórico.");
        estoque.RemoverFichas(await estoque.ListarFichasPorProdutoAsync(id, ct));
        gestao.Remover(p);
        await uow.SaveChangesAsync(ct);
        await tx.ConfirmarAsync(ct);
    }
    private async Task ValidarAsync(ProdutoRequest r, Produto? atual, CancellationToken ct)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(r.Nome) || r.Nome.Trim().Length > 100) e["nome"] = ["Informe um nome com até 100 caracteres."];
        if (string.IsNullOrWhiteSpace(r.Categoria) || r.Categoria.Trim().Length > 50) e["categoria"] = ["Informe uma categoria com até 50 caracteres."];
        if (r.DescricaoBase?.Trim().Length > 500) e["descricaoBase"] = ["Use até 500 caracteres."];
        foreach (var (valor, campo) in new[] { (r.PrecoVenda, "precoVenda"), (r.Custo, "custo") })
            if (valor.HasValue && (valor < 0 || valor > 9999999999.99m))
                e[campo] = ["Informe um valor não negativo e no máximo 9.999.999.999,99."];
        if (e.Count > 0) throw new ValidacaoException(CodigosErro.Validacao, "Corrija os campos destacados.", e);
        if (atual?.Categoria != r.Categoria!.Trim() && !(await configuracoes.NomesAsync(TipoOpcaoCatalogo.CategoriaProduto, ct)).Contains(r.Categoria.Trim()))
            throw new ValidacaoException(CodigosErro.Validacao, "Selecione uma categoria ativa nas Configurações.", new Dictionary<string, string[]> { ["categoria"] = ["Categoria inativa ou não cadastrada."] });
        if (await gestao.NomeExisteAsync(r.Nome!, atual?.Id, ct))
            throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe um produto cadastrado com esse nome.", new Dictionary<string, string[]> { ["nome"] = ["Escolha um nome que ainda não esteja cadastrado."] });
    }
    private async Task ComposicaoAsync(Guid id, IReadOnlyList<FichaTecnicaLinhaRequest> linhas, CancellationToken ct)
    {
        var antigas = await estoque.ListarFichasPorProdutoAsync(id, ct);
        var materias = (await estoque.ListarMateriasAsync(ct)).ToDictionary(x => x.Id);
        if (linhas.Any(x => x.QuantidadePorUnidade <= 0 || x.QuantidadePorUnidade > 999999999m || decimal.Truncate(x.QuantidadePorUnidade) != x.QuantidadePorUnidade ||
                !materias.TryGetValue(x.MateriaPrimaId, out var m) || !m.Ativo && !antigas.Any(a => a.MateriaPrimaId == x.MateriaPrimaId && a.QuantidadePorUnidade == x.QuantidadePorUnidade)) ||
            linhas.Select(x => x.MateriaPrimaId).Distinct().Count() != linhas.Count)
            throw new ValidacaoException(CodigosErro.Validacao, "Revise a composição.", new Dictionary<string, string[]> { ["materiais"] = ["Use materiais ativos, sem repetição, com quantidade inteira entre 1 e 999999999."] });
        estoque.RemoverFichas(antigas);
        foreach (var linha in linhas) estoque.Adicionar(FichaTecnicaMaterial.Criar(id, linha.MateriaPrimaId, linha.QuantidadePorUnidade));
    }
}
