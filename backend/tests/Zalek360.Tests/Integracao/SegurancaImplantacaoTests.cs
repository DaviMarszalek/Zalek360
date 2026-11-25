using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Zalek360.Api.Ferramentas;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Infrastructure.Seguranca;
using Zalek360.Infrastructure.Services;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Integracao;

/// <summary>Fase 7.1: configuração segura de implantação e redefinição de senha pela ferramenta do container.</summary>
public sealed class SegurancaImplantacaoTests
{
    private const string ChaveForte = "chave-de-teste-forte-0123456789-abcdefghijklmnopqrstuvwxyz";
    private const string ConexaoForte = "Host=db;Database=teste;Username=teste;Password=senha-de-teste-bem-longa-123";
    private const string ConexaoFraca = "Host=db;Database=teste;Username=teste;Password=curta";
    private const string SenhaDemo = "Zalek@2026";

    /// <summary>Lê a chave publicada em appsettings.Development.json, sem copiá-la para o código de teste.</summary>
    private static string ChaveDeDesenvolvimentoPublicada()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "src", "Zalek360.Api", "appsettings.Development.json")))
            pasta = pasta.Parent;
        Assert.NotNull(pasta);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(pasta!.FullName, "src", "Zalek360.Api", "appsettings.Development.json")));
        return json.RootElement.GetProperty("Jwt").GetProperty("Chave").GetString()!;
    }

    [Fact]
    public void Desenvolvimento_tolera_segredos_locais()
    {
        Assert.Empty(ValidacaoImplantacao.Verificar(ChaveDeDesenvolvimentoPublicada(), ConexaoFraca, false, null, desenvolvimento: true));
    }

    [Fact]
    public void Fora_de_desenvolvimento_recusa_chave_jwt_publicada_curta_ou_ausente()
    {
        var publicada = ChaveDeDesenvolvimentoPublicada();
        Assert.True(ValidacaoImplantacao.Publicado(publicada));
        Assert.Contains(ValidacaoImplantacao.Verificar(publicada, ConexaoForte, false, null, false), p => p.Contains("publicado") && p.Contains("JWT_CHAVE"));
        Assert.Contains(ValidacaoImplantacao.Verificar("curta-demais", ConexaoForte, false, null, false), p => p.Contains("JWT_CHAVE"));
        Assert.Contains(ValidacaoImplantacao.Verificar(null, ConexaoForte, false, null, false), p => p.Contains("JWT_CHAVE"));
    }

    [Fact]
    public void Fora_de_desenvolvimento_recusa_senha_de_banco_curta_ou_ausente()
    {
        Assert.Contains(ValidacaoImplantacao.Verificar(ChaveForte, ConexaoFraca, false, null, false), p => p.Contains("POSTGRES_PASSWORD"));
        Assert.Contains(ValidacaoImplantacao.Verificar(ChaveForte, "Host=db;Database=teste;Username=teste", false, null, false), p => p.Contains("POSTGRES_PASSWORD"));
    }

    [Fact]
    public void Seed_de_demonstracao_exige_senha_explicita_em_qualquer_ambiente()
    {
        Assert.Contains(ValidacaoImplantacao.Verificar(ChaveForte, ConexaoForte, true, "", false), p => p.Contains("SENHA_USUARIOS_DEMO"));
        Assert.Contains(ValidacaoImplantacao.Verificar(ChaveForte, ConexaoForte, true, null, true), p => p.Contains("SENHA_USUARIOS_DEMO"));
        Assert.Empty(ValidacaoImplantacao.Verificar(ChaveForte, ConexaoForte, true, SenhaDemo, false));
    }

    [Fact]
    public void Configuracao_forte_e_aceita_e_falha_nunca_expoe_valores()
    {
        Assert.Empty(ValidacaoImplantacao.Verificar(ChaveForte, ConexaoForte, false, null, false));
        var publicada = ChaveDeDesenvolvimentoPublicada();
        var erro = Assert.Throws<InvalidOperationException>(() => ValidacaoImplantacao.Garantir(publicada, ConexaoFraca, false, null, false));
        Assert.Contains("Configuração insegura", erro.Message);
        Assert.DoesNotContain(publicada, erro.Message);
        Assert.DoesNotContain("curta", erro.Message);
    }

    private static (UsuarioService Servico, Usuario Usuario, BcryptPasswordHasher Hasher, UnidadeDeTrabalhoFake Uow) CenarioUsuario()
    {
        var banco = new BancoEmMemoria();
        var hasher = new BcryptPasswordHasher();
        var usuario = Usuario.Criar("Admin Teste", "admin@example.test", hasher.Gerar("senha-antiga-123456"), PerfilUsuario.Administrador, DateTime.UtcNow);
        banco.Usuarios.Add(usuario);
        var uow = new UnidadeDeTrabalhoFake(banco);
        return (new UsuarioService(new RepositoriosFake(banco), hasher, uow, new RelogioFixo()), usuario, hasher, uow);
    }

    [Fact]
    public async Task Redefinir_senha_usa_o_mesmo_hash_bcrypt_e_normaliza_o_email()
    {
        var (servico, usuario, hasher, uow) = CenarioUsuario();
        var resultado = await servico.RedefinirSenhaAsync("  ADMIN@Example.test ", "nova-senha-segura-2026", default);
        Assert.Equal("admin@example.test", resultado.Email);
        Assert.True(hasher.Verificar("nova-senha-segura-2026", usuario.SenhaHash));
        Assert.False(hasher.Verificar("senha-antiga-123456", usuario.SenhaHash));
        Assert.Equal(1, uow.Gravacoes);
    }

    [Fact]
    public async Task Redefinir_senha_aplica_a_politica_e_exige_usuario_existente()
    {
        var (servico, usuario, hasher, uow) = CenarioUsuario();
        await Assert.ThrowsAsync<ValidacaoException>(() => servico.RedefinirSenhaAsync("admin@example.test", "curta", default));
        await Assert.ThrowsAsync<ValidacaoException>(() => servico.RedefinirSenhaAsync("admin@example.test", new string('á', 40), default));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => servico.RedefinirSenhaAsync("outro@example.test", "nova-senha-segura-2026", default));
        Assert.True(hasher.Verificar("senha-antiga-123456", usuario.SenhaHash));
        Assert.Equal(0, uow.Gravacoes);
    }

    private static UsuarioAcessoDto Dto(string email) => new(Guid.NewGuid(), "Admin", email, PerfilUsuario.Administrador, "AD", true, DateTime.UtcNow);

    [Fact]
    public async Task Ferramenta_recusa_senha_na_linha_de_comando_sem_ler_nem_alterar()
    {
        var saida = new StringWriter();
        var lidas = 0; var chamadas = 0;
        var codigo = await FerramentaRedefinirSenha.ExecutarAsync(["redefinir-senha", "admin@example.test", "senha-na-linha-123"],
            _ => { lidas++; return "x"; }, saida, (e, s, ct) => { chamadas++; return Task.FromResult(Dto(e)); }, default);
        Assert.Equal(2, codigo);
        Assert.Equal(0, lidas);
        Assert.Equal(0, chamadas);
        Assert.DoesNotContain("senha-na-linha-123", saida.ToString());
    }

    [Fact]
    public async Task Ferramenta_exige_confirmacao_igual_e_nao_exibe_a_senha()
    {
        var respostas = new Queue<string?>(["senha-nova-segura-1", "senha-diferente-22"]);
        var chamadas = 0;
        var saida = new StringWriter();
        var codigo = await FerramentaRedefinirSenha.ExecutarAsync(["redefinir-senha", "admin@example.test"],
            _ => respostas.Dequeue(), saida, (e, s, ct) => { chamadas++; return Task.FromResult(Dto(e)); }, default);
        Assert.Equal(4, codigo);
        Assert.Equal(0, chamadas);

        respostas = new Queue<string?>(["senha-nova-segura-1", "senha-nova-segura-1"]);
        string? recebida = null;
        saida = new StringWriter();
        codigo = await FerramentaRedefinirSenha.ExecutarAsync(["redefinir-senha", " admin@example.test "],
            _ => respostas.Dequeue(), saida, (e, s, ct) => { recebida = s; return Task.FromResult(Dto(e)); }, default);
        Assert.Equal(0, codigo);
        Assert.Equal("senha-nova-segura-1", recebida);
        Assert.Contains("admin@example.test", saida.ToString());
        Assert.DoesNotContain("senha-nova-segura-1", saida.ToString());
    }

    [Fact]
    public async Task Ferramenta_informa_usuario_inexistente_e_politica_sem_alterar()
    {
        var saida = new StringWriter();
        var codigo = await FerramentaRedefinirSenha.ExecutarAsync(["redefinir-senha", "ninguem@example.test"],
            _ => "senha-nova-segura-1", saida, (e, s, ct) => throw new NaoEncontradoException("Usuário"), default);
        Assert.Equal(3, codigo);
        codigo = await FerramentaRedefinirSenha.ExecutarAsync(["redefinir-senha", "admin@example.test"],
            _ => null, saida, (e, s, ct) => Task.FromResult(Dto(e)), default);
        Assert.Equal(2, codigo);
    }

    [Fact]
    public async Task Senha_redefinida_vale_para_o_login_da_api_e_a_anterior_deixa_de_valer()
    {
        await using var factory = new ApiPdfFactory();
        await factory.PrepararSeedAsync();
        using var cliente = factory.CreateClient();
        const string email = "admin@zalekpersonalizados.com.br";
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = SenhaDemo, manterConectado = false })).StatusCode);

        using (var escopo = factory.Services.CreateScope())
            await escopo.ServiceProvider.GetRequiredService<UsuarioService>().RedefinirSenhaAsync(email, "nova-senha-segura-2026", default);

        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = "nova-senha-segura-2026", manterConectado = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha = SenhaDemo, manterConectado = false })).StatusCode);
    }
}
