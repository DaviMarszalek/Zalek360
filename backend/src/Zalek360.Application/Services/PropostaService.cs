using System.Net.Mail;
using System.Text.RegularExpressions;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Application.Services;

public sealed class PropostaService(IPropostaRepository propostas, IOrcamentoQueries consultas, IOrcamentoRepository orcamentos,
    OrcamentoDocumentoService documentos, IEmailPropostaSender email, EmpresaOptions empresa, IClock clock, IUsuarioAtual usuario, IUnitOfWork uow)
{
    private static readonly string[] Campos = ["cliente", "numero", "valorTotal", "validade", "prazo", "empresa"];
    public async Task<IReadOnlyList<ModeloMensagemDto>> ModelosAsync(CancellationToken ct) => (await propostas.ModelosAsync(ct)).Select(Mapear).ToList();
    public bool EmailDisponivel => email.Disponivel;

    public async Task<ModeloMensagemDto> AtualizarModeloAsync(Guid id, ModeloMensagemRequest r, CancellationToken ct)
    {
        var modelo = await propostas.ModeloAsync(id, ct) ?? throw new NaoEncontradoException("Modelo");
        Concorrencia.VerificarVersao(modelo.Versao, r.Versao);
        ValidarTexto(r.Assunto, r.Mensagem, modelo.Canal == CanalProposta.Email);
        if (Regex.Matches((r.Assunto ?? "") + r.Mensagem, @"\{([^{}]+)\}").Any(m => !Campos.Contains(m.Groups[1].Value)))
            throw Erro("mensagem", "Use somente os campos disponíveis: " + string.Join(", ", Campos.Select(x => "{" + x + "}")) + ".");
        modelo.Atualizar(r.Assunto ?? "", r.Mensagem!);
        await uow.SaveChangesAsync(ct);
        return Mapear(modelo);
    }

    public async Task<PreparacaoPropostaDto> PrepararAsync(Guid id, CancellationToken ct)
    {
        var o = await consultas.ObterDetalheAsync(id, ct);
        ValidarSituacao(o);
        var modelos = await propostas.ModelosAsync(ct);
        var em = modelos.Single(x => x.Canal == CanalProposta.Email);
        var wa = modelos.Single(x => x.Canal == CanalProposta.WhatsApp);
        // Lista explícita: nenhum dado de análise interna pode entrar nos modelos.
        var valores = new Dictionary<string, string> { ["cliente"] = o.Cliente.Nome, ["numero"] = $"{o.Numero:D6}",
            ["valorTotal"] = Formatos.Moeda(o.Resumo.ValorTotal), ["validade"] = Formatos.Data(o.Validade),
            ["prazo"] = o.PrazoEstimadoDiasUteis.ToString(), ["empresa"] = empresa.NomeFantasia };
        string Expandir(string texto) => Regex.Replace(texto, @"\{([^{}]+)\}", m => valores.GetValueOrDefault(m.Groups[1].Value, m.Value));
        var ultimo = await propostas.UltimoEnvioAsync(id, ct);
        return new(email.Disponivel, o.Cliente.Email, o.Cliente.WhatsApp, Expandir(em.Assunto), Expandir(em.Mensagem), Expandir(wa.Mensagem), o.Versao, ultimo is null ? null : Mapear(ultimo));
    }

    public async Task<EnvioPropostaDto> ConsultarEnvioAsync(Guid id, Guid chave, CancellationToken ct)
    {
        var envio = await propostas.EnvioAsync(chave, ct);
        if (envio is null || envio.OrcamentoId != id) throw new NaoEncontradoException("Envio");
        return Mapear(envio);
    }

    public async Task<ResultadoEnvioPropostaDto> EnviarAsync(Guid id, EnviarPropostaEmailRequest r, CancellationToken ct)
    {
        if (r.ChaveEnvio == Guid.Empty) throw Erro("chaveEnvio", "Informe a chave desta tentativa de envio.");
        var anterior = await propostas.EnvioAsync(r.ChaveEnvio, ct);
        if (anterior is not null)
        {
            if (anterior.OrcamentoId != id || anterior.Situacao != SituacaoEnvioProposta.Enviado)
                throw new ConflitoException("ENVIO_NAO_CONFIRMADO", "Esta tentativa está em processamento ou não teve confirmação. Confira sua caixa de enviados antes de preparar outro envio.");
            return new(Mapear(anterior), await consultas.ObterDetalheAsync(id, ct));
        }
        if (!email.Disponivel) throw new AppException("EMAIL_NAO_CONFIGURADO", "O envio direto está indisponível. Use seu aplicativo de e-mail e anexe o PDF.", 503);
        ValidarTexto(r.Assunto, r.Mensagem, true);
        var destino = r.Destinatario?.Trim();
        if (destino is null || destino.Length > 254 || destino.Any(char.IsControl) || !MailAddress.TryCreate(destino, out var endereco) || endereco.Address != destino)
            throw Erro("destinatario", "Informe um único endereço de e-mail válido.");
        var previa = await consultas.ObterDetalheAsync(id, ct);
        ValidarSituacao(previa); Concorrencia.VerificarVersao(previa.Versao, r.Versao);
        var tentativa = EnvioProposta.Criar(r.ChaveEnvio, id, usuario.Id, destino, r.Assunto!.Trim(), clock.AgoraUtc);
        propostas.Adicionar(tentativa);
        // Fora da transação de envio: queda do processo/commit incerto deixa esta chave bloqueada para reenvio.
        await uow.SaveChangesAsync(ct);
        await using var transacao = await uow.IniciarTransacaoAsync(CancellationToken.None);
        await propostas.BloquearOrcamentoAsync(id, CancellationToken.None);
        var o = await orcamentos.ObterParaAlteracaoAsync(id, CancellationToken.None) ?? throw new NaoEncontradoException("Orçamento");
        Concorrencia.VerificarVersao(o.Versao, r.Versao);
        ValidarSituacao(await consultas.ObterDetalheAsync(id, CancellationToken.None));
        var pdf = await documentos.GerarAsync(id, false, CancellationToken.None, paraApresentacao: true);
        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            await email.EnviarAsync(new(destino, r.Assunto.Trim(), r.Mensagem!.Trim(), pdf.Conteudo, pdf.NomeArquivo, tentativa.Id), limite.Token);
        }
        catch
        {
            tentativa.Concluir(false, clock.AgoraUtc);
            await uow.SaveChangesAsync(CancellationToken.None); await transacao.ConfirmarAsync(CancellationToken.None);
            throw new AppException("EMAIL_NAO_CONFIRMADO", "Não foi possível confirmar o envio. A apresentação não foi registrada. Confira sua caixa de enviados antes de tentar novamente.", 502);
        }
        o.RegistrarEnvioProposta(MeioContato.Email, $"Proposta enviada por e-mail para {destino}. Assunto: {r.Assunto.Trim()}", clock.HojeLocal, clock.AgoraUtc, usuario.Id);
        o.RegistrarExportacaoPdf(clock.AgoraUtc, usuario.Id);
        tentativa.Concluir(true, clock.AgoraUtc);
        await uow.SaveChangesAsync(CancellationToken.None); await transacao.ConfirmarAsync(CancellationToken.None);
        return new(Mapear(tentativa), await consultas.ObterDetalheAsync(id, CancellationToken.None));
    }

    public async Task<OrcamentoDetalheDto> ConfirmarExternoAsync(Guid id, ConfirmarPropostaRequest r, CancellationToken ct)
    {
        if (!Enum.IsDefined(r.Canal)) throw Erro("canal", "Selecione e-mail ou WhatsApp.");
        await using var transacao = await uow.IniciarTransacaoAsync(ct);
        await propostas.BloquearOrcamentoAsync(id, ct);
        var o = await orcamentos.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Orçamento");
        Concorrencia.VerificarVersao(o.Versao, r.Versao);
        ValidarSituacao(await consultas.ObterDetalheAsync(id, ct));
        o.RegistrarEnvioProposta(r.Canal == CanalProposta.Email ? MeioContato.Email : MeioContato.WhatsApp,
            "Apresentação da proposta confirmada pelo usuário após envio externo.", clock.HojeLocal, clock.AgoraUtc, usuario.Id);
        await uow.SaveChangesAsync(ct); await transacao.ConfirmarAsync(ct);
        return await consultas.ObterDetalheAsync(id, ct);
    }

    private void ValidarSituacao(OrcamentoDetalheDto o)
    {
        if (!o.PodeSerEditado || o.Validade < clock.HojeLocal)
            throw new ConflitoException("PROPOSTA_INDISPONIVEL", "A proposta está encerrada ou vencida. Revise o orçamento antes de enviar.");
    }
    private static void ValidarTexto(string? assunto, string? mensagem, bool exigirAssunto)
    {
        if ((exigirAssunto && string.IsNullOrWhiteSpace(assunto)) || (assunto?.Length ?? 0) > 200 || (assunto?.Any(char.IsControl) ?? false))
            throw Erro("assunto", "Informe um assunto de até 200 caracteres, sem quebras de linha.");
        if (string.IsNullOrWhiteSpace(mensagem) || mensagem.Length > 6000 || mensagem.Contains('\0'))
            throw Erro("mensagem", "Informe uma mensagem de até 6.000 caracteres.");
    }
    private static ValidacaoException Erro(string campo, string mensagem) => new(CodigosErro.Validacao, mensagem, new Dictionary<string, string[]> { [campo] = [mensagem] });
    private static ModeloMensagemDto Mapear(ModeloMensagemProposta m) => new(m.Id, m.Canal, m.Assunto, m.Mensagem, m.Versao);
    private static EnvioPropostaDto Mapear(EnvioProposta e) => new(e.Id, e.Situacao, e.Destinatario, e.Assunto, e.CriadoEm, e.ConcluidoEm);
}
