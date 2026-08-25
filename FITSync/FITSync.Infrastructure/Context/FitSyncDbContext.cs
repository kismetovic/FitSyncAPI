using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        // --- Business tables (14) ---
        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<TrainingType> TrainingTypes => Set<TrainingType>();
        public DbSet<Trainer> Trainers => Set<Trainer>();
        public DbSet<TrainerAvailability> TrainerAvailabilities => Set<TrainerAvailability>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<ReservationService> ReservationServices => Set<ReservationService>();
        public DbSet<ReservationStatusHistory> ReservationStatusHistories => Set<ReservationStatusHistory>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AdditionalService> AdditionalServices => Set<AdditionalService>();
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<SupportContact> SupportContacts { get; set; }
        public DbSet<MembershipPackage> MembershipPackages => Set<MembershipPackage>();
        public DbSet<UserMembership> UserMemberships => Set<UserMembership>();
        public DbSet<UserAction> UserActions => Set<UserAction>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            DefineUserAndRoles(builder);
            DefineMoneyPrecision(builder);
            DefineReservationGraph(builder);
            DefinePaymentGraph(builder);
            DefineReviewGraph(builder);
            DefineTrainerGraph(builder);
            DefineMembershipGraph(builder);
            DefineUserActionGraph(builder);
            DefineHelpContent(builder);
            SeedRoles(builder);
            SeedAdministrator(builder);
        }

        /// <summary>
        /// SQL Server defaults decimal to (18,2) but warns unless it is stated explicitly.
        /// Every monetary column is pinned here so rounding behaviour is identical everywhere.
        /// </summary>
        private static void DefineMoneyPrecision(ModelBuilder builder)
        {
            builder.Entity<Training>().Property(t => t.Price).HasPrecision(18, 2);
            builder.Entity<AdditionalService>().Property(a => a.Price).HasPrecision(18, 2);
            builder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
            builder.Entity<Reservation>().Property(r => r.TotalPrice).HasPrecision(18, 2);
            builder.Entity<Reservation>().Property(r => r.OutsideAvailabilitySurcharge).HasPrecision(18, 2);
            builder.Entity<Trainer>().Property(t => t.OutsideAvailabilitySurcharge).HasPrecision(18, 2);
            builder.Entity<MembershipPackage>().Property(m => m.Price).HasPrecision(18, 2);
            builder.Entity<UserMembership>().Property(m => m.PricePaid).HasPrecision(18, 2);
        }

        private static void DefineReservationGraph(ModelBuilder builder)
        {
            // Reservation reaches User twice (owner, and whoever cancelled it). Neither
            // relationship may cascade, otherwise SQL Server rejects the model with
            // "multiple cascade paths".
            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.CancelledByUser)
                .WithMany()
                .HasForeignKey(r => r.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.Training)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasIndex(r => new { r.TrainingId, r.ReservationDate });
            builder.Entity<Reservation>()
                .HasIndex(r => new { r.UserId, r.ReservationDate });

            builder.Entity<ReservationService>()
                .HasOne(rs => rs.Reservation)
                .WithMany(r => r.ReservationServices)
                .HasForeignKey(rs => rs.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReservationService>()
                .HasOne(rs => rs.AdditionalService)
                .WithMany()
                .HasForeignKey(rs => rs.AdditionalServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReservationStatusHistory>()
                .HasOne(h => h.Reservation)
                .WithMany(r => r.StatusHistory)
                .HasForeignKey(h => h.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReservationStatusHistory>()
                .HasOne(h => h.ChangedByUser)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void DefinePaymentGraph(ModelBuilder builder)
        {
            builder.Entity<Payment>()
                .HasOne(p => p.Reservation)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(p => p.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idempotency: replaying the same PayPal capture callback cannot insert a second row.
            builder.Entity<Payment>()
                .HasIndex(p => p.ProviderOrderId)
                .IsUnique()
                .HasFilter("[ProviderOrderId] IS NOT NULL");

            builder.Entity<Payment>()
                .HasOne(p => p.UserMembership)
                .WithMany()
                .HasForeignKey(p => p.UserMembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            // At most one captured payment per reservation, enforced by the database and not
            // only by the service layer. Status 1 == PaymentStatus.Captured.
            //
            // The "IS NOT NULL" half matters now that a payment can instead belong to a
            // membership: SQL Server treats NULLs as equal in a unique index, so without it
            // the second membership payment ever captured would collide with the first.
            builder.Entity<Payment>()
                .HasIndex(p => p.ReservationId)
                .IsUnique()
                .HasFilter("[Status] = 1 AND [IsDeleted] = 0 AND [ReservationId] IS NOT NULL")
                .HasDatabaseName("IX_Payments_ReservationId_SingleCapture");

            // The same guarantee for packages: a membership cannot be paid for twice.
            builder.Entity<Payment>()
                .HasIndex(p => p.UserMembershipId)
                .IsUnique()
                .HasFilter("[Status] = 1 AND [IsDeleted] = 0 AND [UserMembershipId] IS NOT NULL")
                .HasDatabaseName("IX_Payments_UserMembershipId_SingleCapture");

            // A payment settles exactly one thing. Enforced here so no service, script or
            // manual insert can create a row that belongs to both or to neither.
            builder.Entity<Payment>().ToTable(t => t.HasCheckConstraint(
                "CK_Payments_ExactlyOneSubject",
                "([ReservationId] IS NOT NULL AND [UserMembershipId] IS NULL) OR " +
                "([ReservationId] IS NULL AND [UserMembershipId] IS NOT NULL)"));
        }

        private static void DefineReviewGraph(ModelBuilder builder)
        {
            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Training)
                .WithMany(t => t.Reviews)
                .HasForeignKey(r => r.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.Reservation)
                .WithMany()
                .HasForeignKey(r => r.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            // One review per attended reservation.
            builder.Entity<Review>()
                .HasIndex(r => r.ReservationId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }

        private static void DefineTrainerGraph(ModelBuilder builder)
        {
            builder.Entity<Trainer>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TrainerAvailability>()
                .HasOne(a => a.Trainer)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Training>()
                .HasOne(t => t.Trainer)
                .WithMany(tr => tr.Trainings)
                .HasForeignKey(t => t.TrainerId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private static void DefineMembershipGraph(ModelBuilder builder)
        {
            builder.Entity<MembershipPackage>()
                .HasOne(m => m.TrainingType)
                .WithMany()
                .HasForeignKey(m => m.TrainingTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserMembership>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserMembership>()
                .HasOne(m => m.MembershipPackage)
                .WithMany(p => p.UserMemberships)
                .HasForeignKey(m => m.MembershipPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(r => r.UserMembership)
                .WithMany()
                .HasForeignKey(r => r.UserMembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserMembership>()
                .HasIndex(m => new { m.UserId, m.Status });
        }

        private static void DefineUserActionGraph(ModelBuilder builder)
        {
            builder.Entity<UserAction>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAction>()
                .HasOne(a => a.Training)
                .WithMany()
                .HasForeignKey(a => a.TrainingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAction>()
                .HasOne(a => a.TrainingType)
                .WithMany()
                .HasForeignKey(a => a.TrainingTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAction>()
                .HasIndex(a => new { a.UserId, a.ActionType });
        }

        private void SeedAdministrator(ModelBuilder builder)
        {
            builder.Entity<User>().HasData(new User()
            {
                Id = 1,
                Email = "fitsync@gmail.com",
                NormalizedEmail = "FITSYNC@GMAIL.COM",
                Name = "Glavni",
                Surname = "Administrator",
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

        /// <summary>
        /// Help content. Lengths match the validation on the request contracts, so an
        /// input the API accepts always fits the column.
        /// </summary>
        private static void DefineHelpContent(ModelBuilder builder)
        {
            builder.Entity<Faq>(faq =>
            {
                faq.Property(f => f.Question).IsRequired().HasMaxLength(300);
                faq.Property(f => f.Answer).IsRequired().HasMaxLength(2000);
                // The mobile screen reads in this order, so let SQL Server do the sorting.
                faq.HasIndex(f => new { f.IsActive, f.SortOrder })
                   .HasDatabaseName("IX_Faqs_IsActive_SortOrder");
            });

            builder.Entity<SupportContact>(contact =>
            {
                contact.Property(c => c.Email).IsRequired().HasMaxLength(200);
                contact.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(40);
                contact.Property(c => c.WorkingHours).IsRequired().HasMaxLength(120);
                contact.Property(c => c.Address).HasMaxLength(200);
            });
        }
    }
}
