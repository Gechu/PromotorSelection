using PromotorSelection.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public int TeamSize { get; set; }
    public int LeaderId { get; set; }
    public ICollection<Student> Members { get; set; } = new List<Student>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}