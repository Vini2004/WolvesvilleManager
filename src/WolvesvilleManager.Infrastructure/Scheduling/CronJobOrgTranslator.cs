namespace WolvesvilleManager.Infrastructure.Scheduling;

/// <summary>Agenda no formato do cron-job.org (arrays; -1 = "todos").</summary>
public sealed record CronJobOrgSchedule(int[] Minutes, int[] Hours, int[] Mdays, int[] Months, int[] Wdays);

/// <summary>
/// Traduz expressão cron de 5 campos para o formato de agenda do cron-job.org e calcula o
/// horário de pré-aquecimento (~5 min antes). Cobre o que a UI gera (min/hora fixos, listas,
/// intervalos, passos, nomes de dia) — suficiente para os agendamentos reais do painel.
/// </summary>
public static class CronJobOrgTranslator
{
    private static readonly Dictionary<string, int> Dow = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SUN"] = 0, ["MON"] = 1, ["TUE"] = 2, ["WED"] = 3, ["THU"] = 4, ["FRI"] = 5, ["SAT"] = 6,
    };

    public static CronJobOrgSchedule ToSchedule(string cron)
    {
        var f = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (f.Length != 5)
            throw new FormatException("Expressão cron deve ter 5 campos.");
        return new CronJobOrgSchedule(
            ParseField(f[0], 0, 59),
            ParseField(f[1], 0, 23),
            ParseField(f[2], 1, 31),
            ParseField(f[3], 1, 12),
            ParseDow(f[4]));
    }

    /// <summary>
    /// Cron ~<paramref name="leadMinutes"/> min antes, para pré-aquecer o app/banco. Trata minuto
    /// único com hora única OU lista de horas ("0 18,20,22 * * MON-FRI" → "55 17,19,21 …", usado
    /// pelos horários de retentativa do pular-espera); fora disso devolve null e o gatilho de
    /// execução sozinho + o retry de conexão dão conta (tarefa roda ~40s mais tarde).
    /// ponytail: warm-up é otimização, não correção — o edge de meia-noite (00:0X) fica sem pré-aquecer.
    /// </summary>
    public static string? TryWarmupCron(string cron, int leadMinutes = 5)
    {
        var f = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (f.Length != 5) return null;
        if (!int.TryParse(f[0], out var m)) return null;

        var warmHours = new List<int>();
        var warmMinute = 0;
        foreach (var part in f[1].Split(','))
        {
            if (!int.TryParse(part, out var h)) return null;
            var total = h * 60 + m - leadMinutes;
            if (total < 0) return null; // viraria o dia anterior — sem pré-aquecer
            warmMinute = total % 60; // igual para todas as horas (mesmo minuto de origem)
            warmHours.Add(total / 60);
        }
        return $"{warmMinute} {string.Join(",", warmHours.Distinct().OrderBy(h => h))} {f[2]} {f[3]} {f[4]}";
    }

    private static int[] ParseDow(string field)
    {
        var arr = ParseField(field, 0, 7, s => Dow.TryGetValue(s, out var v) ? v : (int?)null);
        if (arr.Length == 1 && arr[0] == -1) return arr;
        // Em cron, 7 = domingo; no cron-job.org domingo é 0.
        return arr.Select(v => v == 7 ? 0 : v).Distinct().OrderBy(v => v).ToArray();
    }

    private static int[] ParseField(string field, int min, int max, Func<string, int?>? name = null)
    {
        if (field == "*" || field == "?") return new[] { -1 };

        var set = new SortedSet<int>();
        foreach (var raw in field.Split(','))
        {
            var token = raw;
            var step = 1;
            var slash = token.IndexOf('/');
            if (slash >= 0)
            {
                step = int.Parse(token[(slash + 1)..]);
                token = token[..slash];
            }

            int lo, hi;
            if (token == "*")
            {
                lo = min; hi = max;
            }
            else if (token.Contains('-'))
            {
                var r = token.Split('-');
                lo = Resolve(r[0], name); hi = Resolve(r[1], name);
            }
            else
            {
                lo = Resolve(token, name);
                hi = step > 1 ? max : lo; // "n/step" = de n até o fim, de step em step
            }

            for (var v = lo; v <= hi; v += step) set.Add(v);
        }
        return set.Count == 0 ? new[] { -1 } : set.ToArray();
    }

    private static int Resolve(string s, Func<string, int?>? name) =>
        name?.Invoke(s) ?? int.Parse(s);
}
