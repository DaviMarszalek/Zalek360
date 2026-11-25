using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

public enum CanalProposta { Email, WhatsApp }
public enum SituacaoEnvioProposta { EmProcessamento, Enviado, NaoConfirmado }

public sealed class ModeloMensagemProposta : Entidade
{
    public CanalProposta Canal { get; private set; }
    public string Assunto { get; private set; } = "";
    public string Mensagem { get; private set; } = "";
    public int Versao { get; private set; } = 1;
    private ModeloMensagemProposta() { }
    public static ModeloMensagemProposta Criar(Guid id, CanalProposta canal, string assunto, string mensagem) =>
        new() { Id = id, Canal = canal, Assunto = assunto, Mensagem = mensagem };
    public void Atualizar(string assunto, string mensagem) { Assunto = assunto.Trim(); Mensagem = mensagem.Trim(); Versao++; }
}

/// <summary>Chave de envio persistida antes do SMTP. Uma tentativa sem confirmação nunca é reenviada automaticamente.</summary>
public sealed class EnvioProposta : Entidade
{
    public Guid OrcamentoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Destinatario { get; private set; } = "";
    public string Assunto { get; private set; } = "";
    public SituacaoEnvioProposta Situacao { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ConcluidoEm { get; private set; }
    private EnvioProposta() { }
    public static EnvioProposta Criar(Guid chave, Guid orcamentoId, Guid usuarioId, string destinatario, string assunto, DateTime agora) =>
        new() { Id = chave, OrcamentoId = orcamentoId, UsuarioId = usuarioId, Destinatario = destinatario, Assunto = assunto, CriadoEm = agora };
    public void Concluir(bool enviado, DateTime agora) { Situacao = enviado ? SituacaoEnvioProposta.Enviado : SituacaoEnvioProposta.NaoConfirmado; ConcluidoEm = agora; }
}
