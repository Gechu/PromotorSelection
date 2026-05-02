namespace PromotorSelection.Application.Dto
{
    public class StudentDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AlbumNumber { get; set; } = string.Empty;
        public double GradeAverage { get; set; }
        public int TeamId { get; set; }

    }
}
