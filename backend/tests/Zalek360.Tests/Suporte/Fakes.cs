using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Tests.Suporte;

/// <summary>Armazenamento em memória para testar os casos de uso sem banco.</summary>
public sealed class BancoEmMemoria
{
    public List<Usuario> Usuarios { get; } = new();
    public List<Produto> Produtos { get; } = new();
    public List<Cliente> Clientes { get; } = new();
    public List<ObservacaoCliente> Observacoes { get; } = new();
    public List<Orcamento> Orcamentos { get; } = new();
    public List<Pedido> Pedidos { get; } = new();
    public List<ArquivoAnexo> Anexos { get; } = new();
    public List<HistoricoEvento> Historico { get; } = new();
}

public sealed class TransacaoFake : ITransacao
{
    public bool Confirmada { get; private set; }
    public bool Descartada { get; private set; }
    public Task ConfirmarAsync(CancellationToken ct) { Confirmada = true; return Task.CompletedTask; }
    public ValueTask DisposeAsync() { Descartada = true; return ValueTask.CompletedTask; }
}

public sealed class UnidadeDeTrabalhoFake : IUnitOfWork
{
    private readonly BancoEmMemoria _banco;
    public UnidadeDeTrabalhoFake(BancoEmMemoria banco) => _banco = banco;
    public int Gravacoes { get; private set; }
    public List<TransacaoFake> Transacoes { get; } = new();

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        Gravacoes++;
        // Mesmo comportamento do DbContext: coleta os eventos pendentes dos agregados.
        foreach (var agregado in _banco.Clientes.Cast<IPossuiHistorico>().Concat(_banco.Orcamentos).Concat(_banco.Pedidos))
            _banco.Historico.AddRange(agregado.ExtrairHistoricoPendente());
        return Task.FromResult(1);
    }

    public Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct)
    {
        var t = new TransacaoFake();
        Transacoes.Add(t);
        return Task.FromResult<ITransacao>(t);
    }
}

public sealed class NumeradorFake : INumeradorDocumentos
{
    public int ProximoOrcamento { get; set; } = 124;
    public int ProximoPedido { get; set; } = 57;
    public bool FalharAoNumerarPedido { get; set; }

    public Task<int> ProximoNumeroOrcamentoAsync(CancellationToken ct) => Task.FromResult(ProximoOrcamento++);

    public Task<int> ProximoNumeroPedidoAsync(CancellationToken ct) =>
        FalharAoNumerarPedido
            ? throw new InvalidOperationException("Falha simulada ao gerar o número do pedido.")
            : Task.FromResult(ProximoPedido++);
}

public sealed class UsuarioAtualFake : IUsuarioAtual
{
    public bool Autenticado => true;
    public Guid Id { get; set; } = Dados.UsuarioId;
}

public sealed class RepositoriosFake : IUsuarioRepository, IClienteRepository, IProdutoRepository, IOrcamentoRepository, IPedidoRepository, IAnexoRepository
{
    private readonly BancoEmMemoria _b;
    public RepositoriosFake(BancoEmMemoria banco) => _b = banco;

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct) =>
        Task.FromResult(_b.Usuarios.FirstOrDefault(u => u.Email == emailNormalizado));
    Task<Usuario?> IUsuarioRepository.ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Usuarios.FirstOrDefault(u => u.Id == id));

    Task<Cliente?> IClienteRepository.ObterParaAlteracaoAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Clientes.FirstOrDefault(c => c.Id == id));
    public Task<bool> ExisteAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Clientes.Any(c => c.Id == id));
    public void Adicionar(Cliente cliente) => _b.Clientes.Add(cliente);
    public void AdicionarObservacao(ObservacaoCliente observacao) => _b.Observacoes.Add(observacao);

    Task<IReadOnlyList<Produto>> IProdutoRepository.ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Produto>>(_b.Produtos.Where(p => ids.Contains(p.Id)).ToList());

    Task<Orcamento?> IOrcamentoRepository.ObterParaAlteracaoAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Orcamentos.FirstOrDefault(o => o.Id == id));
    public Task<IReadOnlyList<Orcamento>> ListarParaExpiracaoAsync(DateOnly hojeLocal, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Orcamento>>(_b.Orcamentos.Where(o => o.Situacao == SituacaoOrcamento.AguardandoRetorno && o.Validade < hojeLocal).ToList());
    public void Adicionar(Orcamento orcamento) => _b.Orcamentos.Add(orcamento);

    Task<Pedido?> IPedidoRepository.ObterParaAlteracaoAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Pedidos.FirstOrDefault(p => p.Id == id));
    public void Adicionar(Pedido pedido) => _b.Pedidos.Add(pedido);

    Task<ArquivoAnexo?> IAnexoRepository.ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_b.Anexos.FirstOrDefault(a => a.Id == id));
    Task<IReadOnlyList<ArquivoAnexo>> IAnexoRepository.ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ArquivoAnexo>>(_b.Anexos.Where(a => ids.Contains(a.Id)).ToList());
    public Task<IReadOnlyList<ArquivoAnexo>> ListarPendentesCriadosAntesDeAsync(DateTime limiteUtc, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ArquivoAnexo>>(_b.Anexos.Where(a => a.Pendente && a.CriadoEm < limiteUtc).ToList());
    public Task<bool> ChaveUsadaPorOutroAnexoAsync(string chave, Guid excetoId, CancellationToken ct) =>
        Task.FromResult(_b.Anexos.Any(a => a.ChaveArmazenamento == chave && a.Id != excetoId));
    public void Adicionar(ArquivoAnexo anexo) => _b.Anexos.Add(anexo);
    public void Remover(ArquivoAnexo anexo) => _b.Anexos.Remove(anexo);
}

