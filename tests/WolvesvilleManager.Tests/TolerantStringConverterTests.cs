using System.Text.Json;
using WolvesvilleManager.Domain.Wolvesville;
using WolvesvilleManager.Infrastructure.Wolvesville;

namespace WolvesvilleManager.Tests;

public class TolerantStringConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new TolerantStringConverter());
        return o;
    }

    [Fact]
    public void PlayerProfile_ComStatusNumerico_NaoQuebra()
    {
        // A API do Wolvesville devolve "status" como número; o modelo tem string.
        // Antes o desserializador lançava e a busca virava um 500 opaco.
        const string json = """
            { "id": "p1", "username": "miso", "level": 42, "status": 3, "profileIconColor": 16711680 }
            """;

        var player = JsonSerializer.Deserialize<PlayerProfile>(json, Options);

        Assert.NotNull(player);
        Assert.Equal("miso", player!.Username);
        Assert.Equal(42, player.Level);
        Assert.Equal("3", player.Status);
        Assert.Equal("16711680", player.ProfileIconColor);
    }

    [Fact]
    public void PlayerProfile_ComLevelNuloEStatusNumerico_NaoQuebra()
    {
        // Resposta realista de /players/search: nível oculto (null) e status numérico.
        // Antes, "level": null quebrava a desserialização (int não-anulável) → 500.
        const string json = """
            { "id": "p1", "username": "0Hermione", "level": null, "status": 5,
              "receivedRosesCount": 12, "lastOnline": "2026-07-10T08:39:34.192Z" }
            """;

        var player = JsonSerializer.Deserialize<PlayerProfile>(json, Options);

        Assert.NotNull(player);
        Assert.Null(player!.Level);
        Assert.Equal("5", player.Status);
        Assert.Equal(12, player.ReceivedRosesCount);
    }

    [Fact]
    public void CampoString_ComValoresVariados_Converte()
    {
        Assert.Equal("texto", Roundtrip("\"texto\""));
        Assert.Equal("7", Roundtrip("7"));
        Assert.Equal("true", Roundtrip("true"));
        Assert.Null(Roundtrip("null"));
        Assert.Null(Roundtrip("{ \"x\": 1 }")); // objeto inesperado vira null sem quebrar
    }

    private static string? Roundtrip(string statusJson)
    {
        var player = JsonSerializer.Deserialize<PlayerProfile>(
            $"{{ \"username\": \"u\", \"status\": {statusJson} }}", Options);
        return player!.Status;
    }
}
