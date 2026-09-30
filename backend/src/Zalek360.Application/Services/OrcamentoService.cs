using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;

namespace Zalek360.Application.Services;

/// <summary>
/// UC02 — Criar Orçamento · UC03 — Registrar Decisão do Cliente · UC04 — Converter Orçamento Aprovado em Pedido.
/// </summary>
public sealed class OrcamentoService
{
    private readonly IOrcamentoRepository _orcamentos;
    private readonly IPedidoRepository _pedidos;
    private readonly IClienteRepository _clientes;
    private readonly IUsuarioRepository _usuarios;
    private readonly IProdutoRepository _produtos;
    private readonly IAnexoRepository _anexos;
    private readonly IOrcamentoQueries _consultas;
    private readonly INumeradorDocumentos _numerador;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IUsuarioAtual _usuario;

    public OrcamentoService(
        IOrcamentoRepository orcamentos, IPedidoRepository pedidos, IClienteRepository clientes, IUsuarioRepository usuarios,
        IProdutoRepository produtos, IAnexoRepository anexos, IOrcamentoQueries consultas, INumeradorDocumentos numerador,
        IUnitOfWork uow, IClock clock, IUsuarioAtual usuario)
    {
        _orcamentos = orcamentos;
        _pedidos = pedidos;
        _clientes = clientes;
        _usuarios = usuarios;
        _produtos = produtos;
        _anexos = anexos;
        _consultas = consultas;
        _numerador = numerador;
        _uow = uow;
        _clock = clock;
        _usuario = usuario;
    }

    /// <summary>Pré-visualização do resumo financeiro (sequência UC02, passos 7–9). Não persiste nada.</summary>
    public static CalculoDto Calcular(CalculoRequest request)
    {
        var linhas = (request.Itens ?? Array.Empty<CalculoItemRequest>())
            .Select(i => new LinhaCalculo(i.Quantidade ?? 0, i.ValorUnitario ?? 0, i.ValorPersonalizacaoUnitario ?? 0));
        var r = CalculadoraOrcamento.Calcular(linhas, request.Desconto ?? 0);
        return new CalculoDto(
            r.Linhas.Select(l => new CalculoItemDto(l.SubtotalProduto, l.TotalPersonalizacao, l.ValorTotal)).ToList(),
            r.QuantidadeItens, r.TotalUnidades, r.SubtotalProdutos, r.ValorPersonalizacao, r.Desconto,
            Math.Max(0, r.ValorTotal), r.Desconto > r.ValorBruto);
    }

    public async Task<OrcamentoDetalheDto> CriarAsync(OrcamentoRequest request, CancellationToken ct)
    {
        var erros = new ColetorErros();
        await ValidarClienteEResponsavelAsync(request, erros, ct);
        var itens = await MontarItensAsync(request.Itens ?? Array.Empty<ItemOrcamentoRequest>(), null, erros, ct);
        var dados = MapearDadosComerciais(request);
        var hoje = _clock.HojeLocal;
        erros.Mesclar(Orcamento.Validar(hoje, dados, itens));
        LancarSeInvalido(erros, itens.Count);

        // Número só é reservado depois da validação para evitar lacunas desnecessárias na sequência.
        var numero = await _numerador.ProximoNumeroOrcamentoAsync(ct);
        var orcamento = Orcamento.Criar(numero, request.ClienteId!.Value, request.ResponsavelId!.Value, hoje, dados, itens,
            _clock.AgoraUtc, _usuario.Id);
        _orcamentos.Adicionar(orcamento);
        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(orcamento.Id, ct);
    }

    public async Task<OrcamentoDetalheDto> AtualizarAsync(Guid id, OrcamentoRequest request, CancellationToken ct)
    {
        var orcamento = await _orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
        Concorrencia.VerificarVersao(orcamento.Versao, request.Versao);
        if (!orcamento.PodeSerEditado)
            throw new ConflitoException(CodigosErro.OrcamentoNaoEditavel, "Orçamento não pode ser editado nesta situação.");

        var erros = new ColetorErros();
        if (request.ClienteId is { } clienteId && clienteId != orcamento.ClienteId)
            erros.Adicionar("clienteId", "O cliente de um orçamento não pode ser trocado. Crie um novo orçamento para outro cliente.");
        var responsavelId = request.ResponsavelId ?? orcamento.ResponsavelId;
        await ValidarResponsavelAsync(responsavelId, erros, ct);
        var itens = await MontarItensAsync(request.Itens ?? Array.Empty<ItemOrcamentoRequest>(), orcamento, erros, ct);
        var dados = MapearDadosComerciais(request);
        erros.Mesclar(Orcamento.Validar(orcamento.DataOrcamento, dados, itens));
        LancarSeInvalido(erros, itens.Count);

        orcamento.Atualizar(responsavelId, dados, itens, _clock.AgoraUtc, _usuario.Id);
        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(id, ct);
    }

