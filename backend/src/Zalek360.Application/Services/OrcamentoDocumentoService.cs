using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;
using Zalek360.Domain.Services;

namespace Zalek360.Application.Services;

public sealed class OrcamentoDocumentoService(
    IOrcamentoQueries consultas, IClienteQueries clientes, IOrcamentoRepository orcamentos,
    IGeradorDocumentoOrcamento gerador, EmpresaOptions empresa, IClock clock,
    IUsuarioAtual usuario, IUnitOfWork uow)
{
    public async Task<ArquivoDocumentoOrcamento> GerarAsync(Guid id, bool registrarExportacao, CancellationToken ct)
    {
        // GET é estritamente de leitura. POST gera primeiro, e só confirma o evento se o PDF existir.
        await using var transacao = registrarExportacao ? await uow.IniciarTransacaoAsync(ct) : null;
        var agregado = registrarExportacao
            ? await orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento")
            : null;
        var o = await consultas.ObterDetalheAsync(id, ct);
        var cliente = await clientes.ObterDetalheAsync(o.Cliente.Id, ct);
        var instante = clock.AgoraUtc;
        var dto = Montar(o, cliente, empresa, clock, instante);
        var bytes = await gerador.GerarAsync(dto, ct);
        if (agregado is not null)
        {
            agregado.RegistrarExportacaoPdf(instante, usuario.Id);
            await uow.SaveChangesAsync(ct);
            await transacao!.ConfirmarAsync(ct);
        }
        return new(bytes, dto.NomeArquivo);
    }

    /// <summary>Projeção pura: usa os valores persistidos, sem cálculo financeiro ou dependência de PDF.</summary>
    public static OrcamentoDocumentoDto Montar(OrcamentoDetalheDto o, ClienteDetalheDto cliente,
        EmpresaOptions empresa, IClock clock, DateTime instanteUtc)
    {
        var hoje = clock.ParaDataLocal(instanteUtc);
        var dias = o.PrazoEstimadoDiasUteis;
        var semanas = (dias + 4) / 5;
        var duracao = $"{dias} {(dias == 1 ? "dia útil" : "dias úteis")}";
        var encerrado = o.Situacao is SituacaoOrcamento.Recusado or SituacaoOrcamento.Expirado or SituacaoOrcamento.Cancelado;
        var vencido = hoje > o.Validade && o.Situacao is SituacaoOrcamento.AguardandoRetorno or SituacaoOrcamento.EmElaboracao;
        var podeAprovar = !encerrado && !vencido && o.Situacao != SituacaoOrcamento.Aprovado;
        var selo = o.Situacao switch
        {
            SituacaoOrcamento.EmElaboracao => "RASCUNHO – ainda não apresentado ao cliente",
            SituacaoOrcamento.Aprovado => "APROVADO",
            SituacaoOrcamento.Recusado => "Proposta recusada",
            SituacaoOrcamento.Expirado => "Proposta expirada",
            SituacaoOrcamento.Cancelado => "Proposta cancelada",
            _ => vencido ? "Proposta com validade vencida" : null
        };
        var textosPrazo = new List<string>();
        var destaque = "Sem previsão de entrega";
        if (o.Situacao == SituacaoOrcamento.Aprovado && o.Pedido is { } pedido)
        {
            destaque = $"Entrega até {Formatos.Data(pedido.PrazoEntrega)}";
            textosPrazo.Add($"Pedido nº {pedido.Numero:D6} — entrega prevista até {Formatos.Data(pedido.PrazoEntrega)}.");
        }
        else if (podeAprovar)
        {
            destaque = $"{duracao} após a aprovação";
            textosPrazo.Add($"Prazo estimado de produção e entrega: {duracao} (aproximadamente {semanas} {(semanas == 1 ? "semana" : "semanas")}), contados a partir da aprovação da proposta.");
            textosPrazo.Add($"Previsão se aprovada hoje ({Formatos.Data(hoje)}): entrega até {Formatos.Data(CalendarioDiasUteis.Adicionar(hoje, dias))}.");
            textosPrazo.Add($"Se aprovada no último dia de validade ({Formatos.Data(o.Validade)}): entrega até {Formatos.Data(CalendarioDiasUteis.Adicionar(o.Validade, dias))}.");
        }
        else
            textosPrazo.Add(o.Situacao == SituacaoOrcamento.Aprovado
                ? "Consulte o atendente para confirmar o prazo de entrega do pedido."
                : "Consulte o atendente para uma nova proposta e confirmação dos prazos.");

        var contatos = Campos(("Telefone", Telefone(empresa.Telefone)), ("WhatsApp", Telefone(empresa.WhatsApp)),
            ("E-mail", empresa.Email), ("Site", empresa.Site)).Select(c => $"{c.Rotulo}: {c.Valor}").ToArray();
        var rodape = new List<string>();
        if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial)) rodape.Add(empresa.RazaoSocial);
        if (!string.IsNullOrWhiteSpace(empresa.Cnpj)) rodape.Add($"CNPJ: {Documento(empresa.Cnpj)}");
        if (!string.IsNullOrWhiteSpace(empresa.Endereco)) rodape.Add(empresa.Endereco);
        if (!string.IsNullOrWhiteSpace(empresa.TextoRodape)) rodape.Add(empresa.TextoRodape);
        var dadosCliente = Campos((cliente.TipoPessoa == TipoPessoa.PessoaFisica ? "CPF" : "CNPJ", Documento(cliente.Documento)),
            ("Nome fantasia", cliente.NomeFantasia), ("Contato", cliente.ContatoNome), ("Cargo", cliente.ContatoCargo),
            ("Telefone", Telefone(cliente.Telefone)), ("WhatsApp", Telefone(cliente.WhatsApp)), ("E-mail", cliente.Email));
        var itens = o.Itens.Select((i, index) => new ItemDocumentoDto((index + 1).ToString(CultureInfo.InvariantCulture),
            i.ProdutoNome, i.Descricao, i.Quantidade.ToString(CultureInfo.InvariantCulture), Formatos.Moeda(i.ValorUnitario),
            Formatos.Moeda(i.ValorPersonalizacaoUnitario), Formatos.Moeda(i.ValorTotal),
            Campos(("Tipo", i.Personalizacao.TipoPersonalizacao), ("Cor da peça", i.Personalizacao.CorPeca),
                ("Cores da arte", i.Personalizacao.CoresArte), ("Medidas", i.Personalizacao.Medidas),
                ("Local de aplicação", i.Personalizacao.LocalAplicacao), ("Referência da arte", i.Personalizacao.ReferenciaArte),
                ("Observações técnicas", i.Personalizacao.ObservacoesTecnicas)), i.Anexos.Select(a => a.Nome).ToArray())).ToArray();
        return new(NomeArquivo(o.Numero, cliente.NomeRazaoSocial), o.Numero.ToString("D6", CultureInfo.InvariantCulture),
            Formatos.Data(hoje), $"Documento gerado em {clock.ParaHoraLocal(instanteUtc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}",
            empresa.NomeFantasia, empresa.CaminhoLogo, contatos, rodape, cliente.NomeRazaoSocial, dadosCliente, o.Responsavel,
            selo, o.Situacao == SituacaoOrcamento.EmElaboracao, Formatos.Data(o.Validade), destaque, textosPrazo,
            "O prazo é uma estimativa a partir da aprovação e da liberação da arte; contagem em dias úteis (segunda a sexta), sem considerar feriados; sujeito a confirmação.",
            o.CondicaoPagamento, o.ObservacoesComerciais, itens, Formatos.Moeda(o.Resumo.SubtotalProdutos),
            Formatos.Moeda(o.Resumo.ValorPersonalizacao), o.Resumo.Desconto > 0 ? Formatos.Moeda(o.Resumo.Desconto) : null,
            Formatos.Moeda(o.Resumo.ValorTotal), new[] {
                "1) Você confirma a aprovação com nosso atendente.", "2) Geramos o pedido e iniciamos a produção.",
                "3) Avisamos quando estiver pronto para retirada/entrega."
            }, $"Confirme o orçamento nº {o.Numero:D6} com nosso atendente{(contatos.Length > 0 ? ": " + string.Join(" · ", contatos) : ".")}", podeAprovar);
    }

    private static IReadOnlyList<CampoDocumentoDto> Campos(params (string Rotulo, string? Valor)[] campos) =>
        campos.Where(c => !string.IsNullOrWhiteSpace(c.Valor)).Select(c => new CampoDocumentoDto(c.Rotulo, c.Valor!.Trim())).ToArray();

    public static string NomeArquivo(int numero, string cliente)
    {
        var semAcentos = new string(cliente.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
        var nome = Regex.Replace(semAcentos, "[^a-zA-Z0-9]+", "-").Trim('-');
        if (nome.Length == 0) nome = "Cliente";
        nome = nome[..Math.Min(nome.Length, 90)].TrimEnd('-');
        return $"Orcamento-{numero:D6}-{nome}.pdf";
    }

    private static string Documento(string valor)
    {
        var n = TextoBusca.SomenteDigitos(valor);
        return n.Length switch { 11 => $"{n[..3]}.{n[3..6]}.{n[6..9]}-{n[9..]}",
            14 => $"{n[..2]}.{n[2..5]}.{n[5..8]}/{n[8..12]}-{n[12..]}", _ => valor };
    }

    private static string? Telefone(string? valor)
    {
        var n = TextoBusca.SomenteDigitos(valor);
        return n.Length switch { 10 => $"({n[..2]}) {n[2..6]}-{n[6..]}", 11 => $"({n[..2]}) {n[2..7]}-{n[7..]}", _ => valor };
    }
}
