using Microsoft.EntityFrameworkCore;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Promotor> Promotors => Set<Promotor>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Preference> Preferences => Set<Preference>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Schedule> Schedules => Set<Schedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity => {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_user");
            entity.Property(e => e.FirstName).HasColumnName("imie");
            entity.Property(e => e.LastName).HasColumnName("nazwisko");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("haslo_hash");
            entity.Property(e => e.RoleId).HasColumnName("id_typ");
        });

        modelBuilder.Entity<Role>(entity => {
            entity.ToTable("typy_kont");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_typ");
            entity.Property(e => e.Name).HasColumnName("typ_konta");
        });

        modelBuilder.Entity<Student>(entity => {
            entity.ToTable("studenci");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("id_student");
            entity.Ignore(e => e.Id); 

            entity.Property(e => e.AlbumNumber).HasColumnName("nr_albumu");
            entity.Property(e => e.GradeAverage).HasColumnName("srednia_ocen").HasColumnType("REAL");
            entity.Property(e => e.TeamId).HasColumnName("id_zespolu");

            entity.HasOne(s => s.User)
                  .WithOne(u => u.Student)
                  .HasForeignKey<Student>(s => s.UserId);
        });

        modelBuilder.Entity<Promotor>(entity => {
            entity.ToTable("promotorzy");
            entity.HasKey(e => e.UserId); 
            entity.Property(e => e.UserId).HasColumnName("id_promotor");
            entity.Property(e => e.StudentLimit).HasColumnName("max_studentow");
            entity.Ignore(e => e.Id);
            entity.HasOne(p => p.User)
                  .WithOne(u => u.Promotor)
                  .HasForeignKey<Promotor>(p => p.UserId);
        });

        modelBuilder.Entity<Team>(entity => {
            entity.ToTable("zespoly");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_zespolu");
            entity.Property(e => e.TeamSize).HasColumnName("rozmiar_zespolu");
            entity.Property(e => e.LeaderId).HasColumnName("id_lidera");
        });

        modelBuilder.Entity<Topic>(entity => {
            entity.ToTable("tematy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_tematu");
            entity.Property(e => e.Title).HasColumnName("temat");
            entity.Property(e => e.Description).HasColumnName("opis");
            entity.Property(e => e.PromotorId).HasColumnName("id_promotor");
        });

        modelBuilder.Entity<Preference>(entity => {
            entity.ToTable("wybory");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_wyboru");
            entity.Property(e => e.StudentId).HasColumnName("id_student");
            entity.Property(e => e.PromotorId).HasColumnName("id_promotor");
            entity.Property(e => e.Priority).HasColumnName("priorytet");
        });

        modelBuilder.Entity<Assignment>(entity => {
            entity.ToTable("przydzialy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_przydzial");
            entity.Property(e => e.StudentId).HasColumnName("id_student");
            entity.Property(e => e.PromotorId).HasColumnName("id_promotor");
            entity.Property(e => e.TeamId).HasColumnName("id_zespolu");
        });

        modelBuilder.Entity<Schedule>(entity => {
            entity.ToTable("harmonogram");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StartDate).HasColumnName("data_start");
            entity.Property(e => e.EndDate).HasColumnName("data_end");
        });
    }
}