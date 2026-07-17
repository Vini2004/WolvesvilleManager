using WolvesvilleManager.Domain.Entities;

namespace WolvesvilleManager.Application.Polls;

/// <summary>
/// Calcula o estado (aberta/fechada) de uma votação a partir de uma lista de janelas semanais
/// recorrentes (ex.: "domingo 23:00 até segunda 11:00"). Cada janela é independente e pode se
/// sobrepor com as outras; a votação está aberta se "agora" cair em QUALQUER janela configurada.
/// </summary>
public static class PollWindowCalculator
{
    // Cobre janelas de até quase uma semana de duração: gera ocorrências de -3 a +1 semanas ao
    // redor de "agora", suficiente para achar a ocorrência atual/mais recente em qualquer caso.
    private const int LookbackWeeks = 3;

    public static bool IsOpen(IReadOnlyList<PollWindow> windows, string timeZoneId, DateTime utcNow)
    {
        var nowLocal = ToLocal(utcNow, timeZoneId);
        return windows.Any(w => Occurrences(w, nowLocal).Any(o => o.StartLocal <= nowLocal && nowLocal < o.EndLocal));
    }

    /// <summary>Próxima transição (abre ou fecha) a partir de agora, em UTC; null se não houver janelas.</summary>
    public static DateTime? GetNextBoundaryUtc(IReadOnlyList<PollWindow> windows, string timeZoneId, DateTime utcNow)
    {
        if (windows.Count == 0) return null;
        var nowLocal = ToLocal(utcNow, timeZoneId);
        DateTime? best = null;
        foreach (var w in windows)
        {
            foreach (var o in Occurrences(w, nowLocal))
            {
                if (o.StartLocal > nowLocal && (best is null || o.StartLocal < best)) best = o.StartLocal;
                if (o.EndLocal > nowLocal && (best is null || o.EndLocal < best)) best = o.EndLocal;
            }
        }
        return best is null ? null : ToUtc(best.Value, timeZoneId);
    }

    /// <summary>
    /// O último ciclo já concluído (fim ≤ agora) entre todas as janelas configuradas — é este que
    /// a automação "mais votada do formulário" apura. Null se nenhuma janela já terminou (ex.:
    /// votação configurada há poucas horas, antes do primeiro fechamento).
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc)? GetLastCompletedWindowUtc(
        IReadOnlyList<PollWindow> windows, string timeZoneId, DateTime utcNow)
    {
        var nowLocal = ToLocal(utcNow, timeZoneId);
        (DateTime StartLocal, DateTime EndLocal)? best = null;
        foreach (var w in windows)
            foreach (var o in Occurrences(w, nowLocal))
                if (o.EndLocal <= nowLocal && (best is null || o.EndLocal > best.Value.EndLocal))
                    best = o;

        if (best is null) return null;
        return (ToUtc(best.Value.StartLocal, timeZoneId), ToUtc(best.Value.EndLocal, timeZoneId));
    }

    /// <summary>Gera as ocorrências (início/fim locais) de uma janela nas semanas ao redor de <paramref name="aroundLocal"/>.</summary>
    private static IEnumerable<(DateTime StartLocal, DateTime EndLocal)> Occurrences(PollWindow w, DateTime aroundLocal)
    {
        var anchor = aroundLocal.Date;
        while (anchor.DayOfWeek != w.StartDayOfWeek) anchor = anchor.AddDays(-1);

        var durationDays = ((int)w.EndDayOfWeek - (int)w.StartDayOfWeek + 7) % 7;

        for (var i = -LookbackWeeks; i <= 1; i++)
        {
            var start = anchor.AddDays(i * 7) + w.StartTime;
            var end = anchor.AddDays(i * 7 + durationDays) + w.EndTime;
            // Mesmo dia com hora de fim <= hora de início (ou 0 dias de diferença e fim antes do
            // início): a janela só fecha na semana seguinte (ciclo de quase uma semana inteira).
            if (end <= start) end = end.AddDays(7);
            yield return (start, end);
        }
    }

    private static DateTime ToLocal(DateTime utc, string timeZoneId) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

    private static DateTime ToUtc(DateTime local, string timeZoneId) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
}
