using Microsoft.EntityFrameworkCore;
using ProjectManagerWebAPI.Models;

namespace ProjectManagerWebAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<ProjectStatusHistory> ProjectStatusHistories { get; set; }
    public DbSet<ProjectManagerHistory> ProjectManagerHistories { get; set; }
    public DbSet<ProjectOwnerHistory> ProjectOwnerHistories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Setor> Setores { get; set; }
    public DbSet<UserSetor> UserSetores { get; set; }
    public DbSet<Timesheet> Timesheets { get; set; }
    public DbSet<TimesheetEntry> TimesheetEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar campos booleanos para Oracle (converter bool para NUMBER(1) com valores 0/1)
        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<ProjectMember>()
            .Property(pm => pm.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<Setor>()
            .Property(s => s.IsActive)
            .HasColumnType("NUMBER(1)")
            .HasConversion<int>();

        modelBuilder.Entity<ProjectTask>()
            .Property(pt => pt.Progress)
            .HasPrecision(5, 2);

        modelBuilder.Entity<TimesheetEntry>()
            .Property(te => te.WorkHours)
            .HasPrecision(5, 2);

        // Índice único para Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectMembers)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMembers)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectStatusHistory>()
            .HasOne(sh => sh.Project)
            .WithMany(p => p.StatusHistory)
            .HasForeignKey(sh => sh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectManagerHistory>()
            .HasOne(mh => mh.Project)
            .WithMany(p => p.ManagerHistory)
            .HasForeignKey(mh => mh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectOwnerHistory>()
            .HasOne(oh => oh.Project)
            .WithMany(p => p.OwnerHistory)
            .HasForeignKey(oh => oh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Project)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSetores)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasOne(us => us.Setor)
            .WithMany(s => s.UserSetores)
            .HasForeignKey(us => us.SetorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSetor>()
            .HasIndex(us => new { us.UserId, us.SetorId })
            .IsUnique();

        modelBuilder.Entity<Timesheet>()
            .HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Timesheet>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Timesheet>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Timesheet>()
            .HasOne(t => t.ApprovedBy)
            .WithMany()
            .HasForeignKey(t => t.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TimesheetEntry>()
            .HasOne(te => te.Timesheet)
            .WithMany(t => t.Entries)
            .HasForeignKey(te => te.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimesheetEntry>()
            .HasIndex(te => new { te.TimesheetId, te.DayOfWeek })
            .IsUnique();
    }
}
