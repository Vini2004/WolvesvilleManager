using System.Text;

namespace WolvesvilleManager.Application.Quests;

/// <summary>
/// Deriva uma chave de identidade estável para uma missão a partir da URL da imagem
/// promocional. O Id da oferta e o nome do arquivo variam entre rotações (o próprio nome
/// costuma trazer dígitos que mudam), então normalizamos para apenas as letras do nome-base
/// — o mesmo critério usado pelo dicionário de nomes do front (<c>[-_\d]</c> removidos).
/// Ex.: "https://.../fallen-angel-123.png" → "fallenangel".
/// </summary>
public static class QuestMatchKey
{
    public static string? Normalize(string? promoImageUrl)
    {
        if (string.IsNullOrWhiteSpace(promoImageUrl)) return null;

        var file = promoImageUrl.Split('/').Last();
        var dot = file.LastIndexOf('.');
        if (dot > 0) file = file[..dot];

        var sb = new StringBuilder(file.Length);
        foreach (var c in file.ToLowerInvariant())
        {
            if (c is >= 'a' and <= 'z') sb.Append(c);
        }
        return sb.Length == 0 ? null : sb.ToString();
    }
}
