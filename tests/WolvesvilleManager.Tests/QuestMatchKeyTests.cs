using WolvesvilleManager.Application.Quests;

namespace WolvesvilleManager.Tests;

public class QuestMatchKeyTests
{
    [Theory]
    [InlineData("https://cdn/promo/fallen-angel-123.png", "fallenangel")]
    [InlineData("https://cdn/promo/fallen-angel-222.png", "fallenangel")]
    [InlineData("https://cdn/promo/FALLEN_ANGEL.png", "fallenangel")]
    [InlineData("settlers.jpg", "settlers")]
    public void Normalize_RemoveDigitosSeparadoresECaixa(string url, string expected)
    {
        Assert.Equal(expected, QuestMatchKey.Normalize(url));
    }

    [Fact]
    public void Normalize_MesmaMissaoComSufixoDiferente_ProduzMesmaChave()
    {
        Assert.Equal(
            QuestMatchKey.Normalize("https://cdn/promo/fallen-angel-111.png"),
            QuestMatchKey.Normalize("https://cdn/a/b/fallen-angel-999.png"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://cdn/promo/123-456.png")] // sem letras → sem chave
    public void Normalize_SemConteudoUtil_RetornaNull(string? url)
    {
        Assert.Null(QuestMatchKey.Normalize(url));
    }
}
