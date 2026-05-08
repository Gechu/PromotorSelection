namespace PromotorSelection.Application.Dto;

public class AdminReportDto
{
    public List<StudentReportItem> AllAssignments { get; set; } = new();
    public List<PromotorSummaryDto> PromotorSummaries { get; set; } = new();
}

public class StudentReportItem
{
    public string StudentName { get; set; } = string.Empty;
    public string AlbumNumber { get; set; } = string.Empty;
    public double Grade { get; set; }
    public string AssignedPromotor { get; set; } = string.Empty;
}

public class PromotorSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int AssignedCount { get; set; }
    public int RemainingSlots { get; set; }
}