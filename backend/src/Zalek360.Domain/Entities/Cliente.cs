using System.Text.RegularExpressions;
using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Entities;

public sealed record DadosCliente(
    TipoPessoa? TipoPessoa,
    string? Documento,
    string? NomeRazaoSocial,
    string? NomeFantasia,
    string? ContatoNome,
    string? ContatoCargo,
    string? Telefone,
    string? WhatsApp,
    string? Email,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf);

/// <summary>UC01 — Cliente PF ou PJ. RN1: precisa existir antes do orçamento. RN2: CPF/CNPJ único.</summary>
public class Cliente : RaizAgregado
{
    public static readonly string[] Ufs =
    {
        "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB",
        "PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
    };

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public TipoPessoa TipoPessoa { get; private set; }
    /// <summary>CPF (11) ou CNPJ (14) somente com dígitos.</summary>
    public string Documento { get; private set; } = string.Empty;
    public string NomeRazaoSocial { get; private set; } = string.Empty;
    public string? NomeFantasia { get; private set; }
    public string? ContatoNome { get; private set; }
    public string? ContatoCargo { get; private set; }
    public string? Telefone { get; private set; }
    public string? WhatsApp { get; private set; }
    public string? Email { get; private set; }
    public string? EnderecoCep { get; private set; }
    public string? EnderecoLogradouro { get; private set; }
    public string? EnderecoNumero { get; private set; }
    public string? EnderecoComplemento { get; private set; }
    public string? EnderecoBairro { get; private set; }
    public string? EnderecoCidade { get; private set; }
    public string? EnderecoUf { get; private set; }
    /// <summary>Texto normalizado (minúsculo, sem acento) com nome, fantasia, e-mail, documento e telefones.</summary>
    public string BuscaNormalizada { get; private set; } = string.Empty;

    private Cliente() { }

    public string NomeExibicao =>
        TipoPessoa == TipoPessoa.PessoaJuridica && !string.IsNullOrWhiteSpace(NomeFantasia) ? NomeFantasia! : NomeRazaoSocial;

    public string? CidadeUf =>
        EnderecoCidade is null ? null : EnderecoUf is null ? EnderecoCidade : $"{EnderecoCidade}/{EnderecoUf}";

    public static Cliente Criar(DadosCliente dados, DateTime agoraUtc, Guid usuarioId, Guid? id = null)
    {
        var cliente = new Cliente { Id = id ?? Guid.NewGuid() };
        cliente.Aplicar(dados);
        cliente.MarcarCriacao(agoraUtc, usuarioId);
        cliente.RegistrarHistorico(HistoricoEvento.Criar(
            EscopoHistorico.Cliente, TipoEventoHistorico.ClienteCadastrado, cliente.Id, null, null,
            "Cliente cadastrado",
            $"{cliente.NomeExibicao} · {DocumentoFiscal.Rotulo(cliente.TipoPessoa)} {DocumentoFiscal.Formatar(cliente.Documento)}",
            agoraUtc, agoraUtc, usuarioId));
        return cliente;
    }

    public void Atualizar(DadosCliente dados, DateTime agoraUtc, Guid usuarioId)
    {
        var antes = (Documento, NomeRazaoSocial, NomeFantasia, Telefone, WhatsApp, Email, EnderecoCidade);
        Aplicar(dados);
        MarcarAlteracao(agoraUtc, usuarioId);
        IncrementarVersao();

        var alterados = new List<string>();
        if (antes.Documento != Documento) alterados.Add(DocumentoFiscal.Rotulo(TipoPessoa));
        if (antes.NomeRazaoSocial != NomeRazaoSocial || antes.NomeFantasia != NomeFantasia) alterados.Add("nome");
        if (antes.Telefone != Telefone || antes.WhatsApp != WhatsApp || antes.Email != Email) alterados.Add("contato");
        if (antes.EnderecoCidade != EnderecoCidade) alterados.Add("endereço");
        RegistrarHistorico(HistoricoEvento.Criar(
            EscopoHistorico.Cliente, TipoEventoHistorico.ClienteAtualizado, Id, null, null,
            "Cadastro atualizado",
            alterados.Count > 0 ? "Campos alterados: " + string.Join(", ", alterados) : null,
            agoraUtc, agoraUtc, usuarioId));
    }

