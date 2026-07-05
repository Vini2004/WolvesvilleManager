using System.Text.Json;

namespace WolvesvilleManager.Application.Quests;

/// <summary>
/// Converte a resposta de votos do endpoint /quests/votes (formato não documentado)
/// em contagem por missão. Aceita tanto um objeto {questId: [votos...]} ou
/// {questId: número} quanto uma lista [{questId: ...}] — tratamento defensivo.
/// </summary>
public static class QuestVoteCounter
{
    public static Dictionary<string, int> CountVotes(JsonElement votes)
    {
        var result = new Dictionary<string, int>();
        switch (votes.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in votes.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Array => prop.Value.GetArrayLength(),
                        JsonValueKind.Number => prop.Value.GetInt32(),
                        _ => 0
                    };
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in votes.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("questId", out var qid) &&
                        qid.GetString() is string questId)
                    {
                        result[questId] = result.GetValueOrDefault(questId) + 1;
                    }
                }
                break;
        }
        return result;
    }
}
