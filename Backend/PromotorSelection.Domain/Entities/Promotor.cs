namespace PromotorSelection.Domain.Entities;

public class Promotor
{
    public int Id { get; set; }
    public int StudentLimit { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

