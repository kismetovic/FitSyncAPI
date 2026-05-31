using FITSync.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class Role : IdentityRole<int>
    {
        public ICollection<UserRole> Users { get; set; }
    }
}
