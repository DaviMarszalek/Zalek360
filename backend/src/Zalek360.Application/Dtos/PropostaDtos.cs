using Zalek360.Domain.Entities;

namespace Zalek360.Application.Dtos;

public sealed record ModeloMensagemDto(Guid Id, CanalProposta Canal, string Assunto, string Mensagem, int Versao);
public sealed record ModeloMensagemRequest(string? Assunto, string? Mensagem, int? Versao);
public sealed record EnvioPropostaDto(Guid Id, SituacaoEnvioProposta Situacao, string Destinatario, string Assunto, DateTime CriadoEm, DateTime? ConcluidoEm);
public sealed record PreparacaoPropostaDto(bool EmailDisponivel, string? Email, string? WhatsApp, string Assunto, string MensagemEmail, string MensagemWhatsApp, int Versao, EnvioPropostaDto? UltimoEnvio);
public sealed record EnviarPropostaEmailRequest(Guid ChaveEnvio, string? Destinatario, string? Assunto, string? Mensagem, int Versao);
public sealed record ConfirmarPropostaRequest(CanalProposta Canal, int Versao);
public sealed record ResultadoEnvioPropostaDto(EnvioPropostaDto Envio, OrcamentoDetalheDto Orcamento);
public sealed record EmailProposta(string Destinatario, string Assunto, string Mensagem, byte[] Pdf, string NomeArquivo, Guid ChaveEnvio);
