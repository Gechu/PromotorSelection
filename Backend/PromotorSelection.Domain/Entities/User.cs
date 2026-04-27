using System.Data;

namespace PromotorSelection.Domain.Entities
{
    public class User
    {
        public int Id { get; set; } 
        public string Imie { get; set; } = string.Empty;
        public string Nazwisko { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HasloHash { get; set; } = string.Empty;

        public int TypKontaId { get; set; }
        public Role Role { get; set; } = null!;

        public Student? Student { get; set; }
        public Promotor? Promotor { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public string Nazwa { get; set; } = string.Empty; 
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
