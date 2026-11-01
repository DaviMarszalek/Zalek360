using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Application.Services;

public sealed class EstoqueService
{
    private readonly IEstoqueRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IProdutoRepository _produtos;
    private readonly ICatalogoQueries _catalogo;
    private readonly IPedidoRepository _pedidos;
    private readonly IPedidoQueries _consultaPedidos;
    public EstoqueService(IEstoqueRepository repo, IUnitOfWork uow, IClock clock, IProdutoRepository produtos, ICatalogoQueries catalogo, IPedidoRepository pedidos, IPedidoQueries consultaPedidos)
    { _repo = repo; _uow = uow; _clock = clock; _produtos = produtos; _catalogo = catalogo; _pedidos = pedidos; _consultaPedidos = consultaPedidos; }

    public async Task<IReadOnlyList<MateriaPrimaDto>> ListarMateriasAsync(CancellationToken ct)
    {
        var materiais = await _repo.ListarMateriasAsync(ct);
        var compras = await _repo.ListarComprasAsync(ct);
        var reservas = await _repo.ListarReservasAsync(ct);
        var aReceber = compras.Where(x => x.Situacao is SituacaoCompraMateriaPrima.Planejada or SituacaoCompraMateriaPrima.Pedido)
            .GroupBy(x => x.MateriaPrimaId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        var reservada = reservas.Where(x => x.Situacao == SituacaoReservaMaterial.Reservada)
            .GroupBy(x => x.MateriaPrimaId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        return materiais.Select(x => new MateriaPrimaDto(x.Id, x.Nome, x.Unidade, x.QuantidadeDisponivel, x.EstoqueMinimo,
            aReceber.GetValueOrDefault(x.Id), reservada.GetValueOrDefault(x.Id), x.Ativo)).ToList();
    }

    public async Task<MateriaPrimaDto> CriarMateriaAsync(MateriaPrimaRequest r, CancellationToken ct)
    {
        ValidarMateria(r);
        if (await _repo.NomeMateriaExisteAsync(r.Nome!, null, ct)) throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe uma matéria-prima com esse nome.");
        var item = MateriaPrima.Criar(r.Nome!, r.Unidade!, r.QuantidadeDisponivel, r.EstoqueMinimo);
        _repo.Adicionar(item);
        if (r.QuantidadeDisponivel > 0) _repo.Adicionar(MovimentoMateriaPrima.Criar(item.Id, null, r.QuantidadeDisponivel,
            TipoMovimentoMateriaPrima.Ajuste, "Saldo inicial do cadastro", _clock.AgoraUtc));
        await _uow.SaveChangesAsync(ct);
        return new(item.Id, item.Nome, item.Unidade, item.QuantidadeDisponivel, item.EstoqueMinimo, 0, 0, item.Ativo);
    }

    public async Task<MateriaPrimaDto> AtualizarMateriaAsync(Guid id, MateriaPrimaRequest r, CancellationToken ct)
    {
        var item = await _repo.ObterMateriaAsync(id, ct) ?? throw new NaoEncontradoException("Matéria-prima");
        ValidarMateria(r);
        if (await _repo.NomeMateriaExisteAsync(r.Nome!, id, ct)) throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe uma matéria-prima com esse nome.");
        var diferenca = r.QuantidadeDisponivel - item.QuantidadeDisponivel;
        item.Atualizar(r.Nome!, r.Unidade!, r.QuantidadeDisponivel, r.EstoqueMinimo);
        if (diferenca != 0) _repo.Adicionar(MovimentoMateriaPrima.Criar(id, null, diferenca, TipoMovimentoMateriaPrima.Ajuste, "Ajuste manual do saldo", _clock.AgoraUtc));
        await _uow.SaveChangesAsync(ct);
        return (await ListarMateriasAsync(ct)).First(x => x.Id == id);
    }

    public async Task<MateriaPrimaDto> DefinirMateriaAtivaAsync(Guid id, bool ativo, CancellationToken ct)
    {
        var item = await _repo.ObterMateriaAsync(id, ct) ?? throw new NaoEncontradoException("Matéria-prima");
        item.DefinirAtivo(ativo); await _uow.SaveChangesAsync(ct);
        return (await ListarMateriasAsync(ct)).First(x => x.Id == id);
    }

    public async Task<IReadOnlyList<CompraMateriaPrimaDto>> ListarComprasAsync(CancellationToken ct) =>
        (await _repo.ListarComprasAsync(ct)).Select(MapearCompra).ToList();

    public async Task<CompraMateriaPrimaDto> CriarCompraAsync(CompraMateriaPrimaRequest r, CancellationToken ct)
    {
        if (r.Quantidade <= 0) throw new ValidacaoException(CodigosErro.Validacao, "Informe uma quantidade maior que zero.", new Dictionary<string, string[]> { ["quantidade"] = ["A quantidade deve ser maior que zero."] });
        if (r.DataPrevista == default) throw new ValidacaoException(CodigosErro.Validacao, "Informe a data prevista.", new Dictionary<string, string[]> { ["dataPrevista"] = ["Informe a data prevista para a compra."] });
        var materia = await _repo.ObterMateriaAsync(r.MateriaPrimaId, ct) ?? throw new NaoEncontradoException("Matéria-prima");
        if (!materia.Ativo) throw new ValidacaoException(CodigosErro.Validacao, "Reative a matéria-prima antes de planejar uma compra.", new Dictionary<string, string[]> { ["materiaPrimaId"] = ["Esta matéria-prima está inativa."] });
        var compra = CompraMateriaPrima.Criar(materia.Id, r.Quantidade, r.Fornecedor, r.DataPrevista, _clock.AgoraUtc);
        _repo.Adicionar(compra); await _uow.SaveChangesAsync(ct);
        return new(compra.Id, materia.Id, materia.Nome, materia.Unidade, compra.Quantidade, compra.Fornecedor, compra.DataPrevista, compra.Situacao, compra.CriadaEm);
    }

    public async Task<CompraMateriaPrimaDto> AtualizarSituacaoCompraAsync(Guid id, SituacaoCompraMateriaPrima nova, CancellationToken ct)
    {
        var compra = await _repo.ObterCompraAsync(id, ct) ?? throw new NaoEncontradoException("Compra planejada");
        if (compra.Situacao is SituacaoCompraMateriaPrima.Recebida or SituacaoCompraMateriaPrima.Cancelada)
            throw new ConflitoException(CodigosErro.ConflitoConcorrencia, "Esta compra já foi encerrada e não pode ser alterada.");
        if (nova is SituacaoCompraMateriaPrima.Recebida)
        {
            compra.MateriaPrima.AlterarQuantidade(compra.Quantidade);
            _repo.Adicionar(MovimentoMateriaPrima.Criar(compra.MateriaPrimaId, null, compra.Quantidade, TipoMovimentoMateriaPrima.EntradaCompra, $"Recebimento da compra {compra.Id}", _clock.AgoraUtc));
        }
        if (nova is not (SituacaoCompraMateriaPrima.Pedido or SituacaoCompraMateriaPrima.Recebida or SituacaoCompraMateriaPrima.Cancelada))
            throw new ValidacaoException(CodigosErro.Validacao, "Situação de compra inválida.", new Dictionary<string, string[]> { ["situacao"] = ["Escolha Pedido, Recebida ou Cancelada."] });
        compra.DefinirSituacao(nova); await _uow.SaveChangesAsync(ct);
        return MapearCompra(compra);
    }

    private static CompraMateriaPrimaDto MapearCompra(CompraMateriaPrima x) => new(x.Id, x.MateriaPrimaId, x.MateriaPrima.Nome,
        x.MateriaPrima.Unidade, x.Quantidade, x.Fornecedor, x.DataPrevista, x.Situacao, x.CriadaEm);
    private static void ValidarMateria(MateriaPrimaRequest r)
    {
        var e = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(r.Nome)) e["nome"] = ["Informe o nome da matéria-prima."];
        else if (r.Nome.Trim().Length > 120) e["nome"] = ["Use até 120 caracteres."];
        if (string.IsNullOrWhiteSpace(r.Unidade)) e["unidade"] = ["Informe a unidade (un, kg, m, L etc.)."];
        else if (r.Unidade.Trim().Length > 20) e["unidade"] = ["Use até 20 caracteres."];
        if (r.QuantidadeDisponivel < 0) e["quantidadeDisponivel"] = ["O saldo não pode ser negativo."];
        if (r.EstoqueMinimo < 0) e["estoqueMinimo"] = ["O mínimo não pode ser negativo."];
        if (e.Count > 0) throw new ValidacaoException(CodigosErro.Validacao, "Corrija os campos destacados.", e);
    }

    public async Task<IReadOnlyList<FichaTecnicaDto>> ListarFichasAsync(CancellationToken ct)
    {
        var fichas = await _repo.ListarFichasAsync(ct);
        var produtos = await _catalogo.ListarProdutosAsync(ct);
        var materiais = await _repo.ListarMateriasAsync(ct);
        var mapaProduto = produtos.ToDictionary(x => x.Id);
        var mapaMaterial = materiais.ToDictionary(x => x.Id);
        return fichas.Where(x => mapaProduto.ContainsKey(x.ProdutoId) && mapaMaterial.ContainsKey(x.MateriaPrimaId))
            .GroupBy(x => x.ProdutoId).Select(g => new FichaTecnicaDto(g.Key, mapaProduto[g.Key].Nome,
                g.Select(x => new FichaTecnicaLinhaDto(x.MateriaPrimaId, mapaMaterial[x.MateriaPrimaId].Nome, mapaMaterial[x.MateriaPrimaId].Unidade, x.QuantidadePorUnidade)).ToList()))
            .OrderBy(x => x.ProdutoNome).ToList();
    }

    public async Task<FichaTecnicaDto> SalvarFichaAsync(FichaTecnicaRequest r, CancellationToken ct)
    {
        var produto = (await _produtos.ObterPorIdsAsync(new[] { r.ProdutoId }, ct)).FirstOrDefault() ?? throw new NaoEncontradoException("Produto");
        if (!produto.Ativo) throw new ValidacaoException(CodigosErro.Validacao, "Produto inativo.", new Dictionary<string, string[]> { ["produtoId"] = ["Ative o produto antes de configurar a ficha técnica."] });
        var linhas = r.Materiais ?? Array.Empty<FichaTecnicaLinhaRequest>();
        if (linhas.Any(x => x.QuantidadePorUnidade <= 0) || linhas.Select(x => x.MateriaPrimaId).Distinct().Count() != linhas.Count)
            throw new ValidacaoException(CodigosErro.Validacao, "Revise os materiais e quantidades da ficha.", new Dictionary<string, string[]> { ["materiais"] = ["Cada material deve aparecer uma vez e ter consumo maior que zero."] });
        var materiais = (await _repo.ListarMateriasAsync(ct)).ToDictionary(x => x.Id);
        if (linhas.Any(x => !materiais.TryGetValue(x.MateriaPrimaId, out var m) || !m.Ativo))
            throw new ValidacaoException(CodigosErro.Validacao, "A ficha usa material inativo ou inexistente.", new Dictionary<string, string[]> { ["materiais"] = ["Selecione apenas matérias-primas ativas."] });
        var antigas = await _repo.ListarFichasPorProdutoAsync(r.ProdutoId, ct);
        _repo.RemoverFichas(antigas);
        foreach (var linha in linhas) _repo.Adicionar(FichaTecnicaMaterial.Criar(r.ProdutoId, linha.MateriaPrimaId, linha.QuantidadePorUnidade));
        await _uow.SaveChangesAsync(ct);
        var mapa = linhas.Select(x => new FichaTecnicaLinhaDto(x.MateriaPrimaId, materiais[x.MateriaPrimaId].Nome, materiais[x.MateriaPrimaId].Unidade, x.QuantidadePorUnidade)).ToList();
        return new(produto.Id, produto.Nome, mapa);
    }

    /// <summary>Snapshot das quantidades previstas da ficha técnica no momento da aprovação do orçamento.</summary>
    public async Task GerarReservasPedidoAsync(Pedido pedido, CancellationToken ct)
    {
        var itens = pedido.Itens.ToList();
        await CriarReservasParaItensAsync(pedido.Id, itens.Select(x => (x.ProdutoId, x.Quantidade)).ToList(), ct);
    }

    public async Task<IReadOnlyList<ReservaPedidoDto>> AplicarFichaPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await _pedidos.ObterParaAlteracaoAsync(pedidoId, ct) ?? throw new NaoEncontradoException("Pedido");
        if (pedido.Situacao != SituacaoPedido.Aberto) throw new ConflitoException(CodigosErro.PedidoTransicaoInvalida, "A ficha técnica só pode ser aplicada enquanto o pedido aguarda produção.");
        if ((await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Any(x => x.Situacao == SituacaoReservaMaterial.Reservada))
            throw new ConflitoException(CodigosErro.RegistroRelacionado, "Este pedido já tem materiais reservados. Ajuste as quantidades manualmente se houver consumo variável.");
        var detalhe = await _consultaPedidos.ObterDetalheAsync(pedidoId, ct);
        var itens = detalhe.Itens.Select(x => (x.ProdutoId, x.Quantidade)).ToList();
        var criadas = await CriarReservasParaItensAsync(pedidoId, itens, ct);
        if (criadas == 0) throw new ValidacaoException(CodigosErro.Validacao, "Não há fichas técnicas completas para os produtos deste pedido.", new Dictionary<string, string[]> { ["materiais"] = ["Configure a ficha técnica de todos os produtos ou informe o consumo manualmente."] });
        await _uow.SaveChangesAsync(ct);
        return await ListarReservasPedidoAsync(pedidoId, ct);
    }

    private async Task<int> CriarReservasParaItensAsync(Guid pedidoId, IReadOnlyList<(Guid ProdutoId, int Quantidade)> itens, CancellationToken ct)
    {
        var fichas = await _repo.ListarFichasPorProdutosAsync(itens.Select(x => x.ProdutoId).Distinct().ToList(), ct);
        // Um pedido misto não pode baixar apenas parte dos materiais necessários sem avisar.
        if (itens.Any(item => fichas.All(ficha => ficha.ProdutoId != item.ProdutoId))) return 0;
        var quantidades = (from item in itens
                           join ficha in fichas on item.ProdutoId equals ficha.ProdutoId
                           group item.Quantidade * ficha.QuantidadePorUnidade by ficha.MateriaPrimaId into g
                           select new { MaterialId = g.Key, Quantidade = g.Sum() }).ToList();
        foreach (var linha in quantidades) _repo.Adicionar(ReservaMateriaPrimaPedido.Criar(pedidoId, linha.MaterialId, linha.Quantidade, _clock.AgoraUtc));
        return quantidades.Count;
    }

    public async Task<IReadOnlyList<ReservaPedidoDto>> ListarReservasPedidoAsync(Guid pedidoId, CancellationToken ct) =>
        (await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Select(MapearReserva).ToList();

    public async Task<IReadOnlyList<MovimentoMateriaPrimaDto>> ListarMovimentosAsync(Guid materiaPrimaId, CancellationToken ct)
    {
        if (await _repo.ObterMateriaAsync(materiaPrimaId, ct) is null) throw new NaoEncontradoException("Matéria-prima");
        return (await _repo.ListarMovimentosAsync(materiaPrimaId, ct)).Select(x => new MovimentoMateriaPrimaDto(x.Id, x.MateriaPrimaId,
            x.MateriaPrima.Nome, x.PedidoId, x.Pedido?.Numero, x.Variacao, x.Tipo, x.Observacao, x.CriadoEm)).ToList();
    }

    public async Task<IReadOnlyList<ReservaPedidoDto>> AtualizarReservasPedidoAsync(Guid pedidoId, IReadOnlyList<ReservaPedidoRequest> linhas, CancellationToken ct)
    {
        var pedido = await _pedidos.ObterParaAlteracaoAsync(pedidoId, ct) ?? throw new NaoEncontradoException("Pedido");
        if (pedido.Situacao != SituacaoPedido.Aberto) throw new ConflitoException(CodigosErro.PedidoTransicaoInvalida, "Os materiais só podem ser ajustados enquanto o pedido estiver aguardando produção.");
        if (linhas.Any(x => x.Quantidade <= 0) || linhas.Select(x => x.MateriaPrimaId).Distinct().Count() != linhas.Count)
            throw new ValidacaoException(CodigosErro.Validacao, "Revise os materiais do pedido.", new Dictionary<string, string[]> { ["materiais"] = ["Cada material deve aparecer uma vez e ter quantidade maior que zero."] });
        var catalogo = (await _repo.ListarMateriasAsync(ct)).ToDictionary(x => x.Id);
        if (linhas.Any(x => !catalogo.TryGetValue(x.MateriaPrimaId, out var m) || !m.Ativo))
            throw new ValidacaoException(CodigosErro.Validacao, "Selecione apenas matérias-primas ativas.", new Dictionary<string, string[]> { ["materiais"] = ["Há material inativo ou inexistente na lista."] });
        var existentes = await _repo.ListarReservasPedidoAsync(pedidoId, ct);
        var requisitadas = linhas.ToDictionary(x => x.MateriaPrimaId, x => x.Quantidade);
        foreach (var reserva in existentes.Where(x => x.Situacao == SituacaoReservaMaterial.Reservada))
        {
            if (requisitadas.Remove(reserva.MateriaPrimaId, out var qtd)) reserva.Ajustar(qtd);
            else reserva.DefinirSituacao(SituacaoReservaMaterial.Liberada);
        }
        foreach (var (materialId, quantidade) in requisitadas) _repo.Adicionar(ReservaMateriaPrimaPedido.Criar(pedidoId, materialId, quantidade, _clock.AgoraUtc));
        await _uow.SaveChangesAsync(ct);
        return await ListarReservasPedidoAsync(pedidoId, ct);
    }

    public async Task ConsumirReservasPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var reservas = (await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Where(x => x.Situacao == SituacaoReservaMaterial.Reservada).ToList();
        if (reservas.Count == 0) throw new ValidacaoException(CodigosErro.Validacao, "Informe os materiais necessários neste pedido antes de iniciar a produção.", new Dictionary<string, string[]> { ["materiais"] = ["Este pedido não tem materiais reservados. Cadastre a ficha técnica do produto ou informe o consumo neste pedido."] });
        var falta = reservas.Where(x => x.MateriaPrima.QuantidadeDisponivel < x.Quantidade).ToList();
        if (falta.Count > 0)
            throw new ConflitoException(CodigosErro.RegistroRelacionado, "Não há saldo físico suficiente para iniciar a produção. Receba ou ajuste o estoque dos materiais em falta.",
                new Dictionary<string, string[]> { ["materiais"] = falta.Select(x => $"{x.MateriaPrima.Nome}: precisa {x.Quantidade}, disponível {x.MateriaPrima.QuantidadeDisponivel} {x.MateriaPrima.Unidade}.").ToArray() });
        foreach (var reserva in reservas)
        {
            reserva.MateriaPrima.AlterarQuantidade(-reserva.Quantidade);
            reserva.DefinirSituacao(SituacaoReservaMaterial.Consumida);
            _repo.Adicionar(MovimentoMateriaPrima.Criar(reserva.MateriaPrimaId, pedidoId, -reserva.Quantidade,
                TipoMovimentoMateriaPrima.SaidaPedido, "Consumo ao iniciar a produção", _clock.AgoraUtc));
        }
    }

    public async Task LiberarReservasPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var reservas = await _repo.ListarReservasPedidoAsync(pedidoId, ct);
        foreach (var reserva in reservas.Where(x => x.Situacao == SituacaoReservaMaterial.Reservada))
            reserva.DefinirSituacao(SituacaoReservaMaterial.Liberada);
    }

    private static ReservaPedidoDto MapearReserva(ReservaMateriaPrimaPedido x) => new(x.Id, x.MateriaPrimaId,
        x.MateriaPrima.Nome, x.MateriaPrima.Unidade, x.Quantidade, x.Situacao.ToString());
}
