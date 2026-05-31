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

        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<TrainingType> TrainingTypes => Set<TrainingType>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AdditionalService> AdditionalServices => Set<AdditionalService>();
        public DbSet<ReservationService> ReservationServices => Set<ReservationService>();

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
                Email = "fitsync@gmail.com",
                NormalizedEmail = "FITSYNC@GMAIL.COM",
                Name = "Super",
                Surname = "Admin",
                UserName = "superadministrator",
                NormalizedUserName = "SUPERADMINISTRATOR",
                SecurityStamp = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                ConcurrencyStamp = "b2c3d4e5-f6a7-8901-bcde-f12345678901",
                EmailConfirmed = true,
                PhoneNumber = "062123123",
                Enabled = true,
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
                NormalizedName = RoleDefinition.Administrator.ToUpperInvariant(),
                ConcurrencyStamp = "c3d4e5f6-a7b8-9012-cdef-123456789012"
            },
            new Role()
            {
                Id = 2,
                Name = RoleDefinition.Client,
                NormalizedName = RoleDefinition.Client.ToUpperInvariant(),
                ConcurrencyStamp = "d4e5f6a7-b8c9-0123-def0-234567890123"
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
