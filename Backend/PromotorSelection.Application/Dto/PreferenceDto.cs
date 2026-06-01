namespace PromotorSelection.Application.Dto;

public class PreferenceDto
{
    public int PromotorId { get; set; }
    public int Priority { get; set; }
    public string PromotorFirstName { get; set; } = string.Empty;
    public string PromotorLastName { get; set; } = string.Empty;
}