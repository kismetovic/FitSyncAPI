using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Seeding
{
    public class DatabaseSeeder
    {
        private readonly FitSyncDbContext _context;
        private readonly UserManager<User> _userManager;

        public DatabaseSeeder(FitSyncDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task SeedAsync()
        {
            await SeedExampleUserAsync();
            await SeedTrainingTypesAsync();
            await SeedAdditionalServicesAsync();
            await SeedTrainingsAsync();
        }

        private async Task SeedExampleUserAsync()
        {
            const string email = "user@fitsync.com";
            if (await _userManager.FindByEmailAsync(email) != null)
                return;

            var user = new User
            {
                UserName = "johndoe",
                Email = email,
                Name = "John",
                Surname = "Doe",
                PhoneNumber = "061234567",
                EmailConfirmed = true,
                Enabled = true
            };
            var result = await _userManager.CreateAsync(user, "User123!");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, RoleDefinition.Client);
        }

        private async Task SeedTrainingTypesAsync()
        {
            if (await _context.TrainingTypes.AnyAsync())
                return;

            var types = new[]
            {
                new TrainingType { Name = "Yoga" },
                new TrainingType { Name = "Cardio" },
                new TrainingType { Name = "Strength" },
                new TrainingType { Name = "Pilates" },
                new TrainingType { Name = "CrossFit" },
                new TrainingType { Name = "Spinning" },
                new TrainingType { Name = "Zumba" },
                new TrainingType { Name = "Boxing" },
            };
            await _context.TrainingTypes.AddRangeAsync(types);
            await _context.SaveChangesAsync();
        }

        private async Task SeedAdditionalServicesAsync()
        {
            if (await _context.AdditionalServices.AnyAsync())
                return;

            var services = new[]
            {
                new AdditionalService { Name = "Personal Towel", Price = 2.00m },
                new AdditionalService { Name = "Protein Shake", Price = 5.00m },
                new AdditionalService { Name = "Locker Rental", Price = 3.00m },
                new AdditionalService { Name = "Sports Nutrition Pack", Price = 10.00m },
            };
            await _context.AdditionalServices.AddRangeAsync(services);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTrainingsAsync()
        {
            if (await _context.Trainings.AnyAsync())
                return;

            var yoga = await _context.TrainingTypes.FirstAsync(t => t.Name == "Yoga");
            var cardio = await _context.TrainingTypes.FirstAsync(t => t.Name == "Cardio");
            var strength = await _context.TrainingTypes.FirstAsync(t => t.Name == "Strength");
            var pilates = await _context.TrainingTypes.FirstAsync(t => t.Name == "Pilates");
            var crossfit = await _context.TrainingTypes.FirstAsync(t => t.Name == "CrossFit");
            var spinning = await _context.TrainingTypes.FirstAsync(t => t.Name == "Spinning");
            var zumba = await _context.TrainingTypes.FirstAsync(t => t.Name == "Zumba");
            var boxing = await _context.TrainingTypes.FirstAsync(t => t.Name == "Boxing");

            var trainings = new[]
            {
                new Training
                {
                    Name = "Morning Yoga Flow",
                    Description = "Start your day with a gentle yoga flow that improves flexibility and mindfulness. Suitable for all levels.",
                    Price = 15.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 20,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = yoga.Id
                },
                new Training
                {
                    Name = "Power Yoga",
                    Description = "Dynamic, fitness-based approach to vinyasa-style yoga. Build strength and flexibility simultaneously.",
                    Price = 20.00m,
                    DurationMinutes = 75,
                    MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = yoga.Id
                },
                new Training
                {
                    Name = "HIIT Cardio Blast",
                    Description = "High-intensity interval training that burns maximum calories in minimum time. Not for the faint-hearted!",
                    Price = 18.00m,
                    DurationMinutes = 45,
                    MaxCapacity = 25,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = cardio.Id
                },
                new Training
                {
                    Name = "Beginner Cardio",
                    Description = "Low-impact cardiovascular exercises perfect for those just starting their fitness journey.",
                    Price = 12.00m,
                    DurationMinutes = 50,
                    MaxCapacity = 30,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = cardio.Id
                },
                new Training
                {
                    Name = "Full Body Strength",
                    Description = "Compound exercises targeting all major muscle groups for balanced strength development. Dumbbells and barbells included.",
                    Price = 22.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 12,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = strength.Id
                },
                new Training
                {
                    Name = "Core & Abs Intensive",
                    Description = "Focused core strengthening session for a stronger midsection and better posture in everyday life.",
                    Price = 16.00m,
                    DurationMinutes = 45,
                    MaxCapacity = 20,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = strength.Id
                },
                new Training
                {
                    Name = "Beginner Strength",
                    Description = "Learn the foundational movements of strength training with proper form and technique.",
                    Price = 14.00m,
                    DurationMinutes = 55,
                    MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = strength.Id
                },
                new Training
                {
                    Name = "Pilates Fundamentals",
                    Description = "Learn the foundational Pilates movements to build core strength and body awareness. Perfect for beginners.",
                    Price = 20.00m,
                    DurationMinutes = 55,
                    MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = pilates.Id
                },
                new Training
                {
                    Name = "Advanced Pilates Reform",
                    Description = "Advanced Pilates session using reformer equipment for deeper muscle engagement and flexibility.",
                    Price = 28.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 8,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = pilates.Id
                },
                new Training
                {
                    Name = "CrossFit WOD",
                    Description = "Workout of the Day combining functional movements at high intensity. Be prepared to push your limits!",
                    Price = 25.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 10,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = crossfit.Id
                },
                new Training
                {
                    Name = "CrossFit Intro",
                    Description = "Introduction to CrossFit methodology, movements, and community. Great for fitness newcomers.",
                    Price = 18.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 12,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = crossfit.Id
                },
                new Training
                {
                    Name = "Spinning Endurance",
                    Description = "Indoor cycling session designed to build cardiovascular endurance and leg strength over 45 minutes.",
                    Price = 18.00m,
                    DurationMinutes = 45,
                    MaxCapacity = 20,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = spinning.Id
                },
                new Training
                {
                    Name = "Zumba Party",
                    Description = "Dance your way to fitness! Fun, high-energy Zumba class combining Latin rhythms with easy-to-follow moves.",
                    Price = 14.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 30,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = zumba.Id
                },
                new Training
                {
                    Name = "Boxing Fundamentals",
                    Description = "Learn proper boxing technique including stance, footwork, jabs, and combinations. Great cardio and stress relief.",
                    Price = 22.00m,
                    DurationMinutes = 60,
                    MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = boxing.Id
                },
            };

            await _context.Trainings.AddRangeAsync(trainings);
            await _context.SaveChangesAsync();
        }
    }
}
