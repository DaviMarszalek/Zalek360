using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;
using Zalek360.Tests.Suporte;

namespace Zalek360.Tests.Dominio;

public class ClienteTests
{
    [Fact]
    public void Cadastra_cliente_pj_valido_com_documento_normalizado()
    {
        var cliente = Cliente.Criar(Dados.ClientePj("38.221.907/0001-53"), Dados.Agora, Dados.UsuarioId);

        Assert.Equal("38221907000153", cliente.Documento);
        Assert.Equal("Academia Movimento", cliente.NomeExibicao);
        Assert.Equal(TipoPessoa.PessoaJuridica, cliente.TipoPessoa);
        Assert.Single(cliente.ExtrairHistoricoPendente(), e => e.Tipo == TipoEventoHistorico.ClienteCadastrado);
    }

    [Fact]
    public void Cadastra_cliente_pf_valido()
    {
        var cliente = Cliente.Criar(Dados.ClientePf(), Dados.Agora, Dados.UsuarioId);
        Assert.Equal("João Martins", cliente.NomeExibicao);
    }

    [Fact]
    public void Rejeita_cnpj_com_digito_verificador_invalido()
    {
        var erros = Cliente.Validar(Dados.ClientePj("38221907000140"));
        Assert.True(erros.ContainsKey("documento"));
    }

    [Fact]
    public void Rejeita_cpf_com_digitos_repetidos()
    {
        Assert.False(DocumentoFiscal.Valido(TipoPessoa.PessoaFisica, "11111111111"));
        Assert.True(DocumentoFiscal.Valido(TipoPessoa.PessoaFisica, Dados.CpfJoao));
        Assert.True(DocumentoFiscal.Valido(TipoPessoa.PessoaJuridica, Dados.CnpjMetalurgica));
    }

    [Fact]
    public void Exige_tipo_de_pessoa()
    {
        var vazio = new DadosCliente(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        Assert.True(Cliente.Validar(vazio).ContainsKey("tipoPessoa"));
    }

    [Fact]
    public void Exige_documento_nome_e_ao_menos_um_meio_de_contato()
    {
        var vazio = new DadosCliente(TipoPessoa.PessoaJuridica, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var erros = Cliente.Validar(vazio);
        Assert.True(erros.ContainsKey("documento"));
        Assert.True(erros.ContainsKey("nomeRazaoSocial"));
        Assert.True(erros.Count >= 3);
    }

    [Fact]
    public void Criar_com_dados_invalidos_lanca_excecao_de_validacao()
    {
        var ex = Assert.Throws<DomainException>(() => Cliente.Criar(Dados.ClientePj("123"), Dados.Agora, Dados.UsuarioId));
        Assert.Equal(CategoriaErro.Validacao, ex.Categoria);
    }
}
