using Zalek360.Domain.Common;
using Zalek360.Domain.Enums;

namespace Zalek360.Domain.Entities;

/// <summary>
/// UC03 — decisão comunicada pelo cliente fora do sistema e registrada pelo atendente (RN7: meio de contato +
/// data/hora obrigatórios). A expiração automática também gera uma decisão (sem usuário e sem meio de contato).
/// Relação 1:0..1 com Orçamento.
/// </summary>
public class DecisaoOrcamento : Entidade
{
    public static readonly string[] MotivosSugeridos =
        { "Preço", "Prazo de entrega", "Escolheu outro fornecedor", "Desistiu da compra", "Mudança no evento", "Outro" };

    public Guid OrcamentoId { get; private set; }
    public ResultadoDecisao Resultado { get; private set; }
    public MeioContato? MeioContato { get; private set; }
    public DateTime DataHora { get; private set; }
    public string? Motivo { get; private set; }
    public string? Observacao { get; private set; }
    public Guid? RegistradoPorId { get; private set; }
    public Usuario? RegistradoPor { get; private set; }
    public bool Automatica { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private DecisaoOrcamento() { }

    internal static DecisaoOrcamento Manual(Guid orcamentoId, ResultadoDecisao resultado, MeioContato meio, DateTime dataHoraUtc,
        string? motivo, string? observacao, Guid usuarioId, DateTime agoraUtc) => new()
    {
        OrcamentoId = orcamentoId,
        Resultado = resultado,
        MeioContato = meio,
        DataHora = dataHoraUtc,
        Motivo = resultado is ResultadoDecisao.Recusado or ResultadoDecisao.Cancelado ? TextoOpcional.Limpar(motivo) : null,
        Observacao = TextoOpcional.Limpar(observacao),
        RegistradoPorId = usuarioId,
        Automatica = false,
        CriadoEm = agoraUtc
    };

    internal static DecisaoOrcamento ExpiracaoAutomatica(Guid orcamentoId, DateTime momentoExpiracaoUtc, DateTime agoraUtc) => new()
    {
        OrcamentoId = orcamentoId,
        Resultado = ResultadoDecisao.Expirado,
        MeioContato = null,
        DataHora = momentoExpiracaoUtc,
        RegistradoPorId = null,
        Automatica = true,
        CriadoEm = agoraUtc
    };
}
