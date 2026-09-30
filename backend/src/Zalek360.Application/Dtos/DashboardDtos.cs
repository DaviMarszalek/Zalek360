namespace Zalek360.Application.Dtos;

public sealed record IndicadoresDto(
    int OrcamentosEmElaboracao,
    int OrcamentosCriadosHoje,
    int AguardandoRetorno,
    int AguardandoVencemEm3Dias,
    int AprovadosNoMes,
    decimal ValorAprovadoNoMes,
    int PedidosEmAndamento,
    int PedidosConcluidosNoMes,
    string MesReferencia);

public sealed record AguardandoRetornoItemDto(
    Guid Id, int Numero, string Cliente, decimal ValorTotal, DateTime? ApresentadoEm, DateOnly Validade, int DiasParaVencer);

public sealed record DashboardDto(
    IndicadoresDto Indicadores,
    IReadOnlyList<OrcamentoListaItemDto> OrcamentosRecentes,
    IReadOnlyList<AguardandoRetornoItemDto> AguardandoRetorno,
    int TotalAguardandoRetorno);