    public async Task<OrcamentoDetalheDto> MarcarAguardandoRetornoAsync(Guid id, AguardandoRetornoRequest request, CancellationToken ct)
    {
        var orcamento = await _orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
        Concorrencia.VerificarVersao(orcamento.Versao, request.Versao);
        orcamento.MarcarAguardandoRetorno(request.MeioApresentacao, _clock.HojeLocal, _clock.AgoraUtc, _usuario.Id);
        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(id, ct);
    }

    /// <summary>Registrar contato não altera o status do orçamento (BDD-7).</summary>
    public async Task<ContatoDto> RegistrarContatoAsync(Guid id, ContatoRequest request, CancellationToken ct)
    {
        var orcamento = await _orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
        var dataUtc = request.DataHora?.UtcDateTime;
        DateOnly? dataLocal = dataUtc is null ? null : _clock.ParaDataLocal(dataUtc.Value);
        var contato = orcamento.RegistrarContato(request.Tipo, dataUtc, dataLocal, request.Observacao, _clock.AgoraUtc, _usuario.Id);
        await _uow.SaveChangesAsync(ct);
        var contatos = await _consultas.ListarContatosAsync(id, ct);
        return contatos.First(c => c.Id == contato.Id);
    }

    /// <summary>
    /// UC03 + UC04 em uma única transação: aprovar orçamento + registrar decisão + criar pedido + copiar itens,
    /// personalizações e anexos + vínculo + histórico. Qualquer falha provoca ROLLBACK (RNF 5.2.1).
    /// </summary>
    public async Task<DecisaoResultadoDto> RegistrarDecisaoAsync(Guid id, DecisaoRequest request, CancellationToken ct)
    {
        var agora = _clock.AgoraUtc;
        Pedido? pedido = null;

        await using (var transacao = await _uow.IniciarTransacaoAsync(ct))
        {
            var orcamento = await _orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
            Concorrencia.VerificarVersao(orcamento.Versao, request.Versao);

            var dataUtc = request.DataHora?.UtcDateTime;
            DateOnly? dataLocal = dataUtc is null ? null : _clock.ParaDataLocal(dataUtc.Value);
            orcamento.RegistrarDecisao(request.Resultado, request.MeioContato, dataUtc, dataLocal,
                request.Motivo, request.Observacao, agora, _usuario.Id);

            // Passo 1 (diagrama UC04, 3–4): orçamento atualizado + decisão + histórico.
            await _uow.SaveChangesAsync(ct);

            if (orcamento.Situacao == SituacaoOrcamento.Aprovado)
            {
                // Passo 2 (diagrama UC04, 5–9): PedidoService — gera o pedido a partir do orçamento aprovado.
                var numero = await _numerador.ProximoNumeroPedidoAsync(ct);
                pedido = Pedido.CriarAPartirDe(orcamento, numero, dataLocal!.Value, agora, _usuario.Id);
                _pedidos.Adicionar(pedido);
                await _uow.SaveChangesAsync(ct);
            }

            await transacao.ConfirmarAsync(ct);
        }

        var detalhe = await _consultas.ObterDetalheAsync(id, ct);
        return new DecisaoResultadoDto(detalhe, detalhe.Pedido);
    }

    // ------------------------------------------------------------------ auxiliares

    private static DadosComerciaisOrcamento MapearDadosComerciais(OrcamentoRequest r) =>
        new(r.Validade, r.PrazoEstimadoDiasUteis, r.CondicaoPagamento, r.ObservacoesComerciais, r.Desconto ?? 0);

    private static void LancarSeInvalido(ColetorErros erros, int quantidadeItens)
    {
        if (!erros.PossuiErros) return;
        if (quantidadeItens == 0)
            throw new ValidacaoException(CodigosErro.OrcamentoSemItens, Orcamento.MensagemSemItens, erros.ParaDicionario());
        throw new ValidacaoException(CodigosErro.OrcamentoDadosInvalidos, "Corrija os campos destacados.", erros.ParaDicionario());
    }

