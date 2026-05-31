using FITSync.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Models
{
    public class User : IdentityUser<int>
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public override string UserName { get => base.UserName; set => base.UserName = value; }
        public override string Email { get => base.Email; set => base.Email = value; }
        public override bool EmailConfirmed { get => base.EmailConfirmed; set => base.EmailConfirmed = value; }
        public override string PhoneNumber { get => base.PhoneNumber; set => base.PhoneNumber = value; }
        public bool Enabled { get; set; }
        public ICollection<UserRole>? Roles { get; set; }
    }
}
