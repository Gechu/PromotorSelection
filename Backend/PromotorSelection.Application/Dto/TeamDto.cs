namespace PromotorSelection.Application.Dto;

public class TeamDto
{

    public int Id { get; set; }
    public int TeamSize { get; set; }
    public int LeaderId { get; set; }
    public int CurrentMembersCount { get; set; }
    public bool IsClosed => TeamSize == -1; 
}