namespace WolvesvilleManager.Domain.Entities;

/// <summary>
/// Foto diária do XP contribuído por cada membro. A API do Wolvesville só expõe o XP
/// acumulado — para relatórios semanais/mensais precisamos guardar o histórico nós mesmos.
/// </summary>
public class MemberXpSnapshot
{
    public int Id { get; set; }
    public int ClanRegistrationId { get; set; }
    public ClanRegistration ClanRegistration { get; set; } = null!;
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public long Xp { get; set; }
    public DateTime TakenAtUtc { get; set; }
}
