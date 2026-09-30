using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;

namespace Zalek360.Domain.Entities;

public sealed record DadosComerciaisOrcamento(
    DateOnly? Validade,
    int? PrazoEstimadoDiasUteis,
    string? CondicaoPagamento,
    string? ObservacoesComerciais,
    decimal Desconto);

/// <summary>
/// UC02/UC03 — Orçamento. Nasce sempre "Em Elaboração" (RN5), pertence a um cliente cadastrado (RN1),
/// exige ao menos um item (RN3), tem o valor calculado automaticamente (RN4) e só pode ser editado
/// antes da decisão (RN6). A decisão é registrada pelo atendente com meio de contato e data/hora (RN7).
/// </summary>
public class Orcamento : RaizAgregado
{
    public const string MensagemSemItens = "Adicione pelo menos um item ao orçamento antes de salvar.";
    public const int ValidadePadraoDias = 15;
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(5);

    private readonly List<ItemOrcamento> _itens = new();
    private readonly List<ContatoOrcamento> _contatos = new();

    public int Numero { get; private set; }
    public Guid ClienteId { get; private set; }
    public Cliente? Cliente { get; private set; }
    public Guid ResponsavelId { get; private set; }
    public Usuario? Responsavel { get; private set; }
    public DateOnly DataOrcamento { get; private set; }
    public DateOnly Validade { get; private set; }
    public int PrazoEstimadoDiasUteis { get; private set; }
    public string CondicaoPagamento { get; private set; } = string.Empty;
    public string? ObservacoesComerciais { get; private set; }
    public SituacaoOrcamento Situacao { get; private set; }
    public decimal SubtotalProdutos { get; private set; }
    public decimal ValorPersonalizacao { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal ValorTotal { get; private set; }
    public DateTime? AguardandoRetornoDesde { get; private set; }
    public DateTime? DataUltimaEdicao { get; private set; }
    public DecisaoOrcamento? Decisao { get; private set; }
    public Pedido? PedidoGerado { get; private set; }

    public IReadOnlyCollection<ItemOrcamento> Itens => _itens;
    public IReadOnlyCollection<ContatoOrcamento> Contatos => _contatos;

    private Orcamento() { }

    public string NumeroFormatado => Formatos.NumeroDocumento(Numero);

    /// <summary>RF 4.1.2.6 — editável enquanto Em Elaboração ou Aguardando Retorno.</summary>
    public bool PodeSerEditado => Situacao is SituacaoOrcamento.EmElaboracao or SituacaoOrcamento.AguardandoRetorno;

    public int TotalUnidades => _itens.Sum(i => i.Quantidade);

    public static DateOnly ValidadePadrao(DateOnly dataOrcamento) => dataOrcamento.AddDays(ValidadePadraoDias);

    // ------------------------------------------------------------------ validação e criação

    public static IReadOnlyDictionary<string, string[]> Validar(
        DateOnly dataOrcamento, DadosComerciaisOrcamento dados, IReadOnlyList<DadosItemOrcamento> itens)
    {
        var erros = new ColetorErros();
        if (itens.Count == 0) erros.Adicionar("itens", MensagemSemItens);

        if (dados.Validade is null) erros.Adicionar("validade", "Informe a validade do orçamento.");
        else if (dados.Validade < dataOrcamento) erros.Adicionar("validade", "A validade não pode ser anterior à data do orçamento.");
        else if (dados.Validade > dataOrcamento.AddDays(365)) erros.Adicionar("validade", "A validade deve ser de no máximo 1 ano.");

        if (dados.PrazoEstimadoDiasUteis is null or <= 0) erros.Adicionar("prazoEstimadoDiasUteis", "Informe o prazo estimado em dias úteis.");
        else if (dados.PrazoEstimadoDiasUteis > 365) erros.Adicionar("prazoEstimadoDiasUteis", "O prazo estimado deve ser de no máximo 365 dias úteis.");

        var condicao = TextoOpcional.Limpar(dados.CondicaoPagamento);
        if (condicao is null) erros.Adicionar("condicaoPagamento", "Selecione a condição de pagamento.");
        else if (condicao.Length > 100) erros.Adicionar("condicaoPagamento", "Use no máximo 100 caracteres.");

        if ((TextoOpcional.Limpar(dados.ObservacoesComerciais)?.Length ?? 0) > 2000)
            erros.Adicionar("observacoesComerciais", "Use no máximo 2.000 caracteres.");
        if (dados.Desconto < 0) erros.Adicionar("desconto", "O desconto não pode ser negativo.");

        var itensValidos = true;
        for (var i = 0; i < itens.Count; i++)
        {
            var errosItem = ItemOrcamento.Validar(itens[i]);
            if (errosItem.Count > 0) itensValidos = false;
            erros.Mesclar(errosItem, $"itens[{i}].");
        }

        if (itens.Count > 0 && itensValidos && dados.Desconto >= 0)
        {
            var calculo = CalculadoraOrcamento.Calcular(
                itens.Select(i => new LinhaCalculo(i.Quantidade, i.ValorUnitario, i.ValorPersonalizacaoUnitario)), dados.Desconto);
            if (calculo.Desconto > calculo.ValorBruto)
                erros.Adicionar("desconto", "O desconto não pode ser maior que o valor dos itens.");
            if (calculo.ValorBruto > ItemOrcamento.ValorMaximo)
                erros.Adicionar("itens", "O valor total do orçamento excede o limite permitido.");
        }

        return erros.ParaDicionario();
    }

    public static void GarantirValido(DateOnly dataOrcamento, DadosComerciaisOrcamento dados, IReadOnlyList<DadosItemOrcamento> itens)
    {
        var erros = Validar(dataOrcamento, dados, itens);
        if (erros.Count == 0) return;
        if (itens.Count == 0)
            throw new DomainException(CodigosErro.OrcamentoSemItens, MensagemSemItens, CategoriaErro.Validacao, erros);
        throw new DomainException(CodigosErro.OrcamentoDadosInvalidos, "Corrija os campos destacados.", CategoriaErro.Validacao, erros);
    }

    public static Orcamento Criar(
        int numero,
        Guid clienteId,
        Guid responsavelId,
        DateOnly dataOrcamento,
        DadosComerciaisOrcamento dados,
        IReadOnlyList<DadosItemOrcamento> itens,
        DateTime agoraUtc,
        Guid usuarioId,
        Guid? id = null)
    {
        GarantirValido(dataOrcamento, dados, itens);

        var orcamento = new Orcamento
        {
            Id = id ?? Guid.NewGuid(),
            Numero = numero,
            ClienteId = clienteId,
            ResponsavelId = responsavelId,
            DataOrcamento = dataOrcamento,
            Situacao = SituacaoOrcamento.EmElaboracao // RN5: status inicial definido pelo sistema
        };
        orcamento.AplicarDadosComerciais(dados);
        for (var i = 0; i < itens.Count; i++)
            orcamento._itens.Add(ItemOrcamento.Criar(orcamento.Id, i + 1, itens[i]));
        orcamento.Recalcular();
        orcamento.MarcarCriacao(agoraUtc, usuarioId);

        orcamento.RegistrarHistorico(orcamento.Evento(
            TipoEventoHistorico.OrcamentoCriado,
            "Orçamento criado",
            $"Status inicial: {Rotulos.De(SituacaoOrcamento.EmElaboracao)} · {Formatos.Quantidade(orcamento._itens.Count, orcamento.TotalUnidades)} · {Formatos.Moeda(orcamento.ValorTotal)}",
            agoraUtc, agoraUtc, usuarioId, nova: Rotulos.De(SituacaoOrcamento.EmElaboracao)));
        return orcamento;
    }

    // ------------------------------------------------------------------ edição

    public void Atualizar(Guid responsavelId, DadosComerciaisOrcamento dados, IReadOnlyList<DadosItemOrcamento> itens, DateTime agoraUtc, Guid usuarioId)
    {
        if (!PodeSerEditado)
            throw new DomainException(CodigosErro.OrcamentoNaoEditavel,
                "Orçamento não pode ser editado nesta situação.", CategoriaErro.Conflito);

        GarantirValido(DataOrcamento, dados, itens);

        var existentes = _itens.ToDictionary(i => i.Id);
        var erros = new ColetorErros();
        for (var i = 0; i < itens.Count; i++)
            if (itens[i].Id is { } idItem && !existentes.ContainsKey(idItem))
                erros.Adicionar($"itens[{i}]", "Este item não pertence ao orçamento. Recarregue a página.");
        erros.LancarSePossuirErros(CodigosErro.OrcamentoDadosInvalidos, "Corrija os campos destacados.");

        var alteracoes = new List<string>();
        var totalAnterior = ValorTotal;

        if (ResponsavelId != responsavelId) alteracoes.Add("Responsável alterado");
        if (Validade != dados.Validade) alteracoes.Add($"Validade: {Formatos.Data(Validade)} → {Formatos.Data(dados.Validade!.Value)}");
        if (PrazoEstimadoDiasUteis != dados.PrazoEstimadoDiasUteis) alteracoes.Add($"Prazo: {PrazoEstimadoDiasUteis} → {dados.PrazoEstimadoDiasUteis} dias úteis");
        if (CondicaoPagamento != TextoOpcional.Limpar(dados.CondicaoPagamento)) alteracoes.Add("Condição de pagamento alterada");
        if (ObservacoesComerciais != TextoOpcional.Limpar(dados.ObservacoesComerciais)) alteracoes.Add("Observações comerciais alteradas");
        if (Desconto != Dinheiro.Arredondar(dados.Desconto)) alteracoes.Add($"Desconto: {Formatos.Moeda(Desconto)} → {Formatos.Moeda(dados.Desconto)}");

        ResponsavelId = responsavelId;
        AplicarDadosComerciais(dados);

        var idsRecebidos = itens.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        foreach (var removido in _itens.Where(i => !idsRecebidos.Contains(i.Id)).ToList())
        {
            _itens.Remove(removido);
            alteracoes.Add($"Item removido: {removido.ProdutoNome} · {removido.Quantidade} unid.");
        }

        var quantidadesAlteradas = new List<(ItemOrcamento Item, int Anterior, decimal TotalAnterior)>();
        for (var i = 0; i < itens.Count; i++)
        {
            var dado = itens[i];
            if (dado.Id is { } idExistente)
            {
                var item = existentes[idExistente];
                var r = item.Atualizar(i + 1, dado);
                if (r.QuantidadeAlterada) quantidadesAlteradas.Add((item, r.QuantidadeAnterior, r.TotalAnterior));
                if (r.OutrosCampos.Count > 0)
                    alteracoes.Add($"Item {i + 1} · {item.ProdutoNome}: {string.Join(", ", r.OutrosCampos)}");
            }
            else
            {
                var novo = ItemOrcamento.Criar(Id, i + 1, dado);
                _itens.Add(novo);
                alteracoes.Add($"Item {i + 1} adicionado: {novo.ProdutoNome} · {novo.Quantidade} unid.");
            }
        }
        _itens.Sort((a, b) => a.Ordem.CompareTo(b.Ordem));

        Recalcular();

        foreach (var (item, anterior, totalItemAnterior) in quantidadesAlteradas)
        {
            RegistrarHistorico(Evento(
                TipoEventoHistorico.QuantidadeAlterada,
                $"Quantidade alterada de {anterior} para {item.Quantidade}",
                $"Item {item.Ordem} · {item.ProdutoNome} · total de {Formatos.Moeda(totalItemAnterior)} para {Formatos.Moeda(item.ValorTotal)}",
                agoraUtc, agoraUtc, usuarioId));
        }

        if (alteracoes.Count > 0)
        {
            var descricao = string.Join(" · ", alteracoes);
            if (totalAnterior != ValorTotal)
                descricao += $" · Total: {Formatos.Moeda(totalAnterior)} → {Formatos.Moeda(ValorTotal)}";
            RegistrarHistorico(Evento(TipoEventoHistorico.OrcamentoAlterado, "Orçamento alterado", descricao, agoraUtc, agoraUtc, usuarioId));
        }

        if (alteracoes.Count > 0 || quantidadesAlteradas.Count > 0)
        {
            DataUltimaEdicao = agoraUtc;
            MarcarAlteracao(agoraUtc, usuarioId);
            IncrementarVersao();
        }
    }

    // ------------------------------------------------------------------ fluxo comercial

    /// <summary>A proposta foi apresentada ao cliente por um canal externo; passa a aguardar retorno.</summary>
    public void MarcarAguardandoRetorno(MeioContato? meioApresentacao, DateOnly hojeLocal, DateTime agoraUtc, Guid usuarioId)
    {
        if (Situacao != SituacaoOrcamento.EmElaboracao)
            throw new DomainException(CodigosErro.OrcamentoTransicaoInvalida,
                $"Somente orçamentos Em Elaboração podem ser marcados como Aguardando Retorno. Situação atual: {Rotulos.De(Situacao)}.",
                CategoriaErro.Conflito);
        if (Validade < hojeLocal)
            throw new DomainException(CodigosErro.OrcamentoValidadeVencida,
                $"A validade do orçamento ({Formatos.Data(Validade)}) já passou. Atualize a validade antes de apresentar a proposta ao cliente.");

        var anterior = Situacao;
        Situacao = SituacaoOrcamento.AguardandoRetorno;
        AguardandoRetornoDesde = agoraUtc;
        MarcarAlteracao(agoraUtc, usuarioId);
        IncrementarVersao();

        RegistrarHistorico(Evento(
            TipoEventoHistorico.OrcamentoStatusAlterado,
            "Orçamento passou para Aguardando Retorno",
            meioApresentacao is { } meio
                ? $"Proposta apresentada ao cliente via {Rotulos.De(meio)} (fora do sistema)"
                : "Proposta apresentada ao cliente (fora do sistema)",
            agoraUtc, agoraUtc, usuarioId, Rotulos.De(anterior), Rotulos.De(Situacao), meioApresentacao));
    }

    /// <summary>RF 4.1.2.9 — registra um contato feito fora do sistema. NÃO altera a situação do orçamento.</summary>
    public ContatoOrcamento RegistrarContato(MeioContato? tipo, DateTime? dataHoraUtc, DateOnly? dataLocal, string? observacao, DateTime agoraUtc, Guid usuarioId)
    {
        if (!PodeSerEditado)
            throw new DomainException(CodigosErro.OrcamentoFechado,
                $"Não é possível registrar contatos em um orçamento {Rotulos.De(Situacao)}.", CategoriaErro.Conflito);

        var erros = new ColetorErros();
        if (tipo is null) erros.Adicionar("tipo", "Selecione o tipo de contato.");
        if (dataHoraUtc is null) erros.Adicionar("dataHora", "Informe a data e a hora do contato.");
        else if (dataHoraUtc > agoraUtc + ToleranciaFuturo) erros.Adicionar("dataHora", "A data/hora do contato não pode estar no futuro.");
        else if (dataLocal < DataOrcamento) erros.Adicionar("dataHora", "A data do contato não pode ser anterior à data do orçamento.");
        var texto = TextoOpcional.Limpar(observacao);
        if (texto is null) erros.Adicionar("observacao", "Descreva o que foi conversado com o cliente.");
        else if (texto.Length > 2000) erros.Adicionar("observacao", "Use no máximo 2.000 caracteres.");
        erros.LancarSePossuirErros(CodigosErro.ContatoDadosInvalidos, "Não foi possível registrar o contato.");

        var contato = ContatoOrcamento.Criar(Id, tipo!.Value, dataHoraUtc!.Value, texto!, usuarioId, agoraUtc);
        _contatos.Add(contato);

        RegistrarHistorico(Evento(
            TipoEventoHistorico.ContatoRegistrado,
            $"Contato registrado via {Rotulos.De(contato.Tipo)}",
            $"“{contato.Observacao}”",
            contato.DataHora, agoraUtc, usuarioId, meio: contato.Tipo));
        return contato;
    }

    /// <summary>
    /// UC03 — registra a decisão informada pelo cliente fora do sistema.
    /// Aprovado/Recusado/Expirado exigem "Aguardando Retorno"; Cancelado pode ocorrer em qualquer situação
    /// anterior à conversão (RF 4.1.3.4). A geração do pedido (UC04) é orquestrada pela aplicação, em transação.
    /// </summary>
    public DecisaoOrcamento RegistrarDecisao(
        ResultadoDecisao? resultado,
        MeioContato? meioContato,
        DateTime? dataHoraUtc,
        DateOnly? dataDecisaoLocal,
        string? motivo,
        string? observacao,
        DateTime agoraUtc,
        Guid usuarioId)
    {
        var obrigatorios = new ColetorErros();
        if (resultado is null) obrigatorios.Adicionar("resultado", "Selecione o resultado da negociação.");
        if (meioContato is null) obrigatorios.Adicionar("meioContato", "Informe o meio de contato utilizado pelo cliente.");
        if (dataHoraUtc is null) obrigatorios.Adicionar("dataHora", "Informe a data e a hora da decisão.");
        // RN7: toda decisão registrada pelo atendente deve conter meio de contato e data/hora.
        obrigatorios.LancarSePossuirErros(CodigosErro.DecisaoDadosObrigatorios, "Toda decisão precisa de meio de contato e data/hora.");

        if (Decisao is not null || !PodeSerEditado)
            throw new DomainException(CodigosErro.OrcamentoJaDecidido,
                $"Este orçamento já está {Rotulos.De(Situacao)} e não aceita nova decisão.", CategoriaErro.Conflito);

        if (resultado != ResultadoDecisao.Cancelado && Situacao != SituacaoOrcamento.AguardandoRetorno)
            throw new DomainException(CodigosErro.OrcamentoTransicaoInvalida,
                "Marque o orçamento como Aguardando Retorno antes de registrar a decisão do cliente.", CategoriaErro.Conflito);

        var erros = new ColetorErros();
        if (dataHoraUtc > agoraUtc + ToleranciaFuturo) erros.Adicionar("dataHora", "A data/hora da decisão não pode estar no futuro.");
        else if (dataDecisaoLocal < DataOrcamento) erros.Adicionar("dataHora", "A data da decisão não pode ser anterior à data do orçamento.");
        if ((TextoOpcional.Limpar(motivo)?.Length ?? 0) > 100) erros.Adicionar("motivo", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(observacao)?.Length ?? 0) > 2000) erros.Adicionar("observacao", "Use no máximo 2.000 caracteres.");
        erros.LancarSePossuirErros(CodigosErro.DecisaoDadosInvalidos, "Não foi possível registrar a decisão.");

        if (resultado == ResultadoDecisao.Aprovado && dataDecisaoLocal > Validade)
            throw new DomainException(CodigosErro.OrcamentoValidadeVencida,
                $"A aprovação foi informada após a validade do orçamento ({Formatos.Data(Validade)}). Crie um novo orçamento com condições atualizadas.");

        var decisao = DecisaoOrcamento.Manual(Id, resultado!.Value, meioContato!.Value, dataHoraUtc!.Value, motivo, observacao, usuarioId, agoraUtc);
        var anterior = Situacao;
        Decisao = decisao;
        Situacao = Rotulos.ParaSituacao(decisao.Resultado);
        MarcarAlteracao(agoraUtc, usuarioId);
        IncrementarVersao();

        var partes = new List<string> { $"Meio de contato: {Rotulos.De(decisao.MeioContato!.Value)}" };
        if (decisao.Motivo is not null) partes.Add($"Motivo: {decisao.Motivo.ToLowerInvariant()}");
        if (decisao.Observacao is not null) partes.Add($"“{decisao.Observacao}”");
        RegistrarHistorico(Evento(
            TipoEventoHistorico.DecisaoRegistrada,
            $"Decisão registrada: {Rotulos.De(decisao.Resultado)}",
            string.Join(" · ", partes),
            decisao.DataHora, agoraUtc, usuarioId, Rotulos.De(anterior), Rotulos.De(Situacao), decisao.MeioContato));
        return decisao;
    }