    private async Task ValidarClienteEResponsavelAsync(OrcamentoRequest request, ColetorErros erros, CancellationToken ct)
    {
        // RN1: todo orçamento pertence a um cliente já cadastrado.
        if (request.ClienteId is null) erros.Adicionar("clienteId", "Selecione um cliente já cadastrado.");
        else if (!await _clientes.ExisteAsync(request.ClienteId.Value, ct)) erros.Adicionar("clienteId", "Cliente não encontrado.");

        if (request.ResponsavelId is null) erros.Adicionar("responsavelId", "Selecione o responsável.");
        else await ValidarResponsavelAsync(request.ResponsavelId.Value, erros, ct);
    }

    private async Task ValidarResponsavelAsync(Guid responsavelId, ColetorErros erros, CancellationToken ct)
    {
        var responsavel = await _usuarios.ObterPorIdAsync(responsavelId, ct);
        if (responsavel is null || !responsavel.Ativo) erros.Adicionar("responsavelId", "Responsável não encontrado.");
    }

    private async Task<List<DadosItemOrcamento>> MontarItensAsync(
        IReadOnlyList<ItemOrcamentoRequest> requests, Orcamento? existente, ColetorErros erros, CancellationToken ct)
    {
        var produtoIds = requests.Where(r => r.ProdutoId.HasValue).Select(r => r.ProdutoId!.Value).Distinct().ToList();
        var produtos = (await _produtos.ObterPorIdsAsync(produtoIds, ct)).ToDictionary(p => p.Id);
        var anexoIds = requests.SelectMany(r => r.AnexoIds ?? Array.Empty<Guid>()).Distinct().ToList();
        var anexos = (await _anexos.ObterPorIdsAsync(anexoIds, ct)).ToDictionary(a => a.Id);
        var usados = new HashSet<Guid>();
        var resultado = new List<DadosItemOrcamento>();

        for (var i = 0; i < requests.Count; i++)
        {
            var r = requests[i];
            var prefixo = $"itens[{i}].";
            var produtoNome = string.Empty;
            if (r.ProdutoId is null) erros.Adicionar(prefixo + "produtoId", "Selecione o produto.");
            else if (!produtos.TryGetValue(r.ProdutoId.Value, out var produto) || !produto.Ativo)
                erros.Adicionar(prefixo + "produtoId", "Produto não encontrado no catálogo.");
            else produtoNome = produto.Nome;

            if (r.Id is { } itemId && existente is not null && existente.Itens.All(x => x.Id != itemId))
                erros.Adicionar(prefixo + "id", "Este item não pertence ao orçamento. Recarregue a página.");

            var anexosItem = new List<ArquivoAnexo>();
            foreach (var anexoId in r.AnexoIds ?? Array.Empty<Guid>())
            {
                if (!usados.Add(anexoId)) continue;
                if (!anexos.TryGetValue(anexoId, out var anexo))
                {
                    erros.Adicionar(prefixo + "anexos", "Um dos arquivos anexados não foi encontrado. Envie-o novamente.");
                    continue;
                }
                var pendenteDoUsuario = anexo.Pendente && anexo.CriadoPorId == _usuario.Id;
                var jaDesteItem = r.Id.HasValue && anexo.ItemOrcamentoId == r.Id;
                if (!pendenteDoUsuario && !jaDesteItem)
                {
                    erros.Adicionar(prefixo + "anexos", "Um dos arquivos anexados não está disponível para este item.");
                    continue;
                }
                anexosItem.Add(anexo);
            }

            resultado.Add(new DadosItemOrcamento(
                r.Id,
                r.ProdutoId ?? Guid.Empty,
                produtoNome,
                r.Descricao,
                r.Quantidade ?? 0,
                r.ValorUnitario ?? 0,
                r.ValorPersonalizacaoUnitario ?? 0,
                new DadosPersonalizacao(r.TipoPersonalizacao, r.CorPeca, r.CoresArte, r.Medidas,
                    r.LocalAplicacao, r.ReferenciaArte, r.ObservacoesTecnicas),
                anexosItem));
        }
        return resultado;
    }
}
