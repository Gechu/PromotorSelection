using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromotorSelection.Domain.Entities
{
    public class Promotor
    {
        public int Id { get; set; } 
        public int MaxStudentow { get; set; } 

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Topic> Tematy { get; set; } = new List<Topic>();
        public ICollection<Assignment> Przydzialy { get; set; } = new List<Assignment>();
    }

    public class Topic
    {
        public int Id { get; set; } 
        public string NazwaTematu { get; set; } = string.Empty; 
        public string Opis { get; set; } = string.Empty;

        public int PromotorId { get; set; }
        public Promotor Promotor { get; set; } = null!;
    }
}
