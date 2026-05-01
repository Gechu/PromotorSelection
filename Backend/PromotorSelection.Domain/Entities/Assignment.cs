using PromotorSelection.Domain.Entities;

public class Assignment
{
    public int Id { get; set; } 

    public int? StudentId { get; set; }
    public Student? Student { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public int PromotorId { get; set; }
    public Promotor Promotor { get; set; } = null!;
}