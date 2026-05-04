using Microsoft.EntityFrameworkCore;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Student> Students { get; }
    DbSet<Promotor> Promotors { get; }
    DbSet<Team> Teams { get; }
    DbSet<Topic> Topics { get; }
    DbSet<Preference> Preferences { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<Schedule> Schedules { get; }
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}