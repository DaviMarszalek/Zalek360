using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Entities;

/// <summary>
/// RF 4.1.2.9 — contato realizado com o cliente fora do sistema (WhatsApp, ligação, e-mail, presencial, outro).
/// Registro histórico imutável: não altera a situação do orçamento.
/// </summary>
public class ContatoOrcamento : Entidade
{
    public Guid OrcamentoId { get; private set; }
    public MeioContato Tipo { get; private set; }
    public DateTime DataHora { get; private set; }
    public string Observacao { get; private set; } = string.Empty;
    public Guid RegistradoPorId { get; private set; }
    public Usuario? RegistradoPor { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private ContatoOrcamento() { }

    internal static ContatoOrcamento Criar(Guid orcamentoId, MeioContato tipo, DateTime dataHoraUtc, string observacao, Guid usuarioId, DateTime agoraUtc) => new()
    {
        OrcamentoId = orcamentoId,
        Tipo = tipo,
        DataHora = dataHoraUtc,
        Observacao = observacao,
        RegistradoPorId = usuarioId,
        CriadoEm = agoraUtc
    };
}
