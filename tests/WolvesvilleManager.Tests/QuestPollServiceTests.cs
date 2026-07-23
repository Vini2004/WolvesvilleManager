using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Application.Polls;
using WolvesvilleManager.Domain.Entities;
using WolvesvilleManager.Domain.Wolvesville;
using WolvesvilleManager.Infrastructure.Persistence;
using WolvesvilleManager.Tests.Fakes;

namespace WolvesvilleManager.Tests;

public class QuestPollServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static QuestPollService CreateService(AppDbContext db, FakeWolvesvilleClient api) =>
        new(db, api, new FakeApiKeyProtector());

    private static ClanRegistration SeedClan(AppDbContext db)
    {
        var clan = new ClanRegistration
        {
            ClanId = "clan-1",
            ClanName = "Clã de Teste",
            ProtectedApiKey = "chave-teste",
            PollToken = "tok-123",
            PollExpiresAtUtc = DateTime.UtcNow.AddDays(1), // votação aberta
        };
        db.ClanRegistrations.Add(clan);
        db.SaveChanges();
        return clan;
    }

    private static ClanQuest Quest(string id, string promo) => new() { Id = id, PromoImageUrl = promo };

    [Fact]
    public async Task MissaoOcultada_NaoApareceNoFormularioPublico_MasApareceMarcadaNoAdmin()
    {
        using var db = CreateDb();
        var clan = SeedClan(db);
        var api = new FakeWolvesvilleClient
        {
            AvailableQuests =
            [
                Quest("q1", "https://cdn/dragon-1.png"),
                Quest("q2", "https://cdn/fallen-angel-1.png"),
                Quest("q3", "https://cdn/wolf-1.png"),
            ],
        };
        var service = CreateService(db, api);

        await service.SetQuestHiddenAsync(clan.Id, "q2", hidden: true);

        // Público: q2 some; sobram q1, q3 + embaralhar.
        var pub = await service.GetPublicAsync(clan.PollToken!, nickname: null);
        Assert.DoesNotContain(pub.Quests, q => q.QuestId == "q2");
        Assert.Contains(pub.Quests, q => q.QuestId == "q1");
        Assert.Contains(pub.Quests, q => q.QuestId == QuestPollVote.ShuffleOptionId);

        // Admin: q2 continua na lista, marcada como oculta.
        var admin = await service.GetAdminAsync(clan.Id);
        var q2 = Assert.Single(admin.Quests, q => q.QuestId == "q2");
        Assert.True(q2.Hidden);
        Assert.False(Assert.Single(admin.Quests, q => q.QuestId == "q1").Hidden);
    }

    [Fact]
    public async Task OcultacaoSobreviveARotacaoDeId_PelaChaveDaImagem()
    {
        using var db = CreateDb();
        var clan = SeedClan(db);
        var api = new FakeWolvesvilleClient
        {
            AvailableQuests = [Quest("q2", "https://cdn/fallen-angel-1.png")],
        };
        var service = CreateService(db, api);

        await service.SetQuestHiddenAsync(clan.Id, "q2", hidden: true);

        // A oferta rotaciona: novo Id, mesma imagem (só muda o dígito) → mesma chave estável.
        api.AvailableQuests = [Quest("q2-novo", "https://cdn/fallen-angel-7.png")];

        var pub = await service.GetPublicAsync(clan.PollToken!, nickname: null);
        Assert.DoesNotContain(pub.Quests, q => q.QuestId == "q2-novo");
    }

    [Fact]
    public async Task Reexibir_VoltaAApareceNoPublico()
    {
        using var db = CreateDb();
        var clan = SeedClan(db);
        var api = new FakeWolvesvilleClient { AvailableQuests = [Quest("q1", "https://cdn/dragon-1.png")] };
        var service = CreateService(db, api);

        await service.SetQuestHiddenAsync(clan.Id, "q1", hidden: true);
        Assert.DoesNotContain((await service.GetPublicAsync(clan.PollToken!, null)).Quests, q => q.QuestId == "q1");

        await service.SetQuestHiddenAsync(clan.Id, "q1", hidden: false);
        Assert.Contains((await service.GetPublicAsync(clan.PollToken!, null)).Quests, q => q.QuestId == "q1");
    }

    [Fact]
    public async Task Votar_EmMissaoOculta_EhRejeitado()
    {
        using var db = CreateDb();
        var clan = SeedClan(db);
        var api = new FakeWolvesvilleClient { AvailableQuests = [Quest("q2", "https://cdn/fallen-angel-1.png")] };
        var service = CreateService(db, api);

        await service.SetQuestHiddenAsync(clan.Id, "q2", hidden: true);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.VoteAsync(clan.PollToken!, "q2", "Fulano"));
        Assert.Empty(db.QuestPollVotes);
    }

    [Fact]
    public async Task Embaralhar_PodeSerOcultado()
    {
        using var db = CreateDb();
        var clan = SeedClan(db);
        var api = new FakeWolvesvilleClient { AvailableQuests = [Quest("q1", "https://cdn/dragon-1.png")] };
        var service = CreateService(db, api);

        await service.SetQuestHiddenAsync(clan.Id, QuestPollVote.ShuffleOptionId, hidden: true);

        var pub = await service.GetPublicAsync(clan.PollToken!, null);
        Assert.DoesNotContain(pub.Quests, q => q.QuestId == QuestPollVote.ShuffleOptionId);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.VoteAsync(clan.PollToken!, QuestPollVote.ShuffleOptionId, "Fulano"));
    }
}
