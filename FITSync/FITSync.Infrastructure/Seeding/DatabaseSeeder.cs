using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Seeding
{
    /// <summary>
    /// Fills an empty database with a working gym: the catalogue, and enough real
    /// activity that every screen has something to show the moment it opens.
    ///
    /// Everything the user reads is in Bosnian, because that is the language both
    /// apps start in. Every step is guarded by an existence check, so running it
    /// again on a database that already has data changes nothing.
    ///
    /// The seeded activity obeys the same rules the services enforce: a reservation
    /// only reaches Paid or Completed if a captured payment exists for it, a package
    /// is only Active once it has been paid for, a review only exists for a
    /// reservation that was actually attended, and no client holds two packages whose
    /// coverage overlaps.
    /// </summary>
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
            await SeedTrainersAsync();
            await SeedTrainerAvailabilityAsync();
            await SeedTrainingsAsync();
            await SeedMembershipPackagesAsync();
            await SeedFaqsAsync();
            await SeedSupportContactAsync();

            // Depends on everything above, so it runs last.
            await SeedActivityAsync();
        }

        // ------------------------------------------------------------------
        // Accounts
        // ------------------------------------------------------------------

        /// <summary>
        /// The demo client. The administrator is seeded through the model itself
        /// (FitSyncDbContext.SeedAdministrator) because Identity needs a fixed id.
        /// </summary>
        private async Task SeedExampleUserAsync()
        {
            const string email = "user@fitsync.com";
            if (await _userManager.FindByEmailAsync(email) != null)
                return;

            var user = new User
            {
                UserName = "amar",
                Email = email,
                Name = "Amar",
                Surname = "Selimović",
                PhoneNumber = "061234567",
                EmailConfirmed = true,
                Enabled = true
            };
            var result = await _userManager.CreateAsync(user, "User123!");
            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, RoleDefinition.Client);
        }

        // ------------------------------------------------------------------
        // Catalogue
        // ------------------------------------------------------------------

        private async Task SeedTrainingTypesAsync()
        {
            if (await _context.TrainingTypes.AnyAsync())
                return;

            var types = new[]
            {
                new TrainingType { Name = "Joga" },
                new TrainingType { Name = "Kardio" },
                new TrainingType { Name = "Snaga" },
                new TrainingType { Name = "Pilates" },
                new TrainingType { Name = "CrossFit" },
                new TrainingType { Name = "Spinning" },
                new TrainingType { Name = "Zumba" },
                new TrainingType { Name = "Boks" },
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
                new AdditionalService { Name = "Peškir", Price = 2.00m },
                new AdditionalService { Name = "Protein shake", Price = 5.00m },
                new AdditionalService { Name = "Ormarić", Price = 3.00m },
                new AdditionalService { Name = "Sportska prehrana", Price = 10.00m },
                new AdditionalService { Name = "Sauna", Price = 8.50m },
            };
            await _context.AdditionalServices.AddRangeAsync(services);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTrainersAsync()
        {
            if (await _context.Trainers.AnyAsync())
                return;

            var trainers = new[]
            {
                new Trainer
                {
                    FirstName = "Amina",
                    LastName = "Hodžić",
                    Specialty = "Joga i pilates",
                    Biography = "Certificirana instruktorica joge sa deset godina iskustva u radu sa svim uzrastima.",
                    Email = "amina.hodzic@fitsync.ba",
                    PhoneNumber = "061111222",
                    OutsideAvailabilitySurcharge = 12.00m
                },
                new Trainer
                {
                    FirstName = "Emir",
                    LastName = "Begić",
                    Specialty = "Snaga i CrossFit",
                    Biography = "Bivši takmičar u dizanju tegova, specijalizovan za funkcionalni trening.",
                    Email = "emir.begic@fitsync.ba",
                    PhoneNumber = "061333444",
                    OutsideAvailabilitySurcharge = 15.00m
                },
                new Trainer
                {
                    FirstName = "Lejla",
                    LastName = "Kovač",
                    Specialty = "Kardio i grupni programi",
                    Biography = "Vodi zumbu i spinning grupe, fokus na izdržljivosti i zabavi u treningu.",
                    Email = "lejla.kovac@fitsync.ba",
                    PhoneNumber = "061555666",
                    OutsideAvailabilitySurcharge = 10.00m
                },
            };
            await _context.Trainers.AddRangeAsync(trainers);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Weekday working hours. Anything booked outside these windows is an
        /// out-of-availability request: it needs approval and carries a surcharge.
        /// </summary>
        private async Task SeedTrainerAvailabilityAsync()
        {
            if (await _context.TrainerAvailabilities.AnyAsync())
                return;

            var trainers = await _context.Trainers.OrderBy(t => t.Id).ToListAsync();
            if (trainers.Count == 0) return;

            var weekdays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };

            var slots = new List<TrainerAvailability>();
            foreach (var (trainer, index) in trainers.Select((t, i) => (t, i)))
            {
                // Staggered shifts so the three trainers cover different parts of the day.
                var start = TimeSpan.FromHours(7 + index * 2);
                var end = start + TimeSpan.FromHours(9);

                foreach (var day in weekdays)
                {
                    slots.Add(new TrainerAvailability
                    {
                        TrainerId = trainer.Id,
                        DayOfWeek = day,
                        StartTime = start,
                        EndTime = end
                    });
                }

                slots.Add(new TrainerAvailability
                {
                    TrainerId = trainer.Id,
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = TimeSpan.FromHours(9),
                    EndTime = TimeSpan.FromHours(14)
                });
            }

            await _context.TrainerAvailabilities.AddRangeAsync(slots);
            await _context.SaveChangesAsync();
        }

        private async Task SeedTrainingsAsync()
        {
            if (await _context.Trainings.AnyAsync())
                return;

            var types = await _context.TrainingTypes.ToDictionaryAsync(t => t.Name, t => t.Id);
            var trainers = await _context.Trainers.OrderBy(t => t.Id).ToListAsync();

            int? TrainerFor(int index) => trainers.Count == 0 ? null : trainers[index % trainers.Count].Id;

            var trainings = new[]
            {
                new Training
                {
                    Name = "Jutarnja joga",
                    Description = "Blaga jutarnja sekvenca koja budi tijelo, poboljšava fleksibilnost i smiruje misli. Pogodno za sve nivoe.",
                    Price = 15.00m, DurationMinutes = 60, MaxCapacity = 20,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Joga"], TrainerId = TrainerFor(0)
                },
                new Training
                {
                    Name = "Snažna joga",
                    Description = "Dinamična vinyasa joga koja istovremeno gradi snagu i fleksibilnost. Traži nešto kondicije.",
                    Price = 20.00m, DurationMinutes = 75, MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = types["Joga"], TrainerId = TrainerFor(0)
                },
                new Training
                {
                    Name = "Kardio eksplozija",
                    Description = "Intervalni trening visokog intenziteta koji troši maksimum kalorija u minimumu vremena.",
                    Price = 18.00m, DurationMinutes = 45, MaxCapacity = 25,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = types["Kardio"], TrainerId = TrainerFor(2)
                },
                new Training
                {
                    Name = "Kardio za početnike",
                    Description = "Kardio vježbe niskog opterećenja, idealne za one koji tek počinju sa treningom.",
                    Price = 12.00m, DurationMinutes = 50, MaxCapacity = 30,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Kardio"], TrainerId = TrainerFor(2)
                },
                new Training
                {
                    Name = "Snaga cijelog tijela",
                    Description = "Kompleksne vježbe za sve veće mišićne grupe, uz šipke i bučice. Ravnomjeran razvoj snage.",
                    Price = 22.00m, DurationMinutes = 60, MaxCapacity = 12,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = types["Snaga"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "Trbušnjaci i core",
                    Description = "Fokusirani trening trupa za jači centar tijela i bolje držanje u svakodnevnom životu.",
                    Price = 16.00m, DurationMinutes = 45, MaxCapacity = 20,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = types["Snaga"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "Snaga za početnike",
                    Description = "Osnovni obrasci kretanja i tehnika sa lakšim opterećenjem, uz stalni nadzor trenera.",
                    Price = 14.00m, DurationMinutes = 55, MaxCapacity = 15,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Snaga"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "Osnove pilatesa",
                    Description = "Kontrolisani pokreti i disanje kao uvod u pilates. Bez sprava, na strunjači.",
                    Price = 17.00m, DurationMinutes = 55, MaxCapacity = 18,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Pilates"], TrainerId = TrainerFor(0)
                },
                new Training
                {
                    Name = "Napredni pilates na spravama",
                    Description = "Pilates na reformeru za dublje angažovanje mišića i veći raspon pokreta.",
                    Price = 28.00m, DurationMinutes = 60, MaxCapacity = 8,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = types["Pilates"], TrainerId = TrainerFor(0)
                },
                new Training
                {
                    Name = "CrossFit trening dana",
                    Description = "Dnevni WOD koji kombinuje dizanje, gimnastiku i kondiciju. Svaki termin je drugačiji.",
                    Price = 25.00m, DurationMinutes = 60, MaxCapacity = 14,
                    Difficulty = TrainingDifficulty.Advanced,
                    TrainingTypeId = types["CrossFit"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "CrossFit uvod",
                    Description = "Tehnika osnovnih CrossFit pokreta u sporijem tempu, prije ulaska u redovne WOD grupe.",
                    Price = 18.00m, DurationMinutes = 50, MaxCapacity = 12,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["CrossFit"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "Spinning izdržljivost",
                    Description = "Vožnja na sobnom biciklu uz muziku i vođene intervale. Odličan trening srca.",
                    Price = 16.00m, DurationMinutes = 45, MaxCapacity = 22,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = types["Spinning"], TrainerId = TrainerFor(2)
                },
                new Training
                {
                    Name = "Zumba žurka",
                    Description = "Ples i kardio uz latino ritmove. Zabavan trening u kojem se ne broje ponavljanja.",
                    Price = 13.00m, DurationMinutes = 55, MaxCapacity = 30,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Zumba"], TrainerId = TrainerFor(2)
                },
                new Training
                {
                    Name = "Osnove boksa",
                    Description = "Stav, kretanje i osnovni udarci na vreći i fokuserima. Bez kontakta u ringu.",
                    Price = 19.00m, DurationMinutes = 60, MaxCapacity = 16,
                    Difficulty = TrainingDifficulty.Beginner,
                    TrainingTypeId = types["Boks"], TrainerId = TrainerFor(1)
                },
                new Training
                {
                    Name = "Individualni trening",
                    Description = "Termin jedan na jedan sa trenerom, plan prilagođen vašem cilju i trenutnoj formi.",
                    Price = 30.00m, DurationMinutes = 60, MaxCapacity = 1,
                    Difficulty = TrainingDifficulty.Intermediate,
                    TrainingTypeId = types["Snaga"], TrainerId = TrainerFor(1)
                },
            };
            await _context.Trainings.AddRangeAsync(trainings);
            await _context.SaveChangesAsync();
        }

        private async Task SeedMembershipPackagesAsync()
        {
            if (await _context.MembershipPackages.AnyAsync())
                return;

            var types = await _context.TrainingTypes.ToDictionaryAsync(t => t.Name, t => t.Id);

            var packages = new[]
            {
                new MembershipPackage
                {
                    Name = "Mjesečni Basic",
                    Description = "8 termina bilo kojeg treninga u toku 30 dana.",
                    DurationDays = 30, SessionCount = 8, Price = 99.00m, IsActive = true
                },
                new MembershipPackage
                {
                    Name = "Mjesečni Standard",
                    Description = "12 termina bilo kojeg treninga u toku 30 dana.",
                    DurationDays = 30, SessionCount = 12, Price = 139.00m, IsActive = true
                },
                new MembershipPackage
                {
                    Name = "Mjesečni Joga Neograničeno",
                    Description = "20 termina joge u toku 30 dana, za redovne polaznike.",
                    DurationDays = 30, SessionCount = 20, Price = 179.00m,
                    TrainingTypeId = types["Joga"], IsActive = true
                },
                new MembershipPackage
                {
                    Name = "Kvartalni Snaga",
                    Description = "30 termina snage u toku 90 dana, za ozbiljniji ciklus treninga.",
                    DurationDays = 90, SessionCount = 30, Price = 429.00m,
                    TrainingTypeId = types["Snaga"], IsActive = true
                },
            };
            await _context.MembershipPackages.AddRangeAsync(packages);
            await _context.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Help content
        // ------------------------------------------------------------------

        /// <summary>
        /// The help content the mobile app shows. It used to be a hardcoded English
        /// array inside the Flutter widget; it is data now, so an administrator can
        /// change it without a new build.
        /// </summary>
        private async Task SeedFaqsAsync()
        {
            if (await _context.Faqs.AnyAsync())
                return;

            var faqs = new List<Faq>
            {
                new Faq
                {
                    SortOrder = 1,
                    Question = "Kako da rezervišem trening?",
                    Answer = "Na početnoj stranici odaberite trening, otvorite njegov detalj i pritisnite " +
                             "\"Rezerviši odmah\". Zatim birate datum, vrijeme i vrstu rezervacije " +
                             "(jednokratna ili iz mjesečnog paketa)."
                },
                new Faq
                {
                    SortOrder = 2,
                    Question = "Mogu li otkazati rezervaciju?",
                    Answer = "Možete. U kartici \"Moje rezervacije\" otvorite rezervaciju i pritisnite " +
                             "otkazivanje, uz obavezan razlog. Rezervacije se otkazuju, nikada se ne brišu, " +
                             "pa ostaje trag šta se i zašto desilo."
                },
                new Faq
                {
                    SortOrder = 3,
                    Question = "Koji načini plaćanja postoje?",
                    Answer = "PayPal i gotovina na recepciji. Kod PayPal-a plaćanje potvrđuje server nakon " +
                             "provjere kod PayPal-a — vi ništa ne potvrđujete sami. Gotovinsku uplatu " +
                             "evidentira osoblje kada je naplati."
                },
                new Faq
                {
                    SortOrder = 4,
                    Question = "U kojoj valuti su cijene?",
                    Answer = "Sve cijene su u konvertibilnim markama (BAM). PayPal ne podržava BAM, pa se " +
                             "narudžba kreira u eurima po fiksnom kursu (1 EUR = 1.95583 KM); iznos koji " +
                             "vam se naplaćuje prikazan je prije nego što otvorite PayPal."
                },
                new Faq
                {
                    SortOrder = 5,
                    Question = "Kako funkcioniše mjesečni paket?",
                    Answer = "Paket sadrži određen broj termina i vrijedi određen broj dana. Kada " +
                             "rezervišete trening kao mjesečni, troši se jedan termin iz paketa i sam " +
                             "trening se ne naplaćuje — plaćaju se samo dodatne usluge ako ih odaberete."
                },
                new Faq
                {
                    SortOrder = 6,
                    Question = "Zašto ne mogu kupiti drugi paket?",
                    Answer = "Dok imate paket koji pokriva iste treninge, novi se ne može kupiti. " +
                             "Iskoristite postojeći, sačekajte da istekne ili ga otkažite. Paketi vezani " +
                             "za različite tipove treninga mogu se držati istovremeno."
                },
                new Faq
                {
                    SortOrder = 7,
                    Question = "Mogu li otkazati mjesečni paket?",
                    Answer = "Da, dok paket još nije korišten. U kartici \"Mjesečni paketi\" odaberite paket " +
                             "i pritisnite otkazivanje. Paket iz kojeg je već potrošen termin ne može se " +
                             "otkazati."
                },
                new Faq
                {
                    SortOrder = 8,
                    Question = "Kada mogu ostaviti recenziju?",
                    Answer = "Nakon što trening stvarno odradite. Recenzija se veže za vašu rezervaciju, " +
                             "pa je moguća samo za završen termin i samo jednom."
                },
                new Faq
                {
                    SortOrder = 9,
                    Question = "Šta znači \"van rasporeda\"?",
                    Answer = "Termin izvan radnog vremena trenera. Takav zahtjev ide treneru na odobrenje i " +
                             "može nositi doplatu. Dok ga trener ne odobri, rezervacija stoji u statusu " +
                             "\"Čeka odobrenje\"."
                },
                new Faq
                {
                    SortOrder = 10,
                    Question = "Kako da promijenim lozinku?",
                    Answer = "U kartici \"Profil\" odaberite \"Promjena lozinke\". Unosite trenutnu lozinku, " +
                             "pa novu."
                }
            };

            await _context.Faqs.AddRangeAsync(faqs);
            await _context.SaveChangesAsync();
        }

        /// <summary>The gym's own support details, shown on the mobile help screen.</summary>
        private async Task SeedSupportContactAsync()
        {
            if (await _context.SupportContacts.AnyAsync())
                return;

            await _context.SupportContacts.AddAsync(new SupportContact
            {
                Email = "podrska@fitsync.ba",
                PhoneNumber = "+387 33 555 120",
                WorkingHours = "Pon – Pet, 08:00 – 20:00 · Sub, 09:00 – 14:00",
                Address = "Zmaja od Bosne 8, 71000 Sarajevo"
            });
            await _context.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Worked examples
        // ------------------------------------------------------------------

        /// <summary>
        /// A believable history for the demo client so no screen opens empty: bookings
        /// in every status, a package that was paid for and partly spent, the payments
        /// behind them, reviews for what was actually attended, and notifications.
        ///
        /// Dates are relative to the moment of seeding, so the data never goes stale.
        /// </summary>
        private async Task SeedActivityAsync()
        {
            if (await _context.Reservations.AnyAsync())
                return;

            var client = await _userManager.FindByEmailAsync("user@fitsync.com");
            var admin = await _userManager.FindByEmailAsync("fitsync@gmail.com");
            if (client == null) return;

            var adminId = admin?.Id ?? 1;
            var today = DateTime.UtcNow.Date;

            var trainings = await _context.Trainings.OrderBy(t => t.Id).ToListAsync();
            var services = await _context.AdditionalServices.OrderBy(s => s.Id).ToListAsync();
            var packages = await _context.MembershipPackages.OrderBy(p => p.Id).ToListAsync();
            if (trainings.Count == 0 || packages.Count == 0) return;

            Training ByName(string name) => trainings.First(t => t.Name == name);

            // --- the package: bought, paid for, two sessions already spent ---------
            var standard = packages.First(p => p.Name == "Mjesečni Standard");
            var membership = new UserMembership
            {
                UserId = client.Id,
                MembershipPackageId = standard.Id,
                StartDate = today.AddDays(-8),
                EndDate = today.AddDays(-8).AddDays(standard.DurationDays),
                SessionsTotal = standard.SessionCount,
                SessionsUsed = 2,
                Status = MembershipStatus.Active,
                PricePaid = standard.Price,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };
            await _context.UserMemberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            // --- reservations, one per status --------------------------------------
            var joga = ByName("Jutarnja joga");
            var kardio = ByName("Kardio eksplozija");
            var pilates = ByName("Osnove pilatesa");
            var snaga = ByName("Snaga cijelog tijela");
            var spinning = ByName("Spinning izdržljivost");
            var zumba = ByName("Zumba žurka");
            var boks = ByName("Osnove boksa");

            var completed = new Reservation
            {
                UserId = client.Id, TrainingId = joga.Id,
                ReservationDate = today.AddDays(-12).AddHours(9),
                Status = ReservationStatus.Completed,
                ReservationType = ReservationType.OneTime,
                TotalPrice = joga.Price + services[0].Price,   // trening + peškir
                CompletedAt = today.AddDays(-12).AddHours(10),
                CreatedAt = DateTime.UtcNow.AddDays(-14)
            };

            var paidPast = new Reservation
            {
                UserId = client.Id, TrainingId = kardio.Id,
                ReservationDate = today.AddDays(-6).AddHours(18),
                Status = ReservationStatus.Paid,
                ReservationType = ReservationType.OneTime,
                TotalPrice = kardio.Price,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };

            var approved = new Reservation
            {
                UserId = client.Id, TrainingId = snaga.Id,
                ReservationDate = today.AddDays(3).AddHours(17),
                Status = ReservationStatus.Approved,
                ReservationType = ReservationType.OneTime,
                TotalPrice = snaga.Price,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var initial = new Reservation
            {
                UserId = client.Id, TrainingId = spinning.Id,
                ReservationDate = today.AddDays(6).AddHours(19),
                Status = ReservationStatus.Initial,
                ReservationType = ReservationType.OneTime,
                TotalPrice = spinning.Price,
                CreatedAt = DateTime.UtcNow
            };

            // Booked outside the trainer's hours, so it waits for approval and carries
            // the surcharge - review item 19 visible without touching anything.
            var pendingApproval = new Reservation
            {
                UserId = client.Id, TrainingId = boks.Id,
                ReservationDate = today.AddDays(8).AddHours(21),
                Status = ReservationStatus.PendingApproval,
                ReservationType = ReservationType.OneTime,
                IsOutsideTrainerAvailability = true,
                OutsideAvailabilitySurcharge = 15.00m,
                TotalPrice = boks.Price + 15.00m,
                CreatedAt = DateTime.UtcNow
            };

            var cancelled = new Reservation
            {
                UserId = client.Id, TrainingId = zumba.Id,
                ReservationDate = today.AddDays(4).AddHours(18),
                Status = ReservationStatus.Cancelled,
                ReservationType = ReservationType.OneTime,
                TotalPrice = zumba.Price,
                CancelledAt = DateTime.UtcNow.AddDays(-1),
                CancelledByUserId = client.Id,
                CancellationReason = "Spriječen sam zbog posla, javit ću se za drugi termin.",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };

            // The two sessions already drawn from the package.
            var monthlyPast = new Reservation
            {
                UserId = client.Id, TrainingId = pilates.Id,
                ReservationDate = today.AddDays(-4).AddHours(8),
                Status = ReservationStatus.Completed,
                ReservationType = ReservationType.Monthly,
                UserMembershipId = membership.Id,
                TotalPrice = 0m,
                CompletedAt = today.AddDays(-4).AddHours(9),
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            };

            var monthlyUpcoming = new Reservation
            {
                UserId = client.Id, TrainingId = joga.Id,
                ReservationDate = today.AddDays(2).AddHours(9),
                Status = ReservationStatus.Initial,
                ReservationType = ReservationType.Monthly,
                UserMembershipId = membership.Id,
                TotalPrice = 0m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var reservations = new[]
            {
                completed, paidPast, approved, initial, pendingApproval,
                cancelled, monthlyPast, monthlyUpcoming
            };
            await _context.Reservations.AddRangeAsync(reservations);
            await _context.SaveChangesAsync();

            // --- additional services on the completed booking ----------------------
            if (services.Count > 0)
            {
                await _context.ReservationServices.AddAsync(new ReservationService
                {
                    ReservationId = completed.Id,
                    AdditionalServiceId = services[0].Id
                });
            }

            // --- payments: one per thing that is actually paid ----------------------
            // A reservation only reaches Paid or Completed with a captured payment
            // behind it, and an Active package only exists once it has been paid for.
            var payments = new[]
            {
                new Payment
                {
                    ReservationId = completed.Id,
                    Amount = completed.TotalPrice,
                    Currency = "BAM",
                    PaymentProvider = PaymentProvider.Cash,
                    Status = PaymentStatus.Captured,
                    CapturedAt = DateTime.UtcNow.AddDays(-12),
                    ConfirmedByUserId = adminId,
                    TransactionId = $"CASH-{completed.Id}-{today.AddDays(-12):yyyyMMddHHmmss}",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Payment
                {
                    ReservationId = paidPast.Id,
                    Amount = paidPast.TotalPrice,
                    Currency = "BAM",
                    PaymentProvider = PaymentProvider.PayPal,
                    Status = PaymentStatus.Captured,
                    CapturedAt = DateTime.UtcNow.AddDays(-6),
                    ProviderOrderId = "SEED-ORDER-KARDIO-0001",
                    TransactionId = "SEED-CAPTURE-KARDIO-0001",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new Payment
                {
                    UserMembershipId = membership.Id,
                    Amount = membership.PricePaid,
                    Currency = "BAM",
                    PaymentProvider = PaymentProvider.Cash,
                    Status = PaymentStatus.Captured,
                    CapturedAt = DateTime.UtcNow.AddDays(-8),
                    ConfirmedByUserId = adminId,
                    TransactionId = $"CASH-PKG-{membership.Id}-{today.AddDays(-8):yyyyMMddHHmmss}",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
            };
            await _context.Payments.AddRangeAsync(payments);

            // --- status history, so the audit trail is not empty --------------------
            var history = new[]
            {
                new ReservationStatusHistory
                {
                    ReservationId = completed.Id, FromStatus = ReservationStatus.Initial,
                    ToStatus = ReservationStatus.Approved, ChangedByUserId = adminId,
                    ChangedAt = DateTime.UtcNow.AddDays(-13), Reason = "Odobreno od strane osoblja"
                },
                new ReservationStatusHistory
                {
                    ReservationId = completed.Id, FromStatus = ReservationStatus.Approved,
                    ToStatus = ReservationStatus.Paid, ChangedByUserId = adminId,
                    ChangedAt = DateTime.UtcNow.AddDays(-12), Reason = "Gotovinska uplata potvrđena na recepciji"
                },
                new ReservationStatusHistory
                {
                    ReservationId = completed.Id, FromStatus = ReservationStatus.Paid,
                    ToStatus = ReservationStatus.Completed, ChangedByUserId = adminId,
                    ChangedAt = DateTime.UtcNow.AddDays(-12), Reason = "Termin odrađen"
                },
                new ReservationStatusHistory
                {
                    ReservationId = cancelled.Id, FromStatus = ReservationStatus.Initial,
                    ToStatus = ReservationStatus.Cancelled, ChangedByUserId = client.Id,
                    ChangedAt = DateTime.UtcNow.AddDays(-1),
                    Reason = "Spriječen sam zbog posla, javit ću se za drugi termin."
                },
            };
            await _context.ReservationStatusHistories.AddRangeAsync(history);

            // --- reviews, only for sessions actually attended -----------------------
            var reviews = new[]
            {
                new Review
                {
                    UserId = client.Id, TrainingId = joga.Id, ReservationId = completed.Id,
                    Rating = 5,
                    Comment = "Odličan početak dana. Amina objašnjava svaki položaj i ne žuri se.",
                    CreatedAt = DateTime.UtcNow.AddDays(-11)
                },
                new Review
                {
                    UserId = client.Id, TrainingId = pilates.Id, ReservationId = monthlyPast.Id,
                    Rating = 4,
                    Comment = "Solidan trening, taman za jutro. Grupa je malo veća nego što sam očekivao.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
            };
            await _context.Reviews.AddRangeAsync(reviews);

            // --- notifications, so the bell is not empty ----------------------------
            var notifications = new[]
            {
                new Notification
                {
                    UserId = client.Id, IsRead = true,
                    Title = "Uplata evidentirana",
                    Message = $"Vaša uplata za \"{joga.Name}\" je evidentirana. Rezervacija je potvrđena.",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Notification
                {
                    UserId = client.Id, IsRead = true,
                    Title = "Paket plaćen i aktiviran",
                    Message = $"Paket \"{standard.Name}\" je plaćen i aktiviran: {standard.SessionCount} termina.",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Notification
                {
                    UserId = client.Id, IsRead = false,
                    Title = "Rezervacija odobrena",
                    Message = $"Vaša rezervacija za \"{snaga.Name}\" je odobrena. Uplatu možete izvršiti u aplikaciji.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Notification
                {
                    UserId = client.Id, IsRead = false,
                    Title = "Zahtjev zaprimljen",
                    Message = $"Vaš zahtjev za \"{boks.Name}\" van rasporeda je zaprimljen i čeka odobrenje trenera.",
                    CreatedAt = DateTime.UtcNow
                },
            };
            await _context.Notifications.AddRangeAsync(notifications);

            await _context.SaveChangesAsync();
        }
    }
}
