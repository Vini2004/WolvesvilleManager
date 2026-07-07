using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Domain.Entities;

namespace WolvesvilleManager.Application.Common;

/// <summary>
/// Contrato de persistência consumido pelos casos de uso.
/// A implementação concreta (EF Core + SQL Server) fica na Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<ClanRegistration> ClanRegistrations { get; }
    DbSet<ScheduledTask> ScheduledTasks { get; }
    DbSet<TaskExecutionLog> TaskExecutionLogs { get; }
    DbSet<MemberXpSnapshot> MemberXpSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
