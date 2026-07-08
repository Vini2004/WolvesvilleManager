using WolvesvilleManager.Infrastructure.Scheduling;

namespace WolvesvilleManager.Tests;

public class CronJobOrgTranslatorTests
{
    [Fact]
    public void Weekly_single_day()
    {
        var s = CronJobOrgTranslator.ToSchedule("0 14 * * MON");
        Assert.Equal(new[] { 0 }, s.Minutes);
        Assert.Equal(new[] { 14 }, s.Hours);
        Assert.Equal(new[] { -1 }, s.Mdays);
        Assert.Equal(new[] { -1 }, s.Months);
        Assert.Equal(new[] { 1 }, s.Wdays);
    }

    [Fact]
    public void Daily_uses_every_for_day_fields()
    {
        var s = CronJobOrgTranslator.ToSchedule("30 9 * * *");
        Assert.Equal(new[] { 30 }, s.Minutes);
        Assert.Equal(new[] { 9 }, s.Hours);
        Assert.Equal(new[] { -1 }, s.Wdays);
        Assert.Equal(new[] { -1 }, s.Mdays);
    }

    [Fact]
    public void Monthly_uses_day_of_month()
    {
        var s = CronJobOrgTranslator.ToSchedule("0 20 1 * *");
        Assert.Equal(new[] { 1 }, s.Mdays);
        Assert.Equal(new[] { -1 }, s.Wdays);
    }

    [Fact]
    public void Weekday_list_and_names()
    {
        var s = CronJobOrgTranslator.ToSchedule("0 14 * * MON,WED,FRI");
        Assert.Equal(new[] { 1, 3, 5 }, s.Wdays);
    }

    [Fact]
    public void Sunday_maps_to_zero_from_name_and_from_seven()
    {
        Assert.Equal(new[] { 0 }, CronJobOrgTranslator.ToSchedule("0 12 * * SUN").Wdays);
        Assert.Equal(new[] { 0 }, CronJobOrgTranslator.ToSchedule("0 12 * * 7").Wdays);
    }

    [Fact]
    public void Step_and_range_expand()
    {
        Assert.Equal(new[] { 0, 15, 30, 45 }, CronJobOrgTranslator.ToSchedule("*/15 14 * * *").Minutes);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, CronJobOrgTranslator.ToSchedule("0 14 * * 1-5").Wdays);
    }

    [Fact]
    public void Warmup_subtracts_five_minutes()
    {
        Assert.Equal("55 13 * * MON", CronJobOrgTranslator.TryWarmupCron("0 14 * * MON"));
        Assert.Equal("57 13 * * *", CronJobOrgTranslator.TryWarmupCron("2 14 * * *"));
        Assert.Equal("25 9 5 * *", CronJobOrgTranslator.TryWarmupCron("30 9 5 * *"));
    }

    [Fact]
    public void Warmup_null_when_it_would_cross_midnight_or_not_single_time()
    {
        Assert.Null(CronJobOrgTranslator.TryWarmupCron("0 0 * * *"));   // 00:00 → dia anterior
        Assert.Null(CronJobOrgTranslator.TryWarmupCron("3 0 * * *"));   // 00:03 → dia anterior
        Assert.Null(CronJobOrgTranslator.TryWarmupCron("*/15 14 * * *")); // minuto não é único
    }
}
