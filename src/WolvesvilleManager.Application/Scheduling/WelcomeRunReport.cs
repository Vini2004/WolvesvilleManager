namespace WolvesvilleManager.Application.Scheduling;

/// <summary>Desfecho de uma entrada de log na checagem de boas-vindas.</summary>
public enum WelcomeEntryStatus
{
    /// <summary>Mensagem enviada no chat agora.</summary>
    Sent = 1,

    /// <summary>Reconhecida, mas represada até o horário de envio configurado.</summary>
    Held = 2,

    /// <summary>Reconhecida, mas impossível saudar (ex.: o log não trouxe o nick).</summary>
    Skipped = 3,

    /// <summary>Tentou enviar e falhou — será tentada de novo na próxima checagem.</summary>
    Failed = 4,
}

/// <summary>Uma entrada de log processada, com o que aconteceu com ela.</summary>
public record WelcomeEntryReport(
    string? Action, string? Username, DateTime JoinedAtUtc, WelcomeEntryStatus Status, string Detail);

/// <summary>
/// O que a checagem de boas-vindas de um clã fez. Existe para que "não funcionou" deixe de ser
/// indistinguível de "está represada até as 19:00" e de "o app nunca acordou" — é o retorno do
/// botão "Verificar entradas agora" e a fonte do resumo gravado em
/// <c>ClanRegistration.LastWelcomeCheckResult</c>.
/// </summary>
public class WelcomeRunReport
{
    public string ClanName { get; set; } = string.Empty;

    /// <summary>Id do job de ping no cron-job.org; nulo = nada acorda o app nos horários configurados.</summary>
    public int? PingJobId { get; set; }

    /// <summary>Preenchido quando a checagem inteira falhou (ex.: a API do jogo recusou).</summary>
    public string? Error { get; set; }

    /// <summary>Observação quando não há entradas a processar (ex.: primeira checagem).</summary>
    public string? Note { get; set; }

    public List<WelcomeEntryReport> Entries { get; } = new();

    public int SentCount => Entries.Count(e => e.Status == WelcomeEntryStatus.Sent);
    public int HeldCount => Entries.Count(e => e.Status == WelcomeEntryStatus.Held);

    /// <summary>Resumo de uma linha, para o "Última verificação" da tela.</summary>
    public string Summary()
    {
        if (Error is not null) return $"Falhou: {Error}";
        if (Entries.Count == 0) return Note ?? "Nenhuma entrada nova.";

        var parts = new List<string> { $"{Entries.Count} entrada(s)" };
        if (SentCount > 0) parts.Add($"{SentCount} enviada(s)");
        if (HeldCount > 0) parts.Add($"{HeldCount} aguardando horário");
        var skipped = Entries.Count(e => e.Status == WelcomeEntryStatus.Skipped);
        if (skipped > 0) parts.Add($"{skipped} ignorada(s)");
        var failed = Entries.Count(e => e.Status == WelcomeEntryStatus.Failed);
        if (failed > 0) parts.Add($"{failed} com falha");
        return string.Join(", ", parts);
    }
}

/// <summary>Totais da fase de boas-vindas de uma batida do agendador (todos os clãs).</summary>
public class WelcomeRunSummary
{
    public int Clans { get; set; }
    public int Sent { get; set; }
    public int Held { get; set; }
}

/// <summary>O que uma batida do agendador fez — devolvido por <c>/api/scheduler/run</c>.</summary>
public record SchedulerRunResult(int Executed, int Welcomed, int Held, int CheckedClans);
