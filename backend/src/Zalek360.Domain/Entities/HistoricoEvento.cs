using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Entities;

/// <summary>
/// Log de auditoria de negócio, imutável (RNF 5.2.2). Unifica o histórico de cliente, orçamento e pedido:
/// todo evento conhece o cliente, o que permite o histórico consolidado do UC05 com uma única consulta.
/// </summary>
public class HistoricoEvento : Entidade
{
    public EscopoHistorico Escopo { get; private set; }
    public TipoEventoHistorico Tipo { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid? OrcamentoId { get; private set; }
    public Guid? PedidoId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public string? SituacaoAnterior { get; private set; }
    public string? SituacaoNova { get; private set; }
    public MeioContato? MeioContato { get; private set; }
    /// <summary>Momento de negócio (ex.: data/hora informada para um contato feito fora do sistema).</summary>
    public DateTime OcorridoEm { get; private set; }
    /// <summary>Momento em que o evento foi gravado no sistema.</summary>
    public DateTime RegistradoEm { get; private set; }
    /// <summary>Nulo quando o evento foi produzido automaticamente pelo sistema.</summary>
    public Guid? UsuarioId { get; private set; }
    public Usuario? Usuario { get; private set; }

    private HistoricoEvento() { }

    public static HistoricoEvento Criar(
        EscopoHistorico escopo,
        TipoEventoHistorico tipo,
        Guid clienteId,
        Guid? orcamentoId,
        Guid? pedidoId,
        string titulo,
        string? descricao,
        DateTime ocorridoEm,
        DateTime registradoEm,
        Guid? usuarioId,
        string? situacaoAnterior = null,
        string? situacaoNova = null,
        MeioContato? meio = null) => new()
    {
        Escopo = escopo,
        Tipo = tipo,
        ClienteId = clienteId,
        OrcamentoId = orcamentoId,
        PedidoId = pedidoId,
        Titulo = titulo.Length > 200 ? titulo[..200] : titulo,
        Descricao = descricao is { Length: > 4000 } ? descricao[..3997] + "..." : descricao,
        OcorridoEm = ocorridoEm,
        RegistradoEm = registradoEm,
        UsuarioId = usuarioId,
        SituacaoAnterior = situacaoAnterior,
        SituacaoNova = situacaoNova,
        MeioContato = meio
    };
}
