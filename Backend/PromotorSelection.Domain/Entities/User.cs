namespace PromotorSelection.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Student? Student { get; set; }
    public Promotor? Promotor { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public ICollection<User> Users { get; set; } = new List<User>();
}