using System.ComponentModel.DataAnnotations;

namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Marca uma missão como oculta no formulário público de votação de um clã: enquanto existir
/// esta linha, a missão correspondente não aparece na página pública nem aceita votos (a aba
/// admin ainda a mostra, marcada, para poder reexibi-la).
///
/// A identidade guardada é a chave estável da missão (<see cref="QuestKey"/>) — não o Id da
/// oferta, que rotaciona — para que a escolha do admin sobreviva quando a mesma missão sai e
/// volta de cartaz. A opção "embaralhar" usa a chave fixa do seu id reservado.
/// </summary>
public class PollHiddenQuest
{
    public int Id { get; set; }

    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;

    /// <summary>
    /// Chave de identidade estável da missão oculta (imagem promocional normalizada via
    /// <c>QuestMatchKey</c>, ou o id reservado de "embaralhar").
    /// </summary>
    [Required]
    [MaxLength(80)]
    public string QuestKey { get; set; } = string.Empty;
}
