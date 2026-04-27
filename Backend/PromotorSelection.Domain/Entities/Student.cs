using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromotorSelection.Domain.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public string NrAlbumu { get; set; } = string.Empty;
        public double SredniaOcen { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? TeamId { get; set; } 
        public Team? Team { get; set; }

        public ICollection<Preference> Wybory { get; set; } = new List<Preference>();
    }

    public class Team
    {
        public int Id { get; set; }
        public int RozmiarZespołu { get; set; }
        public int LiderId { get; set; }

        public ICollection<Student> Studenci { get; set; } = new List<Student>();
        public ICollection<Assignment> Przydzialy { get; set; } = new List<Assignment>();
    }
}
