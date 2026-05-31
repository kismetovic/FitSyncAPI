using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class TrainingType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Training> Trainings { get; set; } = new List<Training>();
    }
}
