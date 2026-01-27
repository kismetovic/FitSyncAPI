using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
