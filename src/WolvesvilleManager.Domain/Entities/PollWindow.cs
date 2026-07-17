namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Uma janela semanal recorrente em que o formulário público de votação fica aberto (ex.:
/// domingo 23h até segunda 11h). Um clã pode ter quantas janelas quiser — a votação está aberta
/// se "agora" cair em QUALQUER uma delas. Substitui o antigo prazo recorrente por cron único
/// (<c>PollCloseCronExpression</c>), que só permitia um horário de fechamento compartilhado por
/// todos os dias marcados, sem suportar múltiplos ciclos com horários de abertura diferentes.
/// </summary>
public class PollWindow
{
    public int Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    public DayOfWeek StartDayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public DayOfWeek EndDayOfWeek { get; set; }
    public TimeSpan EndTime { get; set; }
}
