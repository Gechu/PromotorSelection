
namespace PromotorSelection.Application.Dto;

public class StatisticsDto
{
    public int TotalTeams { get; set; }
    public int FreelancersCount { get; set; }
    public int IdleStudentsCount { get; set; }
    public IEnumerable<PromotorOccupancyDto> PromotorOccupancy { get; set; }

    public StatisticsDto()
    {
        PromotorOccupancy = new List<PromotorOccupancyDto>();
    }

    public StatisticsDto(int totalTeams, int freelancersCount, int idleStudentsCount, IEnumerable<PromotorOccupancyDto> promotorOccupancy)
    {
        TotalTeams = totalTeams;
        FreelancersCount = freelancersCount;
        IdleStudentsCount = idleStudentsCount;
        PromotorOccupancy = promotorOccupancy;
    }
}