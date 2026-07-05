using System.Text.Json;
using WolvesvilleManager.Application.Quests;

namespace WolvesvilleManager.Tests;

public class QuestVoteCounterTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void CountVotes_ObjetoComArrays_ContaTamanhoDosArrays()
    {
        var votes = Parse("""{ "quest-a": ["p1", "p2", "p3"], "quest-b": ["p4"] }""");

        var result = QuestVoteCounter.CountVotes(votes);

        Assert.Equal(3, result["quest-a"]);
        Assert.Equal(1, result["quest-b"]);
    }

    [Fact]
    public void CountVotes_ObjetoComNumeros_UsaOsNumeros()
    {
        var votes = Parse("""{ "quest-a": 5, "quest-b": 0 }""");

        var result = QuestVoteCounter.CountVotes(votes);

        Assert.Equal(5, result["quest-a"]);
        Assert.Equal(0, result["quest-b"]);
    }

    [Fact]
    public void CountVotes_ListaDeVotos_AgrupaPorQuestId()
    {
        var votes = Parse("""
            [
              { "questId": "quest-a", "playerId": "p1" },
              { "questId": "quest-a", "playerId": "p2" },
              { "questId": "quest-b", "playerId": "p3" }
            ]
            """);

        var result = QuestVoteCounter.CountVotes(votes);

        Assert.Equal(2, result["quest-a"]);
        Assert.Equal(1, result["quest-b"]);
    }

    [Fact]
    public void CountVotes_FormatoInesperado_RetornaVazioSemLancar()
    {
        Assert.Empty(QuestVoteCounter.CountVotes(Parse("\"texto\"")));
        Assert.Empty(QuestVoteCounter.CountVotes(Parse("42")));
        Assert.Empty(QuestVoteCounter.CountVotes(Parse("null")));
        Assert.Empty(QuestVoteCounter.CountVotes(Parse("[1, 2, 3]")));
    }

    [Fact]
    public void CountVotes_ObjetoComValorNaoContavel_ZeraAContagem()
    {
        var votes = Parse("""{ "quest-a": "estranho" }""");

        var result = QuestVoteCounter.CountVotes(votes);

        Assert.Equal(0, result["quest-a"]);
    }
}
