namespace PromotorSelection.Application.Dto;

public class UserDto
{
    public UserDto() { }
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? AlbumNumber { get; set; }
    public double? GradeAverage { get; set; }
    public int? StudentLimit { get; set; }
    public int? TeamId { get; set; }


    public UserDto(int id, string firstName, string lastName, string email, string? albumNumber, double? gradeAverage, int? studentLimit, int? teamId)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        AlbumNumber = albumNumber;
        GradeAverage = gradeAverage;
        StudentLimit = studentLimit;
        TeamId = teamId;
    }
}

