using Microsoft.EntityFrameworkCore;
using QLNS.FullNet.Data.Entities;

namespace QLNS.FullNet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Salary> Salaries { get; set; }
    public DbSet<Timekeeping> Timekeepings { get; set; }
    public DbSet<ProfileUpdateRequest> ProfileUpdateRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().HasKey(e => e.Id);
        modelBuilder.Entity<AuditLog>().HasKey(e => e.Id);
        modelBuilder.Entity<Department>().HasKey(e => e.Id);
        modelBuilder.Entity<Employee>().HasKey(e => e.Id);
        modelBuilder.Entity<Holiday>().HasKey(e => e.Id);
        modelBuilder.Entity<LeaveRequest>().HasKey(e => e.Id);
        modelBuilder.Entity<Position>().HasKey(e => e.Id);
        modelBuilder.Entity<Salary>().HasKey(e => e.Id);
        modelBuilder.Entity<Salary>().Property(e => e.BaseSalary).HasPrecision(18, 2);
        modelBuilder.Entity<Salary>().Property(e => e.Allowance).HasPrecision(18, 2);
        modelBuilder.Entity<Salary>().Property(e => e.Deduction).HasPrecision(18, 2);
        modelBuilder.Entity<Salary>().Property(e => e.TotalSalary).HasPrecision(18, 2);

        modelBuilder.Entity<Employee>().Property(e => e.BaseSalary).HasPrecision(18, 2);
        modelBuilder.Entity<Employee>().Property(e => e.Allowance).HasPrecision(18, 2);

        modelBuilder.Entity<Timekeeping>().HasKey(e => e.Id);
        modelBuilder.Entity<ProfileUpdateRequest>().HasKey(e => e.Id);
    }
}
