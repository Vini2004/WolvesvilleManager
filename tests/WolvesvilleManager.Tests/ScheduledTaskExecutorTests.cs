using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WolvesvilleManager.Application.Scheduling;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Wolvesville;
using WolvesvilleManager.Infrastructure.Persistence;
using WolvesvilleManager.Tests.Fakes;

namespace WolvesvilleManager.Tests;

public class ScheduledTaskExecutorTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ScheduledTask SeedDueTask(AppDbContext db, ScheduledTaskType type, int minVotes = 1)
    {
        var clan = new ClanRegistration
        {
            ClanId = "clan-1",
            ClanName = "Clã de Teste",
            ProtectedApiKey = "chave-teste",
        };
        var task = new ScheduledTask
        {
            ClanRegistration = clan,
            Type = type,
            CronExpression = "*/5 * * * *",
            TimeZoneId = "UTC",
            Enabled = true,
            MinVotes = minVotes,
            NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1), // vencida
        };
        db.ScheduledTasks.Add(task);
        db.SaveChanges();
        return task;
    }

    private static ScheduledTaskExecutor CreateExecutor(AppDbContext db, FakeWolvesvilleClient api) =>
        new(db, api, new FakeApiKeyProtector(), NullLogger<ScheduledTaskExecutor>.Instance);

    private static ClanQuest Quest(string id) => new() { Id = id, PromoImageUrl = $"https://cdn/{id}.png" };

    private static JsonElement VotesJson(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task ClaimMostVotedQuest_IniciaAMissaoMaisVotada()
    {
        using var db = CreateDb();
        var task = SeedDueTask(db, ScheduledTaskType.ClaimMostVotedQuest, minVotes: 2);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = null,
            AvailableQuests = [Quest("quest-a"), Quest("quest-b")],
            Votes = VotesJson("""{ "quest-a": 2, "quest-b": 5 }"""),
        };

        var executed = await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Equal(1, executed);
        Assert.Equal("quest-b", api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Success, log.Outcome);
    }

    [Fact]
    public async Task ClaimMostVotedQuest_ComMissaoAtiva_PulaSemGastar()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.ClaimMostVotedQuest);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest { Quest = Quest("quest-x") },
            AvailableQuests = [Quest("quest-a")],
            Votes = VotesJson("""{ "quest-a": 10 }"""),
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Null(api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task ClaimMostVotedQuest_VotosAbaixoDoMinimo_PulaSemGastar()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.ClaimMostVotedQuest, minVotes: 5);
        var api = new FakeWolvesvilleClient
        {
            AvailableQuests = [Quest("quest-a")],
            Votes = VotesJson("""{ "quest-a": 3 }"""),
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Null(api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task ClaimSpecificQuest_MissaoDisponivel_Inicia()
    {
        using var db = CreateDb();
        var task = SeedDueTask(db, ScheduledTaskType.ClaimSpecificQuest);
        task.TargetQuestId = "quest-b";
        task.TargetQuestName = "quest b";
        db.SaveChanges();
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = null,
            AvailableQuests = [Quest("quest-a"), Quest("quest-b")],
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Equal("quest-b", api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Success, log.Outcome);
    }

    [Fact]
    public async Task ClaimSpecificQuest_NaoDisponivel_Pula()
    {
        using var db = CreateDb();
        var task = SeedDueTask(db, ScheduledTaskType.ClaimSpecificQuest);
        task.TargetQuestId = "quest-x";
        task.TargetQuestName = "quest x";
        db.SaveChanges();
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = null,
            AvailableQuests = [Quest("quest-a"), Quest("quest-b")],
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Null(api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task ClaimSpecificQuest_JaExisteMissaoAtiva_Pula()
    {
        using var db = CreateDb();
        var task = SeedDueTask(db, ScheduledTaskType.ClaimSpecificQuest);
        task.TargetQuestId = "quest-a";
        db.SaveChanges();
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest { Quest = Quest("quest-z") },
            AvailableQuests = [Quest("quest-a")],
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.Null(api.ClaimedQuestId);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task SkipWaitingTime_SoPulaQuandoMissaoEstaNaEspera()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.SkipQuestWaitingTime);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest
            {
                Quest = Quest("quest-a"),
                // Início no futuro = fase de espera.
                TierStartTime = DateTimeOffset.UtcNow.AddHours(2).ToString("O"),
            },
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.True(api.SkippedWaitingTime);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Success, log.Outcome);
    }

    [Fact]
    public async Task SkipWaitingTime_AindaAcumulandoXp_NaoPula()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.SkipQuestWaitingTime);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest
            {
                Quest = Quest("quest-a"),
                // Tier em andamento e objetivo ainda não batido → não há espera a pular.
                TierStartTime = DateTimeOffset.UtcNow.AddHours(-1).ToString("O"),
                Xp = 100,
                XpPerReward = 9500,
            },
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.False(api.SkippedWaitingTime);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task SkipWaitingTime_ObjetivoConcluidoAguardandoCronometro_Pula()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.SkipQuestWaitingTime);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest
            {
                Quest = Quest("quest-a"),
                // Objetivo do tier já batido, mas ainda faltam horas no cronômetro —
                // é exatamente essa espera que o líder pode pular.
                TierStartTime = DateTimeOffset.UtcNow.AddHours(-16).ToString("O"),
                TierEndTime = DateTimeOffset.UtcNow.AddHours(8).ToString("O"),
                Xp = 19000,
                XpPerReward = 9500,
                TierFinished = true,
            },
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.True(api.SkippedWaitingTime);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Success, log.Outcome);
    }

    [Fact]
    public async Task ClaimExtraTime_JaResgatado_Pula()
    {
        using var db = CreateDb();
        SeedDueTask(db, ScheduledTaskType.ClaimQuestExtraTime);
        var api = new FakeWolvesvilleClient
        {
            ActiveQuest = new ActiveQuest
            {
                Quest = Quest("quest-a"),
                ClaimedTime = true,
                TierStartTime = DateTimeOffset.UtcNow.AddHours(-1).ToString("O"),
            },
        };

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.False(api.ClaimedExtraTime);
        var log = Assert.Single(db.TaskExecutionLogs);
        Assert.Equal(TaskExecutionOutcome.Skipped, log.Outcome);
    }

    [Fact]
    public async Task ExecuteDueTasks_ReagendaProximaExecucaoEMarcaUltima()
    {
        using var db = CreateDb();
        var task = SeedDueTask(db, ScheduledTaskType.SkipQuestWaitingTime);
        var api = new FakeWolvesvilleClient(); // sem missão ativa → Skipped

        await CreateExecutor(db, api).ExecuteDueTasksAsync();

        Assert.NotNull(task.LastRunAtUtc);
        Assert.NotNull(task.NextRunAtUtc);
        Assert.True(task.NextRunAtUtc > DateTime.UtcNow, "próxima execução deve estar no futuro");
    }

    [Fact]
    public async Task ExecuteDueTasks_TarefaDesabilitadaOuNaoVencida_NaoExecuta()
    {
        using var db = CreateDb();
        var clan = new ClanRegistration { ClanId = "c", ClanName = "C", ProtectedApiKey = "k" };
        db.ScheduledTasks.Add(new ScheduledTask
        {
            ClanRegistration = clan,
            Type = ScheduledTaskType.SkipQuestWaitingTime,
            CronExpression = "*/5 * * * *",
            TimeZoneId = "UTC",
            Enabled = false,
            NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1),
        });
        db.ScheduledTasks.Add(new ScheduledTask
        {
            ClanRegistration = clan,
            Type = ScheduledTaskType.SkipQuestWaitingTime,
            CronExpression = "*/5 * * * *",
            TimeZoneId = "UTC",
            Enabled = true,
            NextRunAtUtc = DateTime.UtcNow.AddMinutes(10), // ainda não venceu
        });
        db.SaveChanges();

        var executed = await CreateExecutor(db, new FakeWolvesvilleClient()).ExecuteDueTasksAsync();

        Assert.Equal(0, executed);
        Assert.Empty(db.TaskExecutionLogs);
    }
}
