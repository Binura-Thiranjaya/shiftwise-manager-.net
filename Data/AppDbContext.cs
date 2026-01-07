using Microsoft.EntityFrameworkCore;
using TandTFuel.Api.Models;

namespace TandTFuel.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<ShiftType> ShiftTypes => Set<ShiftType>();
    public DbSet<EmployeeStation> EmployeeStations => Set<EmployeeStation>();
    public DbSet<EmployeeShift> EmployeeShifts => Set<EmployeeShift>();
    public DbSet<Payslip> Payslips => Set<Payslip>();
    public DbSet<PayslipDetail> PayslipDetails => Set<PayslipDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Station>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<ShiftType>().HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<EmployeeStation>()
            .HasIndex(x => new { x.EmployeeId, x.StationId })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL");

        // decimals
        modelBuilder.Entity<Employee>().Property(x => x.HourlyRateA).HasPrecision(10, 2);
        modelBuilder.Entity<Employee>().Property(x => x.HourlyRateB).HasPrecision(10, 2);
        modelBuilder.Entity<EmployeeShift>().Property(x => x.HourlyRate).HasPrecision(10, 2);

        modelBuilder.Entity<Payslip>().Property(x => x.GrossPay).HasPrecision(10, 2);
        modelBuilder.Entity<Payslip>().Property(x => x.NetPay).HasPrecision(10, 2);
        modelBuilder.Entity<PayslipDetail>().Property(x => x.Amount).HasPrecision(10, 2);

        // restrict deletes
        modelBuilder.Entity<EmployeeShift>()
            .HasOne(x => x.Station)
            .WithMany(x => x.Shifts)
            .HasForeignKey(x => x.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeShift>()
            .HasOne(x => x.ShiftType)
            .WithMany(x => x.Shifts)
            .HasForeignKey(x => x.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            if (entry.State == EntityState.Added) entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
