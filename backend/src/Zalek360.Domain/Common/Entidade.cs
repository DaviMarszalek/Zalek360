namespace Zalek360.Domain.Common;

/// <summary>Base de todas as entidades. O identificador é gerado no domínio (UUID), antes da persistência.</summary>
public abstract class Entidade
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

/// <summary>Entidade com trilha de auditoria (CreatedAt/CreatedBy/UpdatedAt/UpdatedBy).</summary>
public abstract class EntidadeAuditavel : Entidade
{
    public DateTime CriadoEm { get; protected set; }
    public Guid? CriadoPorId { get; protected set; }
    public DateTime? AtualizadoEm { get; protected set; }
    public Guid? AtualizadoPorId { get; protected set; }

    protected void MarcarCriacao(DateTime agoraUtc, Guid? usuarioId)
    {
        CriadoEm = agoraUtc;
        CriadoPorId = usuarioId;
    }

    protected void MarcarAlteracao(DateTime agoraUtc, Guid? usuarioId)
    {
        AtualizadoEm = agoraUtc;
        AtualizadoPorId = usuarioId;
    }
}

/// <summary>Agregados que produzem eventos de histórico de negócio (log imutável).</summary>
public interface IPossuiHistorico
{
    IReadOnlyList<Entities.HistoricoEvento> ExtrairHistoricoPendente();
}

/// <summary>Raiz de agregado: controla versão (concorrência otimista) e eventos de histórico pendentes.</summary>
public abstract class RaizAgregado : EntidadeAuditavel, IPossuiHistorico
{
    private readonly List<Entities.HistoricoEvento> _historicoPendente = new();

    /// <summary>Token de concorrência otimista, incrementado a cada alteração do agregado.</summary>
    public int Versao { get; protected set; }

    protected void RegistrarHistorico(Entities.HistoricoEvento evento) => _historicoPendente.Add(evento);

    protected void IncrementarVersao() => Versao++;

    public IReadOnlyList<Entities.HistoricoEvento> ExtrairHistoricoPendente()
    {
        var copia = _historicoPendente.ToList();
        _historicoPendente.Clear();
        return copia;
    }
}
