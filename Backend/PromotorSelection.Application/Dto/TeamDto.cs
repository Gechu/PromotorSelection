namespace PromotorSelection.Application.Dto;

public class TeamMemberDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class TeamDto
{
    public TeamDto() { }

    public int Id { get; set; }
    public int TeamSize { get; set; }
    public int LeaderId { get; set; }
    public int CurrentMembersCount { get; set; }
    public bool IsClosed => TeamSize == -1;

    public List<TeamMemberDto> Members { get; set; } = new List<TeamMemberDto>();
}