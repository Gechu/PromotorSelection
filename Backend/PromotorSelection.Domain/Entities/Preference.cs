namespace PromotorSelection.Domain.Entities;

public class Preference
{
    public int Id { get; set; }
    public int Priority { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int PromotorId { get; set; }
    public Promotor Promotor { get; set; } = null!;
}