    /// <summary>Regra de expiração (UC03 A2): "Aguardando Retorno" com validade ultrapassada vira "Expirado", sem pedido.</summary>
    public bool DeveExpirar(DateOnly hojeLocal) => Situacao == SituacaoOrcamento.AguardandoRetorno && Validade < hojeLocal;

    public bool ExpirarAutomaticamente(DateOnly hojeLocal, DateTime momentoExpiracaoUtc, DateTime agoraUtc)
    {
        if (!DeveExpirar(hojeLocal)) return false;
        var anterior = Situacao;
        Decisao = DecisaoOrcamento.ExpiracaoAutomatica(Id, momentoExpiracaoUtc, agoraUtc);
        Situacao = SituacaoOrcamento.Expirado;
        MarcarAlteracao(agoraUtc, null);
        IncrementarVersao();
        RegistrarHistorico(Evento(
            TipoEventoHistorico.OrcamentoExpirado,
            "Orçamento expirado automaticamente",
            $"A validade terminou em {Formatos.Data(Validade)} sem decisão registrada",
            momentoExpiracaoUtc, agoraUtc, null, Rotulos.De(anterior), Rotulos.De(Situacao)));
        return true;
    }

    /// <summary>Chamado pela fábrica de Pedido (UC04) para registrar o vínculo no histórico do orçamento.</summary>
    internal void RegistrarPedidoGerado(int numeroPedido, Guid pedidoId, DateTime agoraUtc)
    {
        RegistrarHistorico(HistoricoEvento.Criar(
            EscopoHistorico.Orcamento, TipoEventoHistorico.PedidoGerado, ClienteId, Id, pedidoId,
            $"Pedido {Formatos.NumeroDocumento(numeroPedido)} gerado automaticamente",
            "Cliente, itens, personalização, artes, valores, prazo e condições copiados deste orçamento",
            Decisao?.DataHora ?? agoraUtc, agoraUtc, null));
    }

