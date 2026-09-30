namespace Zalek360.Domain.Enums;

/// <summary>Rótulos em português usados em mensagens e no histórico. A UI possui o seu próprio mapeamento.</summary>
public static class Rotulos
{
    public static string De(SituacaoOrcamento s) => s switch
    {
        SituacaoOrcamento.EmElaboracao => "Em Elaboração",
        SituacaoOrcamento.AguardandoRetorno => "Aguardando Retorno",
        SituacaoOrcamento.Aprovado => "Aprovado",
        SituacaoOrcamento.Recusado => "Recusado",
        SituacaoOrcamento.Expirado => "Expirado",
        SituacaoOrcamento.Cancelado => "Cancelado",
        _ => s.ToString()
    };

    public static string De(SituacaoPedido s) => s switch
    {
        SituacaoPedido.Aberto => "Aberto",
        SituacaoPedido.EmProducao => "Em produção",
        SituacaoPedido.Pronto => "Pronto",
        SituacaoPedido.Entregue => "Entregue",
        SituacaoPedido.Cancelado => "Cancelado",
        _ => s.ToString()
    };

    public static string De(MeioContato m) => m switch
    {
        MeioContato.WhatsApp => "WhatsApp",
        MeioContato.Ligacao => "Ligação",
        MeioContato.Email => "E-mail",
        MeioContato.Presencial => "Presencial",
        MeioContato.Outro => "Outro",
        _ => m.ToString()
    };

    public static string De(ResultadoDecisao r) => r switch
    {
        ResultadoDecisao.Aprovado => "Aprovado",
        ResultadoDecisao.Recusado => "Recusado",
        ResultadoDecisao.Cancelado => "Cancelado",
        ResultadoDecisao.Expirado => "Expirado",
        _ => r.ToString()
    };

    public static SituacaoOrcamento ParaSituacao(ResultadoDecisao r) => r switch
    {
        ResultadoDecisao.Aprovado => SituacaoOrcamento.Aprovado,
        ResultadoDecisao.Recusado => SituacaoOrcamento.Recusado,
        ResultadoDecisao.Cancelado => SituacaoOrcamento.Cancelado,
        ResultadoDecisao.Expirado => SituacaoOrcamento.Expirado,
        _ => throw new ArgumentOutOfRangeException(nameof(r))
    };
}
