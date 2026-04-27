using Microsoft.EntityFrameworkCore;
using PromotorSelection.Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PromotorSelection.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Promotor> Promotorzy => Set<Promotor>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Preference> Preferences => Set<Preference>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Schedule> Schedules => Set<Schedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>().HasOne(s => s.User).WithOne(u => u.Student).HasForeignKey<Student>(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Promotor>().HasOne(p => p.User).WithOne(u => u.Promotor).HasForeignKey<Promotor>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Student>().HasOne(s => s.Team).WithMany(t => t.Studenci).HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Topic>().HasOne(t => t.Promotor).WithMany(p => p.Tematy).HasForeignKey(t => t.PromotorId);
        modelBuilder.Entity<Preference>().HasOne(p => p.Student).WithMany(s => s.Wybory).HasForeignKey(p => p.StudentId);
        modelBuilder.Entity<Assignment>().HasOne(a => a.Promotor).WithMany(p => p.Przydzialy).HasForeignKey(a => a.PromotorId);
    }
}