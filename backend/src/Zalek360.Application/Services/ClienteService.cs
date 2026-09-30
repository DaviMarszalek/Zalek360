using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Application.Services;

/// <summary>UC01 — Cadastrar Cliente (e manutenção do cadastro).</summary>
public sealed class ClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly IClienteQueries _consultas;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IUsuarioAtual _usuario;

    public ClienteService(IClienteRepository clientes, IClienteQueries consultas, IUnitOfWork uow, IClock clock, IUsuarioAtual usuario)
    {
        _clientes = clientes;
        _consultas = consultas;
        _uow = uow;
        _clock = clock;
        _usuario = usuario;
    }

    public async Task<ClienteDetalheDto> CriarAsync(ClienteRequest request, CancellationToken ct)
    {
        var dados = Mapear(request);
        ValidarOuLancar(dados);
        await GarantirDocumentoDisponivelAsync(dados, null, ct); // RN2

        var agora = _clock.AgoraUtc;
        var cliente = Cliente.Criar(dados, agora, _usuario.Id);
        _clientes.Adicionar(cliente);
        if (!string.IsNullOrWhiteSpace(request.ObservacaoInicial))
            _clientes.AdicionarObservacao(ObservacaoCliente.Criar(cliente.Id, request.ObservacaoInicial, agora, _usuario.Id));

        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(cliente.Id, ct);
    }

    public async Task<ClienteDetalheDto> AtualizarAsync(Guid id, ClienteRequest request, CancellationToken ct)
    {
        var cliente = await _clientes.ObterParaAlteracaoAsync(id, ct) ?? throw new NaoEncontradoException("Cliente");
        Concorrencia.VerificarVersao(cliente.Versao, request.Versao);

        var dados = Mapear(request);
        ValidarOuLancar(dados);
        await GarantirDocumentoDisponivelAsync(dados, id, ct);

        cliente.Atualizar(dados, _clock.AgoraUtc, _usuario.Id);
        await _uow.SaveChangesAsync(ct);
        return await _consultas.ObterDetalheAsync(id, ct);
    }

    /// <summary>Validação "ao sair do campo": dígitos verificadores + disponibilidade (sequência UC01, passos 4–7).</summary>
    public async Task<VerificacaoDocumentoDto> VerificarDocumentoAsync(string? documento, TipoPessoa? tipo, Guid? excetoId, CancellationToken ct)
    {
        var digitos = DocumentoFiscal.Normalizar(documento);
        var tipoEfetivo = tipo ?? (digitos.Length == 11 ? TipoPessoa.PessoaFisica : TipoPessoa.PessoaJuridica);
        var rotulo = DocumentoFiscal.Rotulo(tipoEfetivo);
        if (digitos.Length == 0)
            return new VerificacaoDocumentoDto(false, false, $"Informe o {rotulo}.", null);
        if (!DocumentoFiscal.Valido(tipoEfetivo, digitos))
            return new VerificacaoDocumentoDto(false, false, $"{rotulo} inválido. Confira os dígitos.", null);

        var existente = await _consultas.ObterPorDocumentoAsync(digitos, excetoId, ct);
        return existente is null
            ? new VerificacaoDocumentoDto(true, true, $"{rotulo} válido e disponível para cadastro.", null)
            : new VerificacaoDocumentoDto(true, false, $"Este {rotulo} já está cadastrado.", existente);
    }

    public async Task<ObservacaoClienteDto> AdicionarObservacaoAsync(Guid clienteId, ObservacaoRequest request, CancellationToken ct)
    {
        if (!await _clientes.ExisteAsync(clienteId, ct)) throw new NaoEncontradoException("Cliente");
        var observacao = ObservacaoCliente.Criar(clienteId, request.Texto, _clock.AgoraUtc, _usuario.Id);
        _clientes.AdicionarObservacao(observacao);
        await _uow.SaveChangesAsync(ct);
        var lista = await _consultas.ListarObservacoesAsync(clienteId, ct);
        return lista.First(o => o.Id == observacao.Id);
    }

    private async Task GarantirDocumentoDisponivelAsync(DadosCliente dados, Guid? excetoId, CancellationToken ct)
    {
        var digitos = DocumentoFiscal.Normalizar(dados.Documento);
        var existente = await _consultas.ObterPorDocumentoAsync(digitos, excetoId, ct);
        if (existente is null) return;
        var rotulo = DocumentoFiscal.Rotulo(dados.TipoPessoa!.Value);
        throw new ConflitoException(
            CodigosErro.ClienteDocumentoDuplicado,
            "Já existe um cliente cadastrado com este CPF/CNPJ.",
            new Dictionary<string, string[]> { ["documento"] = new[] { $"Este {rotulo} já está cadastrado." } },
            new { clienteExistente = existente });
    }

    private static void ValidarOuLancar(DadosCliente dados)
    {
        var erros = Cliente.Validar(dados);
        if (erros.Count > 0)
            throw new ValidacaoException(CodigosErro.ClienteDadosInvalidos, "Corrija os campos destacados.", erros);
    }

    private static DadosCliente Mapear(ClienteRequest r) => new(
        r.TipoPessoa, r.Documento, r.NomeRazaoSocial, r.NomeFantasia, r.ContatoNome, r.ContatoCargo,
        r.Telefone, r.WhatsApp, r.Email, r.Cep, r.Logradouro, r.Numero, r.Complemento, r.Bairro, r.Cidade, r.Uf);
}
