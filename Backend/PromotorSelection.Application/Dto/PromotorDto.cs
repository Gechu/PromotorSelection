namespace PromotorSelection.Application.Dto;

public class PromotorDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int StudentLimit { get; set; }

    public ICollection<TopicDto> Topics { get; set; } = new List<TopicDto>();
}