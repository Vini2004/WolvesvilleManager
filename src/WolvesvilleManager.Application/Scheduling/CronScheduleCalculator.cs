using Cronos;

namespace WolvesvilleManager.Application.Scheduling;

/// <summary>
/// Calcula ocorrências de expressões cron (5 campos) interpretadas em um fuso horário,
/// devolvendo sempre UTC — formato em que o agendador armazena e compara.
/// </summary>
public static class CronScheduleCalculator
{
    public static DateTime? GetNextOccurrenceUtc(string cronExpression, string timeZoneId, DateTime fromUtc)
    {
        var expression = CronExpression.Parse(cronExpression);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var next = expression.GetNextOccurrence(new DateTimeOffset(fromUtc, TimeSpan.Zero), timeZone);
        return next?.UtcDateTime;
    }

    public static bool IsValidCron(string cronExpression)
    {
        try
        {
            CronExpression.Parse(cronExpression);
            return true;
        }
        catch (CronFormatException)
        {
            return false;
        }
    }

    public static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}
