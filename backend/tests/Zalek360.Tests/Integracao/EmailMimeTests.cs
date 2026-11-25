using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using MimeKit;
using Zalek360.Application.Dtos;
using Zalek360.Infrastructure.Options;
using Zalek360.Infrastructure.Services;

namespace Zalek360.Tests.Integracao;

public sealed class EmailMimeTests
{
    [Fact]
    public async Task Transporte_smtp_preserva_unicode_destinatario_mensagem_e_anexo_pdf()
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var servidor = new TcpListener(IPAddress.Loopback, 0); servidor.Start();
        var porta = ((IPEndPoint)servidor.LocalEndpoint).Port;
        var recebido = Task.Run(async () =>
        {
            using var cliente = await servidor.AcceptTcpClientAsync(limite.Token);
            await using var stream = cliente.GetStream();
            using var leitor = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var escritor = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
            await escritor.WriteLineAsync("220 localhost teste");
            var dados = new StringBuilder(); var recebendo = false;
            while (await leitor.ReadLineAsync(limite.Token) is { } linha)
            {
                if (recebendo && linha != ".") { dados.Append(linha.StartsWith("..") ? linha[1..] : linha).Append("\r\n"); continue; }
                if (recebendo) { recebendo = false; await escritor.WriteLineAsync("250 aceito"); }
                else if (linha.StartsWith("EHLO")) await escritor.WriteAsync("250-localhost\r\n250 8BITMIME\r\n");
                else if (linha == "DATA") { recebendo = true; await escritor.WriteLineAsync("354 continue"); }
                else if (linha == "QUIT") { await escritor.WriteLineAsync("221 fim"); break; }
                else await escritor.WriteLineAsync("250 OK");
            }
            return dados.ToString();
        }, limite.Token);
        var sender = new EmailPropostaSender(Options.Create(new EmailOptions { Host = "127.0.0.1", Porta = porta, Seguranca = "None", Remetente = "propostas@example.test", NomeRemetente = "Zalek — teste" }));
        Assert.True(sender.Disponivel);
        var pdf = Encoding.ASCII.GetBytes("%PDF-fixture-transporte");
        await sender.EnviarAsync(new EmailProposta("destino@example.test", "Proposta — revisão", "Olá! Aprovação e condições.", pdf, "Proposta-revisão.pdf", Guid.NewGuid()), limite.Token);
        using var bytes = new MemoryStream(Encoding.UTF8.GetBytes(await recebido));
        using var mensagem = await MimeMessage.LoadAsync(bytes, limite.Token);
        Assert.Equal("destino@example.test", Assert.Single(mensagem.To.Mailboxes).Address);
        Assert.Equal("Proposta — revisão", mensagem.Subject); Assert.Equal("Olá! Aprovação e condições.", mensagem.TextBody!.Trim());
        var anexo = Assert.IsType<MimePart>(Assert.Single(mensagem.Attachments));
        Assert.Equal("application/pdf", anexo.ContentType.MimeType); Assert.Equal("Proposta-revisão.pdf", anexo.FileName);
        using var conteudo = new MemoryStream(); await anexo.Content!.DecodeToAsync(conteudo, limite.Token); Assert.Equal(pdf, conteudo.ToArray());
    }
}
