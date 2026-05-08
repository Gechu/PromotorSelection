namespace PromotorSelection.Application.Dto;

public class AllocationResultDto
{
    public int StudentId { get; set; }
    public string StudentFirstName { get; set; }
    public string StudentLastName { get; set; }
    public string AlbumNumber { get; set; }
    public double? GradeAverage { get; set; }
    public int PromotorId { get; set; }
    public string PromotorFirstName { get; set; }
    public string PromotorLastName { get; set; }

    public AllocationResultDto() { }

    public AllocationResultDto(int studentId, string sFn, string sLn, string album, double? grade, int promotorId, string pFn, string pLn)
    {
        StudentId = studentId;
        StudentFirstName = sFn;
        StudentLastName = sLn;
        AlbumNumber = album;
        GradeAverage = grade;
        PromotorId = promotorId;
        PromotorFirstName = pFn;
        PromotorLastName = pLn;
    }
}