    // ------------------------------------------------------------------ internos

    private void AplicarDadosComerciais(DadosComerciaisOrcamento dados)
    {
        Validade = dados.Validade!.Value;
        PrazoEstimadoDiasUteis = dados.PrazoEstimadoDiasUteis!.Value;
        CondicaoPagamento = dados.CondicaoPagamento!.Trim();
        ObservacoesComerciais = TextoOpcional.Limpar(dados.ObservacoesComerciais);
        Desconto = Dinheiro.Arredondar(dados.Desconto);
    }

    /// <summary>O backend sempre recalcula: os totais enviados pelo frontend nunca são usados.</summary>
    private void Recalcular()
    {
        var calculo = CalculadoraOrcamento.Calcular(_itens.Select(i => i.ParaLinhaCalculo()), Desconto);
        for (var i = 0; i < _itens.Count; i++) _itens[i].AplicarCalculo(calculo.Linhas[i]);
        SubtotalProdutos = calculo.SubtotalProdutos;
        ValorPersonalizacao = calculo.ValorPersonalizacao;
        Desconto = calculo.Desconto;
        ValorTotal = calculo.ValorTotal;
    }

    private HistoricoEvento Evento(
        TipoEventoHistorico tipo, string titulo, string? descricao, DateTime ocorridoEm, DateTime registradoEm,
        Guid? usuarioId, string? anterior = null, string? nova = null, MeioContato? meio = null) =>
        HistoricoEvento.Criar(EscopoHistorico.Orcamento, tipo, ClienteId, Id, null, titulo, descricao,
            ocorridoEm, registradoEm, usuarioId, anterior, nova, meio);
}
