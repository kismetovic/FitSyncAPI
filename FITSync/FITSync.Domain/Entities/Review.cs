using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class Review : BaseEntity
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int TrainingId { get; set; }
        public virtual Training Training { get; set; } = null!;
    }
}
