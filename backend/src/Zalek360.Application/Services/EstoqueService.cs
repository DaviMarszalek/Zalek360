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
    private readonly IUsuarioAtual _usuario;
    public EstoqueService(IEstoqueRepository repo, IUnitOfWork uow, IClock clock, IProdutoRepository produtos, ICatalogoQueries catalogo, IPedidoRepository pedidos, IPedidoQueries consultaPedidos, IUsuarioAtual usuario)
    { _repo = repo; _uow = uow; _clock = clock; _produtos = produtos; _catalogo = catalogo; _pedidos = pedidos; _consultaPedidos = consultaPedidos; _usuario = usuario; }

    public async Task<IReadOnlyList<MateriaPrimaDto>> ListarMateriasAsync(CancellationToken ct)
    {
        var materiais = await _repo.ListarMateriasAsync(ct);
        var compras = await _repo.ListarComprasAsync(ct);
        var reservas = await _repo.ListarReservasAsync(ct);
        var aReceber = compras.Where(x => x.Situacao is SituacaoCompraMateriaPrima.Planejada or SituacaoCompraMateriaPrima.Pedido)
            .GroupBy(x => x.MateriaPrimaId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        var pendente = reservas.Where(x => x.Situacao == SituacaoReservaMaterial.Reservada)
            .GroupBy(x => x.MateriaPrimaId).ToDictionary(g => g.Key, g => g.Sum(x => x.QuantidadePendente));
        return materiais.Select(x => MapearMateria(x, aReceber.GetValueOrDefault(x.Id), pendente.GetValueOrDefault(x.Id))).ToList();
    }

    public Task<IReadOnlyList<PendenciaEstoquePedidoDto>> ListarPendenciasAsync(CancellationToken ct) => _consultaPedidos.ListarPendenciasMateriaisAsync(ct);

    public async Task<MateriaPrimaDto> CriarMateriaAsync(MateriaPrimaRequest r, CancellationToken ct)
    {
        ValidarMateria(r);
        if (await _repo.NomeMateriaExisteAsync(r.Nome!, null, ct)) throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe uma matéria-prima com esse nome.");
        var item = MateriaPrima.Criar(r.Nome!, r.Unidade!, r.QuantidadeDisponivel, r.EstoqueMinimo);
        _repo.Adicionar(item);
        RegistrarMovimento(item, null, r.QuantidadeDisponivel, TipoMovimentoMateriaPrima.Ajuste, "Saldo inicial do cadastro");
        await _uow.SaveChangesAsync(ct);
        return MapearMateria(item);
    }

    public async Task<MateriaPrimaDto> AtualizarMateriaAsync(Guid id, MateriaPrimaRequest r, CancellationToken ct)
    {
        await using var transacao = await _uow.IniciarTransacaoAsync(ct);
        var item = (await _repo.BloquearMateriasAsync(new[] { id }, ct)).SingleOrDefault() ?? throw new NaoEncontradoException("Matéria-prima");
        ValidarMateria(r);
        if (await _repo.NomeMateriaExisteAsync(r.Nome!, id, ct)) throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe uma matéria-prima com esse nome.");
        var diferenca = r.QuantidadeDisponivel - item.QuantidadeDisponivel;
        if (diferenca != 0 && !item.Ativo) throw new ConflitoException(CodigosErro.RegistroRelacionado, "Reative o material antes de movimentar o saldo.");
        item.Atualizar(r.Nome!, r.Unidade!, r.QuantidadeDisponivel, r.EstoqueMinimo);
        RegistrarMovimento(item, null, diferenca, TipoMovimentoMateriaPrima.Ajuste, "Ajuste manual do saldo");
        await _uow.SaveChangesAsync(ct);
        await transacao.ConfirmarAsync(ct);
        return (await ListarMateriasAsync(ct)).First(x => x.Id == id);
    }

    public async Task<MateriaPrimaDto> DefinirMateriaAtivaAsync(Guid id, bool ativo, CancellationToken ct)
    {
        await using var transacao = await _uow.IniciarTransacaoAsync(ct);
        var item = (await _repo.BloquearMateriasAsync(new[] { id }, ct)).SingleOrDefault() ?? throw new NaoEncontradoException("Matéria-prima");
        item.DefinirAtivo(ativo); await _uow.SaveChangesAsync(ct);
        await transacao.ConfirmarAsync(ct);
        return (await ListarMateriasAsync(ct)).First(x => x.Id == id);
    }

    public async Task<IReadOnlyList<CompraMateriaPrimaDto>> ListarComprasAsync(CancellationToken ct) =>
        (await _repo.ListarComprasAsync(ct)).Select(MapearCompra).ToList();

    public async Task<CompraMateriaPrimaDto> CriarCompraAsync(CompraMateriaPrimaRequest r, CancellationToken ct)
    {
        ValidarPrecisao(r.Quantidade);
        if (r.Quantidade <= 0) throw new ValidacaoException(CodigosErro.Validacao, "Informe uma quantidade maior que zero.", new Dictionary<string, string[]> { ["quantidade"] = ["A quantidade deve ser maior que zero."] });
        if (r.DataPrevista == default) throw new ValidacaoException(CodigosErro.Validacao, "Informe a data prevista.", new Dictionary<string, string[]> { ["dataPrevista"] = ["Informe a data prevista para a compra."] });
        var materia = await _repo.ObterMateriaAsync(r.MateriaPrimaId, ct) ?? throw new NaoEncontradoException("Matéria-prima");
        if (!materia.Ativo) throw new ValidacaoException(CodigosErro.Validacao, "Reative a matéria-prima antes de planejar uma compra.", new Dictionary<string, string[]> { ["materiaPrimaId"] = ["Esta matéria-prima está inativa."] });
        var compra = CompraMateriaPrima.Criar(materia.Id, r.Quantidade, r.Fornecedor, r.DataPrevista, _clock.AgoraUtc);
        _repo.Adicionar(compra); await _uow.SaveChangesAsync(ct);
        return MapearCompra(compra, materia);
    }

    public async Task<CompraMateriaPrimaDto> AtualizarSituacaoCompraAsync(Guid id, SituacaoCompraMateriaPrima nova, CancellationToken ct)
    {
        await using var transacao = await _uow.IniciarTransacaoAsync(ct);
        var compra = await _repo.ObterCompraAsync(id, ct) ?? throw new NaoEncontradoException("Compra planejada");
        var materia = (await _repo.BloquearMateriasAsync(new[] { compra.MateriaPrimaId }, ct)).Single();
        if (compra.Situacao is SituacaoCompraMateriaPrima.Recebida or SituacaoCompraMateriaPrima.Cancelada)
            throw new ConflitoException(CodigosErro.ConflitoConcorrencia, "Esta compra já foi encerrada e não pode ser alterada.");
        if (nova is SituacaoCompraMateriaPrima.Recebida)
        {
            if (!materia.Ativo) throw new ConflitoException(CodigosErro.RegistroRelacionado, "Reative o material antes de receber a compra.");
            materia.AlterarQuantidade(compra.Quantidade);
            RegistrarMovimento(materia, null, compra.Quantidade, TipoMovimentoMateriaPrima.EntradaCompra, $"Recebimento da compra {compra.Id}");
        }
        if (nova is not (SituacaoCompraMateriaPrima.Pedido or SituacaoCompraMateriaPrima.Recebida or SituacaoCompraMateriaPrima.Cancelada))
            throw new ValidacaoException(CodigosErro.Validacao, "Situação de compra inválida.", new Dictionary<string, string[]> { ["situacao"] = ["Escolha Pedido, Recebida ou Cancelada."] });
        compra.DefinirSituacao(nova); await _uow.SaveChangesAsync(ct);
        await transacao.ConfirmarAsync(ct);
        return MapearCompra(compra, materia);
    }

    private static CompraMateriaPrimaDto MapearCompra(CompraMateriaPrima x) => MapearCompra(x, x.MateriaPrima);
    private static CompraMateriaPrimaDto MapearCompra(CompraMateriaPrima x, MateriaPrima materia)
    {
        var aberta = x.Situacao is SituacaoCompraMateriaPrima.Planejada or SituacaoCompraMateriaPrima.Pedido;
        var opcoes = aberta ? new[] { SituacaoCompraMateriaPrima.Pedido, SituacaoCompraMateriaPrima.Recebida, SituacaoCompraMateriaPrima.Cancelada }
            .Where(s => s != x.Situacao).Select(s => new OpcaoSituacaoCompraDto(s, RotuloCompra(s))).ToArray() : Array.Empty<OpcaoSituacaoCompraDto>();
        return new(x.Id, x.MateriaPrimaId, materia.Nome, materia.Unidade, x.Quantidade, x.Fornecedor, x.DataPrevista, x.Situacao,
            x.CriadaEm, aberta, RotuloCompra(x.Situacao), opcoes);
    }
    private static string RotuloCompra(SituacaoCompraMateriaPrima s) => s == SituacaoCompraMateriaPrima.Pedido ? "Pedido realizado" : s.ToString();
    private static MateriaPrimaDto MapearMateria(MateriaPrima x, decimal aReceber = 0, decimal pendente = 0) =>
        new(x.Id, x.Nome, x.Unidade, x.QuantidadeDisponivel, x.EstoqueMinimo, aReceber, x.SaldoReservado, x.Ativo,
            x.SaldoDisponivel, pendente, x.SaldoDisponivel <= x.EstoqueMinimo, Math.Max(0, x.EstoqueMinimo + pendente - x.SaldoDisponivel - aReceber));
    private static void ValidarMateria(MateriaPrimaRequest r)
    {
        ValidarPrecisao(r.QuantidadeDisponivel);
        ValidarPrecisao(r.EstoqueMinimo);
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
        foreach (var linha in linhas) ValidarPrecisao(linha.QuantidadePorUnidade);
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

    /// <summary>Necessidades e reservas entram na transacao da aprovacao; falta de saldo/ficha nunca bloqueia a aprovacao.</summary>
    public Task GerarReservasPedidoAsync(Pedido pedido, CancellationToken ct) =>
        AplicarNecessidadesFichaAsync(pedido, pedido.Itens.Select(x => (x.ProdutoId, x.Quantidade)).ToList(), ct);

    public async Task<MateriaisPedidoDto> AplicarFichaPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        await using var transacao = await _uow.IniciarTransacaoAsync(ct);
        var pedido = await _pedidos.ObterParaAlteracaoAsync(pedidoId, ct) ?? throw new NaoEncontradoException("Pedido");
        pedido.RegistrarRevisaoMateriais(_clock.AgoraUtc, _usuario.Id, ativarControle: true);
        var detalhe = await _consultaPedidos.ObterDetalheAsync(pedidoId, ct);
        await AplicarNecessidadesFichaAsync(pedido, detalhe.Itens.Select(x => (x.ProdutoId, x.Quantidade)).ToList(), ct);
        await _uow.SaveChangesAsync(ct);
        await transacao.ConfirmarAsync(ct);
        return await ListarReservasPedidoAsync(pedidoId, ct);
    }

    private async Task AplicarNecessidadesFichaAsync(Pedido pedido, IReadOnlyList<(Guid ProdutoId, int Quantidade)> itens, CancellationToken ct)
    {
        var fichas = await _repo.ListarFichasPorProdutosAsync(itens.Select(x => x.ProdutoId).Distinct().ToArray(), ct);
        var ausentes = itens.Where(x => fichas.All(f => f.ProdutoId != x.ProdutoId))
            .GroupBy(x => x.ProdutoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));
        foreach (var pendencia in await _repo.ListarPendenciasPedidoAsync(pedido.Id, ct))
        {
            if (ausentes.Remove(pendencia.ProdutoId)) continue;
            pendencia.Resolver(_clock.AgoraUtc);
        }
        foreach (var (produtoId, quantidade) in ausentes)
            _repo.Adicionar(PendenciaFichaTecnicaPedido.Criar(pedido.Id, produtoId, quantidade, _clock.AgoraUtc));
        var necessidades = (from item in itens
                            join ficha in fichas on item.ProdutoId equals ficha.ProdutoId
                            group item.Quantidade * ficha.QuantidadePorUnidade by ficha.MateriaPrimaId into g
                            select new { Id = g.Key, Quantidade = g.Sum() }).ToDictionary(x => x.Id, x => x.Quantidade);
        await RecalcularReservasAsync(pedido.Id, necessidades, ct);
    }

    private async Task RecalcularReservasAsync(Guid pedidoId, IReadOnlyDictionary<Guid, decimal> necessidades, CancellationToken ct)
    {
        var existentes = (await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Where(x => x.Situacao == SituacaoReservaMaterial.Reservada).ToList();
        var ids = existentes.Select(x => x.MateriaPrimaId).Concat(necessidades.Keys).Distinct().ToArray();
        var materiais = (await _repo.BloquearMateriasAsync(ids, ct)).ToDictionary(x => x.Id);
        if (necessidades.Keys.Any(id => !materiais.ContainsKey(id)))
            throw new NaoEncontradoException("Matéria-prima");
        var mapa = existentes.ToDictionary(x => x.MateriaPrimaId);
        foreach (var id in ids.OrderBy(x => x))
        {
            var material = materiais[id];
            mapa.TryGetValue(id, out var reserva);
            var anterior = reserva?.QuantidadeReservada ?? 0;
            if (!necessidades.TryGetValue(id, out var necessaria))
            {
                material.AlterarReserva(-anterior);
                reserva!.DefinirSituacao(SituacaoReservaMaterial.Liberada);
                RegistrarMovimento(material, pedidoId, -anterior, TipoMovimentoMateriaPrima.LiberacaoReserva, "Material removido do consumo do pedido");
                continue;
            }
            var alocada = material.Ativo ? Math.Min(necessaria, material.SaldoDisponivel + anterior) : 0;
            var variacao = alocada - anterior;
            material.AlterarReserva(variacao);
            if (reserva is null) _repo.Adicionar(ReservaMateriaPrimaPedido.Criar(pedidoId, id, necessaria, _clock.AgoraUtc, alocada));
            else reserva.Ajustar(necessaria, alocada);
            RegistrarMovimento(material, pedidoId, variacao,
                variacao >= 0 ? TipoMovimentoMateriaPrima.Reserva : TipoMovimentoMateriaPrima.LiberacaoReserva, "Reserva dos materiais do pedido");
        }
    }

    public async Task<MateriaisPedidoDto> ListarReservasPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var pedido = await _pedidos.ObterParaAlteracaoAsync(pedidoId, ct) ?? throw new NaoEncontradoException("Pedido");
        var reservas = await _repo.ListarReservasPedidoAsync(pedidoId, ct);
        var registros = reservas.Select(MapearReserva).ToList();
        var nomes = pedido.Itens.GroupBy(x => x.ProdutoId).ToDictionary(g => g.Key, g => g.First().ProdutoNome);
        var fichas = (await _repo.ListarPendenciasPedidoAsync(pedidoId, ct)).Select(x =>
            new PendenciaFichaPedidoDto(x.ProdutoId, nomes.GetValueOrDefault(x.ProdutoId, "Produto"), x.QuantidadeProduto, "Ficha técnica ausente")).ToList();
        var ativas = registros.Where(x => x.Ativa).ToList();
        var aberto = pedido.Situacao == SituacaoPedido.Aberto;
        var pendente = pedido.ControleMateriais && (fichas.Count > 0 || (aberto && ativas.Count == 0) || ativas.Any(x => x.QuantidadePendente > 0)
            || reservas.Any(x => x.Situacao == SituacaoReservaMaterial.Reservada && !x.MateriaPrima.Ativo));
        return new(pedido.ControleMateriais, aberto && pedido.ControleMateriais, aberto,
            pendente, aberto && (!pedido.ControleMateriais || !pendente), registros, fichas);
    }

    public async Task<IReadOnlyList<MovimentoMateriaPrimaDto>> ListarMovimentosAsync(Guid materiaPrimaId, CancellationToken ct)
    {
        if (await _repo.ObterMateriaAsync(materiaPrimaId, ct) is null) throw new NaoEncontradoException("Matéria-prima");
        return (await _repo.ListarMovimentosAsync(materiaPrimaId, ct)).Select(x => new MovimentoMateriaPrimaDto(x.Id, x.MateriaPrimaId,
            x.MateriaPrima.Nome, x.PedidoId, x.Pedido?.Numero, x.Variacao, x.Tipo, x.Observacao, x.CriadoEm,
            x.Tipo switch {
                TipoMovimentoMateriaPrima.EntradaCompra => "Entrada de compra",
                TipoMovimentoMateriaPrima.SaidaPedido => "Consumo de pedido",
                TipoMovimentoMateriaPrima.Reserva => "Reserva",
                TipoMovimentoMateriaPrima.LiberacaoReserva => "Liberação de reserva",
                _ => "Ajuste"
            }, x.SaldoFisicoApos, x.SaldoReservadoApos, x.UsuarioId)).ToList();
    }

    public async Task<MateriaisPedidoDto> AtualizarReservasPedidoAsync(Guid pedidoId, IReadOnlyList<ReservaPedidoRequest> linhas, CancellationToken ct)
    {
        await using var transacao = await _uow.IniciarTransacaoAsync(ct);
        var pedido = await _pedidos.ObterParaAlteracaoAsync(pedidoId, ct) ?? throw new NaoEncontradoException("Pedido");
        if (!pedido.ControleMateriais)
            throw new ConflitoException(CodigosErro.PedidoTransicaoInvalida, "Aplique a ficha técnica para ativar o controle de materiais deste pedido legado.");
        pedido.RegistrarRevisaoMateriais(_clock.AgoraUtc, _usuario.Id);
        if (linhas.Any(x => x.Quantidade <= 0) || linhas.Select(x => x.MateriaPrimaId).Distinct().Count() != linhas.Count)
            throw new ValidacaoException(CodigosErro.Validacao, "Revise os materiais do pedido.", new Dictionary<string, string[]> { ["materiais"] = ["Cada material deve aparecer uma vez e ter quantidade maior que zero."] });
        foreach (var linha in linhas) ValidarPrecisao(linha.Quantidade);
        var catalogo = (await _repo.ListarMateriasAsync(ct)).ToDictionary(x => x.Id);
        if (linhas.Any(x => !catalogo.TryGetValue(x.MateriaPrimaId, out var m) || !m.Ativo))
            throw new ValidacaoException(CodigosErro.Validacao, "Selecione apenas matérias-primas ativas.", new Dictionary<string, string[]> { ["materiais"] = ["Há material inativo ou inexistente na lista."] });
        await RecalcularReservasAsync(pedidoId, linhas.ToDictionary(x => x.MateriaPrimaId, x => x.Quantidade), ct);
        await _uow.SaveChangesAsync(ct);
        await transacao.ConfirmarAsync(ct);
        return await ListarReservasPedidoAsync(pedidoId, ct);
    }

    public async Task ConsumirReservasPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var reservas = (await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Where(x => x.Situacao == SituacaoReservaMaterial.Reservada).ToList();
        var materiais = (await _repo.BloquearMateriasAsync(reservas.Select(x => x.MateriaPrimaId).Distinct().ToArray(), ct)).ToDictionary(x => x.Id);
        var pendenciasFicha = await _repo.ListarPendenciasPedidoAsync(pedidoId, ct);
        var falta = reservas.Where(x => x.QuantidadePendente > 0 || !materiais[x.MateriaPrimaId].Ativo).ToList();
        if (reservas.Count == 0 || pendenciasFicha.Count > 0 || falta.Count > 0)
            throw new ConflitoException(CodigosErro.RegistroRelacionado, "Não é possível iniciar a produção: há pendências de materiais ou ficha técnica.",
                new Dictionary<string, string[]> { ["materiais"] =
                    falta.Select(x => $"{x.MateriaPrima.Nome}: necessário {x.Quantidade}, reservado {x.QuantidadeReservada}, pendente {x.QuantidadePendente} {x.MateriaPrima.Unidade}.")
                    .Concat(pendenciasFicha.Select(_ => "Ficha técnica ausente; configure e aplique a ficha neste pedido."))
                    .Concat(reservas.Count == 0 ? new[] { "Este pedido ainda não tem materiais reservados." } : Array.Empty<string>()).ToArray() });
        foreach (var reserva in reservas)
        {
            var quantidade = reserva.QuantidadeReservada;
            var material = materiais[reserva.MateriaPrimaId];
            material.ConsumirReserva(quantidade);
            reserva.DefinirSituacao(SituacaoReservaMaterial.Consumida);
            RegistrarMovimento(material, pedidoId, -quantidade, TipoMovimentoMateriaPrima.SaidaPedido, "Consumo transitório ao iniciar a produção");
        }
    }

    public async Task LiberarReservasPedidoAsync(Guid pedidoId, CancellationToken ct)
    {
        var reservas = (await _repo.ListarReservasPedidoAsync(pedidoId, ct)).Where(x => x.Situacao == SituacaoReservaMaterial.Reservada).ToList();
        var materiais = (await _repo.BloquearMateriasAsync(reservas.Select(x => x.MateriaPrimaId).Distinct().ToArray(), ct)).ToDictionary(x => x.Id);
        foreach (var reserva in reservas)
        {
            var quantidade = reserva.QuantidadeReservada;
            var material = materiais[reserva.MateriaPrimaId];
            material.AlterarReserva(-quantidade);
            reserva.DefinirSituacao(SituacaoReservaMaterial.Liberada);
            RegistrarMovimento(material, pedidoId, -quantidade, TipoMovimentoMateriaPrima.LiberacaoReserva, "Cancelamento do pedido");
        }
        foreach (var pendencia in await _repo.ListarPendenciasPedidoAsync(pedidoId, ct)) pendencia.Resolver(_clock.AgoraUtc);
    }

    private void RegistrarMovimento(MateriaPrima material, Guid? pedidoId, decimal variacao, TipoMovimentoMateriaPrima tipo, string motivo)
    {
        if (variacao == 0) return;
        _repo.Adicionar(MovimentoMateriaPrima.Criar(material.Id, pedidoId, variacao, tipo, motivo, _clock.AgoraUtc,
            material.QuantidadeDisponivel, material.SaldoReservado, _usuario.Id));
    }

    private static void ValidarPrecisao(decimal quantidade)
    {
        if (decimal.Round(quantidade, 3) != quantidade || quantidade < -999999999.999m || quantidade > 999999999.999m)
            throw new ValidacaoException(CodigosErro.Validacao, "Use uma quantidade de até 999999999,999 com no máximo três casas decimais.",
                new Dictionary<string, string[]> { ["quantidade"] = ["Quantidade fora da precisão permitida."] });
    }

    private static ReservaPedidoDto MapearReserva(ReservaMateriaPrimaPedido x) => new(x.Id, x.MateriaPrimaId,
        x.MateriaPrima.Nome, x.MateriaPrima.Unidade, x.Quantidade, x.Situacao.ToString(), x.QuantidadeReservada,
        x.QuantidadePendente, x.Situacao == SituacaoReservaMaterial.Reservada, x.Situacao == SituacaoReservaMaterial.Consumida,
        x.Situacao == SituacaoReservaMaterial.Consumida ? "Baixada do estoque" : x.Situacao == SituacaoReservaMaterial.Liberada
            ? "Reserva liberada" : x.QuantidadePendente > 0 ? "Reserva parcial" : "Reservada");
}