    /// <summary>Validação completa, devolvendo todos os erros por campo (UI destaca os campos, como no protótipo).</summary>
    public static IReadOnlyDictionary<string, string[]> Validar(DadosCliente d)
    {
        var erros = new ColetorErros();
        if (d.TipoPessoa is null)
        {
            erros.Adicionar("tipoPessoa", "Selecione o tipo de pessoa.");
            return erros.ParaDicionario();
        }

        var pf = d.TipoPessoa == TipoPessoa.PessoaFisica;
        var doc = DocumentoFiscal.Normalizar(d.Documento);
        if (doc.Length == 0)
            erros.Adicionar("documento", pf ? "Informe o CPF." : "Informe o CNPJ.");
        else if (!DocumentoFiscal.Valido(d.TipoPessoa.Value, doc))
            erros.Adicionar("documento", pf ? "CPF inválido. Confira os dígitos." : "CNPJ inválido. Confira os dígitos.");

        var nome = TextoOpcional.Limpar(d.NomeRazaoSocial);
        if (nome is null) erros.Adicionar("nomeRazaoSocial", pf ? "Informe o nome completo." : "Informe a razão social.");
        else if (nome.Length > 150) erros.Adicionar("nomeRazaoSocial", "Use no máximo 150 caracteres.");

        if ((TextoOpcional.Limpar(d.NomeFantasia)?.Length ?? 0) > 100) erros.Adicionar("nomeFantasia", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(d.ContatoNome)?.Length ?? 0) > 100) erros.Adicionar("contatoNome", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(d.ContatoCargo)?.Length ?? 0) > 60) erros.Adicionar("contatoCargo", "Use no máximo 60 caracteres.");

        var telefone = TextoBusca.SomenteDigitos(d.Telefone);
        var whatsapp = TextoBusca.SomenteDigitos(d.WhatsApp);
        var email = TextoOpcional.Limpar(d.Email);

        if (telefone.Length == 0 && whatsapp.Length == 0 && email is null)
        {
            erros.Adicionar("contato", "Informe ao menos um meio de contato.");
        }
        if (telefone.Length > 0 && telefone.Length is < 10 or > 11) erros.Adicionar("telefone", "Telefone inválido. Use DDD + número.");
        if (whatsapp.Length > 0 && whatsapp.Length is < 10 or > 11) erros.Adicionar("whatsApp", "WhatsApp inválido. Use DDD + número.");
        if (email is not null && (email.Length > 150 || !EmailRegex.IsMatch(email))) erros.Adicionar("email", "E-mail inválido.");

        var cep = TextoBusca.SomenteDigitos(d.Cep);
        if (cep.Length > 0 && cep.Length != 8) erros.Adicionar("cep", "CEP inválido.");
        var uf = TextoOpcional.Limpar(d.Uf)?.ToUpperInvariant();
        if (uf is not null && !Ufs.Contains(uf)) erros.Adicionar("uf", "UF inválida.");
        if ((TextoOpcional.Limpar(d.Logradouro)?.Length ?? 0) > 150) erros.Adicionar("logradouro", "Use no máximo 150 caracteres.");
        if ((TextoOpcional.Limpar(d.Cidade)?.Length ?? 0) > 100) erros.Adicionar("cidade", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(d.Numero)?.Length ?? 0) > 20) erros.Adicionar("numero", "Use no máximo 20 caracteres.");
        if ((TextoOpcional.Limpar(d.Complemento)?.Length ?? 0) > 100) erros.Adicionar("complemento", "Use no máximo 100 caracteres.");
        if ((TextoOpcional.Limpar(d.Bairro)?.Length ?? 0) > 100) erros.Adicionar("bairro", "Use no máximo 100 caracteres.");

        return erros.ParaDicionario();
    }

    private void Aplicar(DadosCliente d)
    {
        var erros = Validar(d);
        if (erros.Count > 0)
            throw new DomainException(CodigosErro.ClienteDadosInvalidos, "Corrija os campos destacados.", CategoriaErro.Validacao, erros);

        TipoPessoa = d.TipoPessoa!.Value;
        var pj = TipoPessoa == TipoPessoa.PessoaJuridica;
        Documento = DocumentoFiscal.Normalizar(d.Documento);
        NomeRazaoSocial = d.NomeRazaoSocial!.Trim();
        NomeFantasia = pj ? TextoOpcional.Limpar(d.NomeFantasia) : null;
        ContatoNome = pj ? TextoOpcional.Limpar(d.ContatoNome) : null;
        ContatoCargo = pj ? TextoOpcional.Limpar(d.ContatoCargo) : null;
        Telefone = TextoOpcional.Limpar(TextoBusca.SomenteDigitos(d.Telefone));
        WhatsApp = TextoOpcional.Limpar(TextoBusca.SomenteDigitos(d.WhatsApp));
        Email = TextoOpcional.Limpar(d.Email)?.ToLowerInvariant();
        EnderecoCep = TextoOpcional.Limpar(TextoBusca.SomenteDigitos(d.Cep));
        EnderecoLogradouro = TextoOpcional.Limpar(d.Logradouro);
        EnderecoNumero = TextoOpcional.Limpar(d.Numero);
        EnderecoComplemento = TextoOpcional.Limpar(d.Complemento);
        EnderecoBairro = TextoOpcional.Limpar(d.Bairro);
        EnderecoCidade = TextoOpcional.Limpar(d.Cidade);
        EnderecoUf = TextoOpcional.Limpar(d.Uf)?.ToUpperInvariant();
        BuscaNormalizada = string.Join(' ', new[]
        {
            TextoBusca.Normalizar(NomeRazaoSocial), TextoBusca.Normalizar(NomeFantasia), TextoBusca.Normalizar(ContatoNome),
            Email ?? string.Empty, Documento, Telefone ?? string.Empty, WhatsApp ?? string.Empty
        }.Where(s => s.Length > 0));
    }
}
