using Microsoft.EntityFrameworkCore;
using WolvesvilleManager.Application.Common;
using WolvesvilleManager.Domain.Entities;

namespace WolvesvilleManager.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ClanRegistration> ClanRegistrations => Set<ClanRegistration>();
    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();
    public DbSet<TaskExecutionLog> TaskExecutionLogs => Set<TaskExecutionLog>();
    public DbSet<MemberXpSnapshot> MemberXpSnapshots => Set<MemberXpSnapshot>();
    public DbSet<QuestPollVote> QuestPollVotes => Set<QuestPollVote>();
    public DbSet<QuestPollResult> QuestPollResults => Set<QuestPollResult>();
    public DbSet<PollWindow> PollWindows => Set<PollWindow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestPollVote>(e =>
        {
            // Um voto por nick por clã (comparação case-insensitive feita no serviço); votar de
            // novo com o mesmo nick troca a missão (upsert). Índice é rede de segurança para
            // duplicata exata — a deduplicação por maiúsculas/minúsculas acontece na aplicação.
            e.HasIndex(v => new { v.ClanRegistrationId, v.Nickname }).IsUnique();
            e.HasOne(v => v.ClanRegistration)
                .WithMany()
                .HasForeignKey(v => v.ClanRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestPollResult>(e =>
        {
            e.HasIndex(r => new { r.ClanRegistrationId, r.DecidedAtUtc });
            e.Property(r => r.QuestName).HasMaxLength(200);
            e.HasOne(r => r.ClanRegistration)
                .WithMany()
                .HasForeignKey(r => r.ClanRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClanRegistration>(e =>
        {
            e.HasIndex(c => c.ClanId).IsUnique();
            // Resolução do formulário público: token → clã.
            e.HasIndex(c => c.PollToken).IsUnique();
            e.HasMany(c => c.ScheduledTasks)
                .WithOne(t => t.ClanRegistration)
                .HasForeignKey(t => t.ClanRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.PollWindows)
                .WithOne(w => w.ClanRegistration)
                .HasForeignKey(w => w.ClanRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PollWindow>(e =>
        {
            e.HasIndex(w => w.ClanRegistrationId);
        });

        modelBuilder.Entity<ScheduledTask>(e =>
        {
            // Índice do poll do agendador: busca por tarefas habilitadas e vencidas.
            e.HasIndex(t => new { t.Enabled, t.NextRunAtUtc });
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(50);
            e.HasMany(t => t.ExecutionLogs)
                .WithOne(l => l.ScheduledTask)
                .HasForeignKey(l => l.ScheduledTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskExecutionLog>(e =>
        {
            e.HasIndex(l => new { l.ScheduledTaskId, l.RanAtUtc });
            e.Property(l => l.Outcome).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<MemberXpSnapshot>(e =>
        {
            e.HasIndex(s => new { s.ClanRegistrationId, s.TakenAtUtc });
            e.Property(s => s.PlayerId).HasMaxLength(50);
            e.Property(s => s.Username).HasMaxLength(100);
            e.HasOne(s => s.ClanRegistration)
                .WithMany()
                .HasForeignKey(s => s.ClanRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
