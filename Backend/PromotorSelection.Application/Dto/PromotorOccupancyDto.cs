namespace PromotorSelection.Application.Dto;

public class PromotorOccupancyDto
{
    public int PromotorId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int InterestedStudentsCount { get; set; }
    public int StudentLimit { get; set; }

    public PromotorOccupancyDto() { }

    public PromotorOccupancyDto(int promotorId, string firstName, string lastName, int interestedStudentsCount, int studentLimit)
    {
        PromotorId = promotorId;
        FirstName = firstName;
        LastName = lastName;
        InterestedStudentsCount = interestedStudentsCount;
        StudentLimit = studentLimit;
    }
}