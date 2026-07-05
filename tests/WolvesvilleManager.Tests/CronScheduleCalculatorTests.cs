using WolvesvilleManager.Application.Scheduling;

namespace WolvesvilleManager.Tests;

public class CronScheduleCalculatorTests
{
    [Fact]
    public void GetNextOccurrenceUtc_InterpretaCronNoFusoInformado()
    {
        // Sexta 20h em São Paulo (UTC-3) = 23h UTC.
        // Partindo de uma quinta-feira, a próxima ocorrência é a sexta seguinte.
        var fromUtc = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc); // quinta

        var next = CronScheduleCalculator.GetNextOccurrenceUtc("0 20 * * FRI", "America/Sao_Paulo", fromUtc);

        Assert.Equal(new DateTime(2026, 7, 3, 23, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void GetNextOccurrenceUtc_EmUtc_NaoAplicaDeslocamento()
    {
        var fromUtc = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

        var next = CronScheduleCalculator.GetNextOccurrenceUtc("30 14 * * *", "UTC", fromUtc);

        Assert.Equal(new DateTime(2026, 7, 2, 14, 30, 0, DateTimeKind.Utc), next);
    }

    [Theory]
    [InlineData("0 20 * * FRI", true)]
    [InlineData("*/15 * * * *", true)]
    [InlineData("0 20 * *", false)]      // faltando campo
    [InlineData("99 99 * * *", false)]   // valores fora do intervalo
    [InlineData("abc", false)]
    public void IsValidCron_ValidaFormato(string cron, bool expected) =>
        Assert.Equal(expected, CronScheduleCalculator.IsValidCron(cron));

    [Theory]
    [InlineData("America/Sao_Paulo", true)]
    [InlineData("UTC", true)]
    [InlineData("Fuso/Inexistente", false)]
    public void IsValidTimeZone_ValidaId(string timeZoneId, bool expected) =>
        Assert.Equal(expected, CronScheduleCalculator.IsValidTimeZone(timeZoneId));
}
