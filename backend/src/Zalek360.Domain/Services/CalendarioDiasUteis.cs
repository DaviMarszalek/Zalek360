namespace Zalek360.Domain.Services;

/// <summary>
/// Prazo em dias úteis (segunda a sexta). Feriados não são considerados no MVP 1.
/// Ex.: aprovação em 24/09/2026 + 15 dias úteis = 15/10/2026 (conforme protótipo).
/// </summary>
public static class CalendarioDiasUteis
{
    public static DateOnly Adicionar(DateOnly inicio, int diasUteis)
    {
        var data = inicio;
        var contados = 0;
        while (contados < diasUteis)
        {
            data = data.AddDays(1);
            if (data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) contados++;
        }
        return data;
    }
}
