using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Context
{
    public class FitSyncDbContext : IdentityDbContext<User, Role, int, IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        public FitSyncDbContext()
        {

        }

        public FitSyncDbContext(DbContextOptions<FitSyncDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DefineUserAndRoles(builder);
            SeedRoles(builder);
            SeedAdministrator(builder);
        }

        private void SeedAdministrator(ModelBuilder builder)
        {
            builder.Entity<User>().HasData(new User()
            {
                Id = 1,
                Email = "tokenise@gmail.com",
                NormalizedEmail = "TOKENISE@GMAIL.COM",
                Name = "SUPER",
                Surname = "ADMIN",
                UserName = "superadministrator",
                NormalizedUserName = "SUPERADMINISTRATOR",
                SecurityStamp = String.Empty,
                EmailConfirmed = true,
                PhoneNumber = "062123123",
                PasswordHash = new PasswordHasher<User>().HashPassword(null!, "Admin123!")
            });

            builder.Entity<UserRole>().HasData(new UserRole()
            {
                RoleId = 1,
                UserId = 1
            });
        }
        private void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<Role>().HasData(
            new Role()
            {
                Id = 1,
                Name = RoleDefinition.Administrator,
                NormalizedName = RoleDefinition.Administrator
            },
            new Role()
            {
                Id = 2,
                Name = RoleDefinition.Client,
                NormalizedName = RoleDefinition.Client
            });
        }
        private void DefineUserAndRoles(ModelBuilder builder)
        {
            builder.Entity<User>().
                   HasMany(u => u.Roles).
                   WithOne().
                   HasForeignKey(u => u.UserId);

            builder.Entity<Role>().HasMany(r => r.Users)
               .WithOne(ur => ur.Role)
               .HasForeignKey(ur => ur.RoleId);

            builder.Entity<UserRole>().HasKey(ur => new
            {
                ur.UserId,
                ur.RoleId
            });
        }
    }
}
