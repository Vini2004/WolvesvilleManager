namespace WolvesvilleManager.Domain.Entities;

/// <summary>Tipos de ação que o agendador sabe executar contra a API do Wolvesville.</summary>
public enum ScheduledTaskType
{
    /// <summary>
    /// Apura os votos dos membros nas missões disponíveis e inicia a mais votada.
    /// Só executa se não houver missão ativa. Gasta ouro/gemas do clã.
    /// </summary>
    ClaimMostVotedQuest = 1,

    /// <summary>
    /// Pula o tempo de espera da missão ativa (fase antes do início). Gasta gemas do clã.
    /// </summary>
    SkipQuestWaitingTime = 2,

    /// <summary>
    /// Resgata o tempo extra da missão ativa (uma vez por missão). Gasta ouro do clã.
    /// </summary>
    ClaimQuestExtraTime = 3,
}
