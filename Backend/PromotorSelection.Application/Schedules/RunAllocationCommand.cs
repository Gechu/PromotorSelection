using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Schedules;

public record RunAllocationCommand : IRequest<bool>;

public class RunAllocationHandler : IRequestHandler<RunAllocationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public RunAllocationHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(RunAllocationCommand request, CancellationToken ct)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(ct);
        if (schedule == null || DateTime.Now <= schedule.EndDate)
            throw new Exception("Przydział można uruchomić dopiero po zakończeniu terminu wyborów.");

        var oldAssignments = await _context.Assignments.ToListAsync(ct);
        _context.Assignments.RemoveRange(oldAssignments);

        var promotors = await _context.Promotors.ToListAsync(ct);
        var limits = promotors.ToDictionary(p => p.UserId, p => p.StudentLimit);
        var currentCounts = promotors.ToDictionary(p => p.UserId, p => 0);

        var students = await _context.Students
            .Include(s => s.Team)
            .Include(s => s.Preferences)
            .ToListAsync(ct);

        var groups = students
            .GroupBy(s => s.TeamId ?? -s.UserId) 
            .Select(g => new
            {
                TeamId = g.Key > 0 ? (int?)g.Key : null,
                Members = g.ToList(),
                AverageGrade = g.Average(s => s.GradeAverage ?? 0)
            })
            .OrderByDescending(g => g.AverageGrade)
            .ToList();

        foreach (var group in groups)
        {
            var leader = group.Members.First();
            var preferences = leader.Preferences.OrderBy(p => p.Priority).ToList();

            foreach (var pref in preferences)
            {
                int requiredSeats = group.Members.Count;
                int availableSeats = limits[pref.PromotorId] - currentCounts[pref.PromotorId];

                if (availableSeats >= requiredSeats)
                {
                    foreach (var member in group.Members)
                    {
                        _context.Assignments.Add(new Assignment
                        {
                            StudentId = member.UserId, 
                            PromotorId = pref.PromotorId,
                            TeamId = group.TeamId
                        });
                    }
                    currentCounts[pref.PromotorId] += requiredSeats;
                    break;
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}