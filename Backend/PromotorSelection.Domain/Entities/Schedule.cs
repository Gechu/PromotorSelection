using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PromotorSelection.Domain.Entities
{
    public class Schedule
    {
        public int Id { get; set; }
        public DateTime DataStart { get; set; }
        public DateTime DataEnd { get; set; } 
    }
}
