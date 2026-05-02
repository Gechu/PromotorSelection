namespace PromotorSelection.Application.Dto;

public class TopicDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PromotorId { get; set; }

    public TopicDto(int id, string title, string description, int promotorId)
    {
        Id = id;
        Title = title;
        Description = description;
        PromotorId = promotorId;
    }
}