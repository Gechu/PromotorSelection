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

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int PromotorId { get; set; }
    public Promotor Promotor { get; set; } = null!;
}