using System.Text;
using FITSync.Domain.Entities;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Authentication;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Seeding;
using FITSync.Infrastructure.Services;
using FITSync.Infrastructure.Services.ExternalServices;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

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
            var secretKey = jwtSettings["SecretKey"];

            // The secret is supplied through configuration/environment only. Failing loudly
            // here beats issuing tokens signed with an empty key.
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey is missing or shorter than 32 characters. " +
                    "Provide it via the JwtSettings__SecretKey environment variable (see .env.example).");
            }

            // The API only publishes onto RabbitMQ. Consuming and sending happens in the
            // separate FITSync.Worker service; there is no consumer hosted inside the API.
            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

            // Hosts that provide SignalR replace this with a real publisher after calling
            // AddInfrastructure; TryAdd keeps the no-op only as a fallback.
            services.TryAddScoped<INotificationPublisher, NoOpNotificationPublisher>();

            services.AddHttpClient<IPayPalPaymentService, PaypalPaymentService>();

            // --- Repositories ---
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITrainingRepository, TrainingRepository>();
            services.AddScoped<ITrainingTypeRepository, TrainingTypeRepository>();
            services.AddScoped<ITrainerRepository, TrainerRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IReservationStatusHistoryRepository, ReservationStatusHistoryRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IAdditionalServiceRepository, AdditionalServiceRepository>();
            services.AddScoped<IFaqRepository, FaqRepository>();
            services.AddScoped<ISupportContactRepository, SupportContactRepository>();
            services.AddScoped<IMembershipPackageRepository, MembershipPackageRepository>();
            services.AddScoped<IUserMembershipRepository, UserMembershipRepository>();
            services.AddScoped<IUserActionRepository, UserActionRepository>();

            // --- Services ---
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITrainingService, TrainingService>();
            services.AddScoped<ITrainingTypeService, TrainingTypeService>();
            services.AddScoped<ITrainerService, TrainerService>();
            services.AddScoped<IReservationService, Services.ReservationService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAdditionalServiceService, AdditionalServiceService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<ISupportContactService, SupportContactService>();
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IUserActionService, UserActionService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            services.AddScoped<DatabaseSeeder>();

            services.AddAutoMapper(typeof(DependencyInjection).Assembly);

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

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

                    // SignalR cannot set an Authorization header on the WebSocket handshake,
                    // so the token arrives as a query string parameter for hub routes only.
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                                context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }
    }
}
