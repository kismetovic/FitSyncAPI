using FITSync.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        public override int UserId { get; set; }
        public override int RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
