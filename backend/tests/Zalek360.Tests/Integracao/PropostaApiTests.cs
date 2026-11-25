using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Documentos;
using Zalek360.Infrastructure.Persistence;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

public sealed class EmailPropostaTeste : IEmailPropostaSender
{
    public bool Disponivel { get; set; } = true;
    public bool Falhar { get; set; }
    public System.Collections.Concurrent.ConcurrentQueue<EmailProposta> Envios { get; } = new();
    public Func<Task>? Aguardar { get; set; }
    public async Task EnviarAsync(EmailProposta email, CancellationToken ct)
    {
        Envios.Enqueue(email);
        if (Aguardar is not null) await Aguardar();
        if (Falhar) throw new InvalidOperationException("detalhe-interno-teste");
    }
}

public sealed class PropostaApiTests
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private sealed class PdfTeste : IGeradorDocumentoOrcamento
    {
        public OrcamentoDocumentoDto? Documento { get; private set; }
        public Task<byte[]> GerarAsync(OrcamentoDocumentoDto documento, CancellationToken ct)
        { Documento = documento; return new GeradorDocumentoOrcamento().GerarAsync(documento, ct); }
    }
    internal static async Task<HttpClient> Login(ApiPdfFactory f, string email = "gestao@zalekpersonalizados.com.br")
    {
        var c = f.CreateClient(); Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/login", new { email, senha = "Zalek@2026" })).StatusCode); return c;
    }
    internal static async Task<OrcamentoDetalheDto> Criar(ApiPdfFactory f, HttpClient c)
    {
        Guid id;
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
            var u = await db.Usuarios.FirstAsync(x => x.Perfil == PerfilUsuario.Gestor);
            var cliente = await db.Clientes.FirstAsync(); var produto = await db.Produtos.FirstAsync();
            var o = Orcamento.Criar(999, cliente.Id, u.Id, f.Relogio.HojeLocal, Dados.Comercial(f.Relogio.HojeLocal.AddDays(15)),
                [Dados.Item(produto.Id, 2, 40, 5) with { CustoUnitarioInterno = 12 }], f.Relogio.AgoraUtc, u.Id);
            db.Orcamentos.Add(o); await db.SaveChangesAsync(); id = o.Id;
        }
        return (await (await c.GetAsync($"/api/orcamentos/{id}")).Content.ReadFromJsonAsync<OrcamentoDetalheDto>(Json))!;
    }
    internal static EnviarPropostaEmailRequest Pedido(OrcamentoDetalheDto o, Guid? chave = null) => new(chave ?? Guid.NewGuid(), "destinatario@example.test", "Proposta revisada — teste", "Olá! Esta é a mensagem revisada.", o.Versao);

    [Theory]
    [InlineData("gestao@zalekpersonalizados.com.br")]
    [InlineData("admin@zalekpersonalizados.com.br")]
    public async Task Envia_mensagem_revisada_e_pdf_publico_registra_apenas_apos_sucesso_e_nao_repete_chave(string usuario)
    {
        var sender = new EmailPropostaTeste(); var pdf = new PdfTeste();
        await using var f = new ApiPdfFactory(pdf, email: sender); await f.PrepararSeedAsync(); using var c = await Login(f, usuario);
        var o = await Criar(f, c); var caminho = $"/api/orcamentos/{o.Id}/proposta";
        var previa = await c.GetAsync(caminho); Assert.Equal(HttpStatusCode.OK, previa.StatusCode);
        var texto = await previa.Content.ReadAsStringAsync(); Assert.DoesNotContain("custo", texto, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("margem", texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("999", texto); Assert.Empty(sender.Envios);
        var r = Pedido(o); var envio = await c.PostAsJsonAsync(caminho + "/email", r);
        Assert.Equal(HttpStatusCode.OK, envio.StatusCode);
        var resultado = (await envio.Content.ReadFromJsonAsync<ResultadoEnvioPropostaDto>(Json))!;
        Assert.Equal(SituacaoEnvioProposta.Enviado, resultado.Envio.Situacao); Assert.Equal(SituacaoOrcamento.AguardandoRetorno, resultado.Orcamento.Situacao);
        var enviado = Assert.Single(sender.Envios); Assert.Equal(r.Destinatario, enviado.Destinatario); Assert.Equal(r.Assunto, enviado.Assunto); Assert.Equal(r.Mensagem, enviado.Mensagem);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(enviado.Pdf, 0, 5)); Assert.EndsWith(".pdf", enviado.NomeArquivo);
        Assert.False(pdf.Documento!.Rascunho); Assert.Null(pdf.Documento.Selo);
        Assert.DoesNotContain("custo", JsonSerializer.Serialize(pdf.Documento), StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("margem", JsonSerializer.Serialize(pdf.Documento), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync(caminho + "/email", r)).StatusCode); Assert.Single(sender.Envios);
        var contatos = await (await c.GetAsync($"/api/orcamentos/{o.Id}/contatos")).Content.ReadFromJsonAsync<List<ContatoDto>>(Json);
        var contato = Assert.Single(contatos!); Assert.Equal(MeioContato.Email, contato.Tipo); Assert.Contains(r.Destinatario!, contato.Observacao);
        Assert.Equal(usuario.StartsWith("admin") ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await c.GetAsync("/api/configuracoes/modelos-proposta")).StatusCode);
    }

    [Fact]
    public async Task Falha_smtp_nao_registra_apresentacao_nem_exportacao_e_tentativa_nao_e_reenviada()
    {
        var sender = new EmailPropostaTeste { Falhar = true };
        await using var f = new ApiPdfFactory(email: sender); await f.PrepararSeedAsync(); using var c = await Login(f);
        var o = await Criar(f, c); var r = Pedido(o); var caminho = $"/api/orcamentos/{o.Id}/proposta";
        var falha = await c.PostAsJsonAsync(caminho + "/email", r); Assert.Equal(HttpStatusCode.BadGateway, falha.StatusCode);
        Assert.DoesNotContain("detalhe-interno-teste", await falha.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(caminho + "/email", r)).StatusCode); Assert.Single(sender.Envios);
        var tentativa = await (await c.GetAsync(caminho + $"/email/{r.ChaveEnvio}")).Content.ReadFromJsonAsync<EnvioPropostaDto>(Json);
        Assert.Equal(SituacaoEnvioProposta.NaoConfirmado, tentativa!.Situacao);
        var atual = await (await c.GetAsync($"/api/orcamentos/{o.Id}")).Content.ReadFromJsonAsync<OrcamentoDetalheDto>(Json); Assert.Equal(SituacaoOrcamento.EmElaboracao, atual!.Situacao); Assert.Equal(o.Versao, atual.Versao);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ZalekDbContext>();
        Assert.False(await db.ContatosOrcamento.AnyAsync(x => x.OrcamentoId == o.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/orcamentos/{Guid.NewGuid()}/proposta/email/{r.ChaveEnvio}")).StatusCode);
    }

    [Fact]
    public async Task Sem_smtp_pdf_e_links_nao_apresentam_e_confirmacao_externa_preserva_fluxo()
    {
        var sender = new EmailPropostaTeste { Disponivel = false }; var pdf = new PdfTeste();
        await using var f = new ApiPdfFactory(pdf, email: sender); await f.PrepararSeedAsync(); using var c = await Login(f);
        var o = await Criar(f, c); var caminho = $"/api/orcamentos/{o.Id}/proposta";
        var p = await (await c.GetAsync(caminho)).Content.ReadFromJsonAsync<PreparacaoPropostaDto>(Json); Assert.False(p!.EmailDisponivel);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await c.PostAsJsonAsync(caminho + "/email", Pedido(o))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync(caminho + "/pdf", null)).StatusCode); Assert.False(pdf.Documento!.Rascunho);
        var antes = await (await c.GetAsync($"/api/orcamentos/{o.Id}")).Content.ReadFromJsonAsync<OrcamentoDetalheDto>(Json); Assert.Equal(SituacaoOrcamento.EmElaboracao, antes!.Situacao);
        var confirmacao = await c.PostAsJsonAsync(caminho + "/confirmacao", new ConfirmarPropostaRequest(CanalProposta.WhatsApp, o.Versao)); Assert.Equal(HttpStatusCode.OK, confirmacao.StatusCode);
        Assert.Equal(SituacaoOrcamento.AguardandoRetorno, (await confirmacao.Content.ReadFromJsonAsync<OrcamentoDetalheDto>(Json))!.Situacao);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(caminho + "/confirmacao", new ConfirmarPropostaRequest(CanalProposta.WhatsApp, o.Versao))).StatusCode);
        Assert.Empty(sender.Envios);
    }

    [Fact]
    public async Task Valida_endereco_cabecalhos_modelo_versao_e_campos_publicos()
    {
        var sender = new EmailPropostaTeste(); await using var f = new ApiPdfFactory(email: sender); await f.PrepararSeedAsync(); using var c = await Login(f, "admin@zalekpersonalizados.com.br");
        var o = await Criar(f, c); var caminho = $"/api/orcamentos/{o.Id}/proposta/email";
        foreach (var r in new[] { Pedido(o) with { Destinatario = "a@example.test,b@example.test" }, Pedido(o) with { Assunto = "Proposta\r\nBcc: outro@example.test" }, Pedido(o) with { Mensagem = "" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(caminho, r)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(caminho, Pedido(o) with { Versao = o.Versao + 1 })).StatusCode); Assert.Empty(sender.Envios);
        var modelos = await (await c.GetAsync("/api/configuracoes/modelos-proposta")).Content.ReadFromJsonAsync<JsonElement>();
        var modelo = modelos.GetProperty("modelos")[0]; var id = modelo.GetProperty("id").GetGuid(); var versao = modelo.GetProperty("versao").GetInt32();
        var url = $"/api/configuracoes/modelos-proposta/{id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(url, new ModeloMensagemRequest("Proposta {numero}", "Custo {custo}", versao))).StatusCode);
        var valido = new ModeloMensagemRequest("Proposta {numero}", "Olá {cliente}. Total {valorTotal}. {empresa}", versao);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync(url, valido)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(url, valido)).StatusCode);
    }
    [Theory]
    [InlineData(PerfilUsuario.Atendente)]
    [InlineData(PerfilUsuario.Operador)]
    public async Task Perfis_legados_e_anonimos_nao_acessam_envio_nem_modelos(PerfilUsuario perfil)
    {
        await using var f = new ApiPdfFactory(); await f.PrepararSeedAsync(); using var c = f.CreateClient();
        var id = Guid.NewGuid(); var url = $"/api/orcamentos/{id}/proposta";
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync(url)).StatusCode);
        using var scope = f.Services.CreateScope();
        var u = Usuario.Criar("Legado", "legado@example.test", "hash-teste", perfil, f.Relogio.AgoraUtc);
        c.DefaultRequestHeaders.Authorization = new("Bearer", scope.ServiceProvider.GetRequiredService<ITokenService>().Gerar(u, false).Token);
        foreach (var resposta in new[] { await c.GetAsync(url), await c.GetAsync(url + $"/email/{id}"), await c.PostAsJsonAsync(url + "/email", new { }), await c.PostAsJsonAsync(url + "/confirmacao", new { }), await c.PostAsync(url + "/pdf", null), await c.GetAsync("/api/configuracoes/modelos-proposta"), await c.PutAsJsonAsync($"/api/configuracoes/modelos-proposta/{id}", new { }) })
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

}
