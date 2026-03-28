using Microsoft.EntityFrameworkCore;
using SessionManager.Application.Interfaces.Persistence;
using SessionManager.Domain.Entities;

namespace SessionManager.Infrastructure.Data;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AllowedProcess> AllowedProcesses => Set<AllowedProcess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Server>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Hostname).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Hostname).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorUsername).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ServerName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TargetUsername).HasMaxLength(120);
            entity.Property(x => x.ProcessName).HasMaxLength(80);
            entity.Property(x => x.ClientIpAddress).HasMaxLength(80);
            entity.Property(x => x.ErrorMessage).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson).HasMaxLength(2000);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(260).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<AllowedProcess>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProcessName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.ProcessName).IsUnique();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Domain.Common.BaseEntity auditableEntity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                if (auditableEntity.CreatedAtUtc == default)
                {
                    auditableEntity.CreatedAtUtc = utcNow;
                }

                auditableEntity.UpdatedAtUtc = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditableEntity.UpdatedAtUtc = utcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
