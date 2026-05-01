namespace PromotorSelection.Domain.Entities;

public class Student
{
    public int Id { get; set; }
    public string AlbumNumber { get; set; } = string.Empty;
    public double? GradeAverage { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public ICollection<Preference> Preferences { get; set; } = new List<Preference>();
}

public class Team
{
    public int Id { get; set; }
    public int TeamSize { get; set; }
    public int LeaderId { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}