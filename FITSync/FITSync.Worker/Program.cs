using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Services.ExternalServices;
using FITSync.Infrastructure.Services.Interfaces;
using FITSync.Worker.Consumers;

// FITSync.Worker is a standalone microservice. It shares only the messaging and SMTP
// pieces of the Infrastructure assembly; it deliberately does not call AddInfrastructure,
// so it needs no database, no Identity and no JWT configuration. Its single job is to
// consume the RabbitMQ email queue that the API publishes to.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection(RabbitMQSettings.SectionName));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<EmailQueueConsumer>();

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>()
    .LogInformation("FITSync.Worker starting: RabbitMQ email consumer.");

host.Run();
