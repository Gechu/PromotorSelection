namespace PromotorSelection.Application.Dto;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? AlbumNumber { get; set; }
    public double? GradeAverage { get; set; }
    public int? StudentLimit { get; set; }
}

