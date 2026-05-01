namespace PromotorSelection.Application.Dto
{
    public class StudentDto
    {
        public string AlbumNumber { get; set; } = string.Empty;
        public double GradeAverage { get; set; }
        public int UserId { get; set; }

        public int TeamId { get; set; }
    }
}