/// <summary>Consultas mínimas (somente o necessário para os casos de uso devolverem o resultado).</summary>
public sealed class ConsultasFake : IClienteQueries, IOrcamentoQueries
{
    private readonly BancoEmMemoria _b;
    public ConsultasFake(BancoEmMemoria banco) => _b = banco;

    public Task<ClienteDetalheDto> ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var c = _b.Clientes.FirstOrDefault(x => x.Id == id) ?? throw new NaoEncontradoException("Cliente");
        return Task.FromResult(new ClienteDetalheDto(c.Id, c.TipoPessoa, c.Documento, c.NomeRazaoSocial, c.NomeFantasia, c.NomeExibicao,
            c.ContatoNome, c.ContatoCargo, c.Telefone, c.WhatsApp, c.Email,
            new EnderecoDto(c.EnderecoCep, c.EnderecoLogradouro, c.EnderecoNumero, c.EnderecoComplemento, c.EnderecoBairro, c.EnderecoCidade, c.EnderecoUf),
            c.CriadoEm, 0, 0, c.Versao));
    }

    public Task<ClienteExistenteDto?> ObterPorDocumentoAsync(string documentoDigitos, Guid? excetoId, CancellationToken ct)
    {
        var c = _b.Clientes.FirstOrDefault(x => x.Documento == documentoDigitos && x.Id != excetoId);
        return Task.FromResult(c is null ? null : new ClienteExistenteDto(c.Id, c.NomeExibicao, c.TipoPessoa, c.Documento, c.CidadeUf, null));
    }

    public Task<IReadOnlyList<ObservacaoClienteDto>> ListarObservacoesAsync(Guid clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ObservacaoClienteDto>>(_b.Observacoes.Where(o => o.ClienteId == clienteId)
            .Select(o => new ObservacaoClienteDto(o.Id, o.Texto, o.CriadoEm, "Maria Silva", "MS")).ToList());

    Task<OrcamentoDetalheDto> IOrcamentoQueries.ObterDetalheAsync(Guid id, CancellationToken ct)
    {
        var o = _b.Orcamentos.FirstOrDefault(x => x.Id == id) ?? throw new NaoEncontradoException("Orçamento");
        var p = _b.Pedidos.FirstOrDefault(x => x.OrcamentoOrigemId == id);
        return Task.FromResult(new OrcamentoDetalheDto(o.Id, o.Numero, o.Situacao,
            new ClienteCardDto(o.ClienteId, "Cliente", TipoPessoa.PessoaJuridica, "", null, null, null, null, null, null),
            o.DataOrcamento, o.CriadoEm, o.Validade, o.PrazoEstimadoDiasUteis, o.Validade, o.CondicaoPagamento, o.ObservacoesComerciais,
            o.ResponsavelId, "Maria Silva", Array.Empty<ItemDetalheDto>(),
            new ResumoFinanceiroDto(o.Itens.Count, o.TotalUnidades, o.SubtotalProdutos, o.ValorPersonalizacao, o.Desconto, o.ValorTotal),
            o.Decisao is { } d ? new DecisaoDto(d.Resultado, d.MeioContato, d.DataHora, d.Motivo, d.Observacao, "Maria Silva", d.Automatica) : null,
            p is null ? null : new PedidoVinculadoDto(p.Id, p.Numero, p.Situacao, p.CriadoEm, p.PrazoEntrega),
            o.AguardandoRetornoDesde, o.DataUltimaEdicao, o.PodeSerEditado, o.Versao));
    }

    public Task<IReadOnlyList<ContatoDto>> ListarContatosAsync(Guid id, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ContatoDto>>(_b.Orcamentos.First(o => o.Id == id).Contatos
            .Select(c => new ContatoDto(c.Id, c.Tipo, c.DataHora, c.Observacao, "Maria Silva", c.CriadoEm)).ToList());

    public Task<PaginaResultado<ClienteListaItemDto>> ListarAsync(ClienteFiltro filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<string>> ListarCidadesAsync(CancellationToken ct) => throw new NotSupportedException();
    public Task<ClienteVisaoGeralDto> ObterVisaoGeralAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<ListaFiltradaDto<OrcamentoResumoDto>> ListarOrcamentosAsync(Guid clienteId, FiltroOrcamentosCliente filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<ListaFiltradaDto<PedidoResumoDto>> ListarPedidosAsync(Guid clienteId, FiltroPedidosCliente filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid clienteId, FiltroHistoricoCliente filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<ListaComContagemDto<OrcamentoListaItemDto>> ListarAsync(OrcamentoFiltro filtro, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<ItemDetalheDto>> ListarItensAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    public Task<IReadOnlyList<HistoricoEventoDto>> ObterHistoricoAsync(Guid id, string? categoria, CancellationToken ct) => throw new NotSupportedException();
}

/// <summary>Monta os serviços da aplicação sobre os fakes.</summary>
public sealed class Cenario
{
    public BancoEmMemoria Banco { get; } = new();
    public RelogioFixo Relogio { get; } = new();
    public NumeradorFake Numerador { get; } = new();
    public UsuarioAtualFake Usuario { get; } = new();
    public UnidadeDeTrabalhoFake Uow { get; }
    public RepositoriosFake Repos { get; }
    public ConsultasFake Consultas { get; }
    public Produto Polo { get; }
    public Cliente Metalurgica { get; }

    public Cenario()
    {
        Uow = new UnidadeDeTrabalhoFake(Banco);
        Repos = new RepositoriosFake(Banco);
        Consultas = new ConsultasFake(Banco);
        Banco.Usuarios.Add(Usuario_("Maria Silva"));
        Polo = Produto.Criar("Camisa Polo", "Camiseta", "Camisa polo piquet");
        Banco.Produtos.Add(Polo);
        Metalurgica = Cliente.Criar(Dados.ClientePj(Dados.CnpjMetalurgica, "Metalúrgica Horizonte Ltda.") with { NomeFantasia = null },
            Dados.Agora.AddDays(-400), Dados.UsuarioId);
        Banco.Clientes.Add(Metalurgica);
        Banco.Historico.AddRange(Metalurgica.ExtrairHistoricoPendente());
    }

    private static Domain.Entities.Usuario Usuario_(string nome) =>
        Domain.Entities.Usuario.Criar(nome, "maria.silva@zalekpersonalizados.com.br", "hash", PerfilUsuario.Atendente, Dados.Agora.AddYears(-1), Dados.UsuarioId);

    public Application.Services.OrcamentoService Orcamentos() => new(Repos, Repos, Repos, Repos, Repos, Repos, Consultas, Numerador, Uow, Relogio, Usuario);
    public Application.Services.ClienteService Clientes() => new(Repos, Consultas, Uow, Relogio, Usuario);
    public Application.Services.ExpiracaoOrcamentoService Expiracao() => new(Repos, Uow, Relogio);

    public static OrcamentoRequest Requisicao(Guid clienteId, Guid produtoId, params ItemOrcamentoRequest[] itens) =>
        new(clienteId, Dados.UsuarioId, Dados.Hoje.AddDays(15), 15, "50% na aprovação + 50% na entrega (PIX)", null, 0m, itens, null);

    public static ItemOrcamentoRequest ItemReq(Guid produtoId, int quantidade = 150, decimal unitario = 38m, decimal personalizacao = 8m,
        params Guid[] anexos) =>
        new(null, produtoId, null, quantidade, unitario, personalizacao, "Bordado", "Azul marinho", "2 cores", "9 × 5 cm",
            "Peito esquerdo", "Logo oficial", null, anexos);
}
