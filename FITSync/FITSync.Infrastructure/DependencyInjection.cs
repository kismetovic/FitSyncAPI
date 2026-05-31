using System.Text;
using FITSync.Domain.Models;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Authentication;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.HostedServices;
using FITSync.Infrastructure.Repositories;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Seeding;
using FITSync.Infrastructure.Services;
using FITSync.Infrastructure.Services.ExternalServices;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FITSync.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));
            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            services.Configure<PayPalSettings>(configuration.GetSection(PayPalSettings.SectionName));
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICaller, Caller>();

            var jwtSettings = configuration.GetSection(JwtSettings.SectionName);
            var secretKey = jwtSettings["SecretKey"] ?? "";

            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddHostedService<EmailQueueConsumerHostedService>();

            services.AddHttpClient<IPayPalPaymentService, PaypalPaymentService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITrainingRepository, TrainingRepository>();
            services.AddScoped<ITrainingTypeRepository, TrainingTypeRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAdditionalServiceRepository, AdditionalServiceRepository>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITrainingService, TrainingService>();
            services.AddScoped<ITrainingTypeService, TrainingTypeService>();
            services.AddScoped<IReservationService, FITSync.Infrastructure.Services.ReservationService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAdditionalServiceService, AdditionalServiceService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IUserActionService, UserActionService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            services.AddScoped<DatabaseSeeder>();

            services.AddAutoMapper(typeof(DependencyInjection).Assembly);
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=FitSyncDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            services.AddDbContext<FitSyncDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddIdentity<User, Role>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<FitSyncDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["ValidIssuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSettings["ValidAudience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}
