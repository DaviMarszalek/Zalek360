using Zalek360.Application.Interfaces;

namespace Zalek360.Application.Common;

/// <summary>
/// Implementação de <see cref="IClock"/> com fuso de negócio configurável (padrão America/Sao_Paulo).
/// O Brasil não adota horário de verão desde 2019; se o fuso não existir no SO, usa UTC−3 fixo.
/// </summary>
public class RelogioComFusoHorario : IClock
{
    private readonly TimeZoneInfo _fuso;

    public RelogioComFusoHorario(string? idFuso = "America/Sao_Paulo")
    {
        _fuso = ObterFuso(idFuso ?? "America/Sao_Paulo");
    }

    public virtual DateTime AgoraUtc => DateTime.UtcNow;

    public DateOnly HojeLocal => ParaDataLocal(AgoraUtc);

    public DateOnly ParaDataLocal(DateTime utc) => DateOnly.FromDateTime(ParaHoraLocal(utc));

    public DateTime ParaHoraLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuso);

    public DateTime InicioDoDiaLocalEmUtc(DateOnly data) =>
        TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), _fuso);

    private static TimeZoneInfo ObterFuso(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Horário de Brasília", "Horário de Brasília");
        }
    }
}
