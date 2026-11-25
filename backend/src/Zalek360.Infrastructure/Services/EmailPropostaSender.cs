using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Infrastructure.Options;

namespace Zalek360.Infrastructure.Services;

internal sealed class EmailPropostaSender(IOptions<EmailOptions> options) : IEmailPropostaSender
{
    public bool Disponivel => !string.IsNullOrWhiteSpace(options.Value.Host) && options.Value.Porta is > 0 and <= 65535 &&
        MailboxAddress.TryParse(options.Value.Remetente, out _) && options.Value.Seguranca is "StartTls" or "SslOnConnect" or "None" &&
        (string.IsNullOrEmpty(options.Value.Usuario) || !string.IsNullOrEmpty(options.Value.Senha));

    public async Task EnviarAsync(EmailProposta email, CancellationToken ct)
    {
        var o = options.Value;
        using var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(o.NomeRemetente, o.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(email.Destinatario));
        mensagem.Subject = email.Assunto;
        mensagem.MessageId = $"{email.ChaveEnvio:N}@{o.Remetente.Split('@').Last()}";
        var corpo = new BodyBuilder { TextBody = email.Mensagem };
        corpo.Attachments.Add(email.NomeArquivo, email.Pdf, new ContentType("application", "pdf"));
        mensagem.Body = corpo.ToMessageBody();
        using var smtp = new SmtpClient { Timeout = 30000 };
        await smtp.ConnectAsync(o.Host, o.Porta, o.Seguranca switch { "SslOnConnect" => SecureSocketOptions.SslOnConnect, "None" => SecureSocketOptions.None, _ => SecureSocketOptions.StartTls }, ct);
        if (!string.IsNullOrEmpty(o.Usuario)) await smtp.AuthenticateAsync(o.Usuario, o.Senha, ct);
        await smtp.SendAsync(mensagem, ct);
        // A aceitação SMTP já ocorreu. Uma falha ao desconectar não deve transformar sucesso em reenvio.
        try { await smtp.DisconnectAsync(true, ct); } catch { }
    }
}
