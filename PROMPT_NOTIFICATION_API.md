# Prompt: สร้าง notification-api

---

## บทบาท

คุณคือ Senior .NET Backend Architect

สร้าง **notification-api** microservice ด้วย **C# .NET 9 / ASP.NET Core** โดยใช้โครงสร้างเดียวกับ `core-auth-api` ทุกประการ

---

## โครงสร้าง Solution ที่ต้องสร้าง

โครงสร้างไฟล์ทั้งหมด (สร้างให้ครบทุกไฟล์):

```
DodeeEdge.Core.Notification.sln
README.md
src/
    API/
        API.csproj
        Program.cs
        appsettings.json
        appsettings.Development.json
        Controllers/
            NotificationController.cs
            TemplateController.cs
            LogController.cs
        Extensions/
            ServiceCollectionExtensions.cs
        Properties/
            launchSettings.json
    Domain/
        Domain.csproj
        AggregatesModel/
            EntityAggregate/
                NotificationLog.cs
                NotificationTemplate.cs
            NotificationAggregate/
                NotificationModels.cs
        Services/
            NotificationService.cs
            Interface/
                INotificationService.cs
                IEmailSender.cs
                ISmsSender.cs
                ITemplateRenderer.cs
    Infrastructure/
        Infrastructure.csproj
        Configurations/
            EnvironmentOptions.cs
        DbContexts/
            NotificationDbContext.cs
            EntityTypeConfigurations/
                NotificationLogEntityTypeConfiguration.cs
                NotificationTemplateEntityTypeConfiguration.cs
        Migrations/
        Repositories/
            NotificationLogRepository.cs
            NotificationTemplateRepository.cs
        Email/
            SendGridEmailSender.cs
            SmtpEmailSender.cs
        Sms/
            TwilioSmsSender.cs
        Rendering/
            HandlebarsTemplateRenderer.cs
        Extensions/
            HttpClientFactoryExtension.cs
        Services/
            NotificationCleanupService.cs
```

---

## .csproj Files

### src/API/API.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <NoWarn>$(NoWarn);8600;8601;8602;8603;8604;8614;8618;8619;8620;8625;0414</NoWarn>
    <AssemblyName>DodeeEdge.Core.Notification.API</AssemblyName>
    <RootNamespace>DodeeEdge.Core.Notification.API</RootNamespace>
    <PackageId>DodeeEdge.Core.Notification.API</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>Monkora Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.2" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.10" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### src/Domain/Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <NoWarn>$(NoWarn);8600;8601;8602;8603;8604;8614;8618;8619;8620;8625;0414</NoWarn>
    <AssemblyName>DodeeEdge.Core.Notification.Domain</AssemblyName>
    <RootNamespace>DodeeEdge.Core.Notification.Domain</RootNamespace>
    <PackageId>DodeeEdge.Core.Notification.Domain</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>Monkora Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="HandlebarsDotNet" Version="2.1.6" />
  </ItemGroup>
</Project>
```

### src/Infrastructure/Infrastructure.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <NoWarn>$(NoWarn);8600;8601;8602;8603;8604;8614;8618;8619;8620;8625;0414</NoWarn>
    <AssemblyName>DodeeEdge.Core.Notification.Infrastructure</AssemblyName>
    <RootNamespace>DodeeEdge.Core.Notification.Infrastructure</RootNamespace>
    <PackageId>DodeeEdge.Core.Notification.Infrastructure</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>Monkora Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="SendGrid" Version="9.29.3" />
    <PackageReference Include="Twilio" Version="7.5.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include="Migrations\" />
  </ItemGroup>
</Project>
```

---

## Namespace Convention

ทุกไฟล์ใช้ namespace pattern นี้:

- API layer: `DodeeEdge.Core.Notification.API.*`
- Domain layer: `DodeeEdge.Core.Notification.Domain.*`
- Infrastructure layer: `DodeeEdge.Core.Notification.Infrastructure.*`

---

## Domain Layer

### src/Domain/AggregatesModel/EntityAggregate/NotificationLog.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;

public class NotificationLog : BaseEntity
{
    public string? TemplateCode { get; set; }
    public string Channel { get; set; }       // EMAIL | SMS
    public string Recipient { get; set; }     // stored masked
    public string? Purpose { get; set; }      // LOGIN | REGISTER | VERIFY | PASSWORD_RESET
    public string? Provider { get; set; }     // SENDGRID | SMTP | TWILIO
    public string? ProviderId { get; set; }   // provider message ID
    public string Status { get; set; } = "QUEUED";  // QUEUED | SENT | FAILED
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/AggregatesModel/EntityAggregate/NotificationTemplate.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;

public class NotificationTemplate : BaseEntity
{
    public string TemplateCode { get; set; }  // e.g. OTP_EMAIL_LOGIN
    public string Channel { get; set; }       // EMAIL | SMS
    public string Language { get; set; } = "en";
    public string? Subject { get; set; }      // email subject only
    public string? BodyHtml { get; set; }     // email HTML
    public string BodyText { get; set; }      // plain text / SMS body
    public string[] Variables { get; set; } = Array.Empty<string>(); // e.g. ["{{otp}}", "{{displayName}}"]
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/AggregatesModel/NotificationAggregate/NotificationModels.cs

```csharp
namespace DodeeEdge.Core.Notification.Domain.AggregatesModel.NotificationAggregate;

public class SendEmailOtpRequest
{
    public string To { get; set; }
    public string Otp { get; set; }
    public string Purpose { get; set; }
    public string? DisplayName { get; set; }
    public string? Language { get; set; } = "en";
}

public class SendSmsOtpRequest
{
    public string To { get; set; }
    public string Otp { get; set; }
    public string Purpose { get; set; }
}

public class SendEmailVerificationRequest
{
    public string To { get; set; }
    public string Token { get; set; }
    public string? DisplayName { get; set; }
    public string? Language { get; set; } = "en";
}

public class SendPasswordResetRequest
{
    public string To { get; set; }
    public string Token { get; set; }
    public string? DisplayName { get; set; }
    public string? Language { get; set; } = "en";
}

public class NotificationResult
{
    public Guid NotificationId { get; set; }
    public string Channel { get; set; }
    public string Status { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/Services/Interface/INotificationService.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.NotificationAggregate;

namespace DodeeEdge.Core.Notification.Domain.Services.Interface;

public interface INotificationService
{
    Task<NotificationResult> SendEmailOtpAsync(SendEmailOtpRequest request);
    Task<NotificationResult> SendSmsOtpAsync(SendSmsOtpRequest request);
    Task<NotificationResult> SendEmailVerificationAsync(SendEmailVerificationRequest request);
    Task<NotificationResult> SendPasswordResetAsync(SendPasswordResetRequest request);
}
```

### src/Domain/Services/Interface/IEmailSender.cs

```csharp
namespace DodeeEdge.Core.Notification.Domain.Services.Interface;

public interface IEmailSender
{
    Task<string?> SendAsync(string to, string subject, string htmlBody, string textBody);
}
```

### src/Domain/Services/Interface/ISmsSender.cs

```csharp
namespace DodeeEdge.Core.Notification.Domain.Services.Interface;

public interface ISmsSender
{
    Task<string?> SendAsync(string toPhone, string message);
}
```

### src/Domain/Services/Interface/ITemplateRenderer.cs

```csharp
namespace DodeeEdge.Core.Notification.Domain.Services.Interface;

public interface ITemplateRenderer
{
    string Render(string template, Dictionary<string, string> variables);
}
```

### src/Domain/Services/NotificationService.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.Notification.Domain.AggregatesModel.NotificationAggregate;
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using DodeeEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace DodeeEdge.Core.Notification.Domain.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationLogRepository _logRepo;
    private readonly INotificationTemplateRepository _templateRepo;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ITemplateRenderer _renderer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork uow,
        INotificationLogRepository logRepo,
        INotificationTemplateRepository templateRepo,
        IEmailSender emailSender,
        ISmsSender smsSender,
        ITemplateRenderer renderer,
        ILogger<NotificationService> logger)
    {
        _uow = uow;
        _logRepo = logRepo;
        _templateRepo = templateRepo;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<NotificationResult> SendEmailOtpAsync(SendEmailOtpRequest request)
    {
        var templateCode = $"OTP_EMAIL_{request.Purpose.ToUpperInvariant()}";
        var template = await _templateRepo.GetByCodeAndLanguageAsync(templateCode, request.Language ?? "en")
                    ?? await _templateRepo.GetByCodeAndLanguageAsync(templateCode, "en");

        var variables = new Dictionary<string, string>
        {
            ["otp"] = request.Otp,
            ["displayName"] = request.DisplayName ?? "",
            ["purpose"] = request.Purpose
        };

        var bodyText = template != null ? _renderer.Render(template.BodyText, variables) : $"Your OTP is: {request.Otp}";
        var bodyHtml = template?.BodyHtml != null ? _renderer.Render(template.BodyHtml, variables) : bodyText;
        var subject = template?.Subject ?? "Your verification code";

        var log = new NotificationLog
        {
            TemplateCode = templateCode,
            Channel = "EMAIL",
            Recipient = MaskEmail(request.To),
            Purpose = request.Purpose,
            Status = "QUEUED"
        };
        _logRepo.Insert(log);
        await _uow.SaveChangesAsync();

        try
        {
            var providerId = await _emailSender.SendAsync(request.To, subject, bodyHtml, bodyText);
            log.Status = "SENT";
            log.ProviderId = providerId;
            log.SentAt = DateTime.UtcNow;
            _logger.LogInformation("Email OTP sent. Purpose={Purpose} Recipient={Recipient}", request.Purpose, log.Recipient);
        }
        catch (Exception ex)
        {
            log.Status = "FAILED";
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Email OTP failed. Purpose={Purpose}", request.Purpose);
        }

        _logRepo.Update(log);
        await _uow.SaveChangesAsync();

        return new NotificationResult { NotificationId = log.Id, Channel = "EMAIL", Status = log.Status };
    }

    public async Task<NotificationResult> SendSmsOtpAsync(SendSmsOtpRequest request)
    {
        var templateCode = $"OTP_SMS_{request.Purpose.ToUpperInvariant()}";
        var template = await _templateRepo.GetByCodeAndLanguageAsync(templateCode, "en");
        var variables = new Dictionary<string, string> { ["otp"] = request.Otp };
        var body = template != null ? _renderer.Render(template.BodyText, variables) : $"Your OTP is: {request.Otp}";

        var log = new NotificationLog
        {
            TemplateCode = templateCode,
            Channel = "SMS",
            Recipient = MaskPhone(request.To),
            Purpose = request.Purpose,
            Status = "QUEUED"
        };
        _logRepo.Insert(log);
        await _uow.SaveChangesAsync();

        try
        {
            var providerId = await _smsSender.SendAsync(request.To, body);
            log.Status = "SENT";
            log.ProviderId = providerId;
            log.SentAt = DateTime.UtcNow;
            _logger.LogInformation("SMS OTP sent. Purpose={Purpose}", request.Purpose);
        }
        catch (Exception ex)
        {
            log.Status = "FAILED";
            log.ErrorMessage = ex.Message;
            _logger.LogError(ex, "SMS OTP failed. Purpose={Purpose}", request.Purpose);
        }

        _logRepo.Update(log);
        await _uow.SaveChangesAsync();

        return new NotificationResult { NotificationId = log.Id, Channel = "SMS", Status = log.Status };
    }

    public async Task<NotificationResult> SendEmailVerificationAsync(SendEmailVerificationRequest request)
    {
        var template = await _templateRepo.GetByCodeAndLanguageAsync("EMAIL_VERIFY", request.Language ?? "en")
                    ?? await _templateRepo.GetByCodeAndLanguageAsync("EMAIL_VERIFY", "en");
        var variables = new Dictionary<string, string>
        {
            ["token"] = request.Token,
            ["displayName"] = request.DisplayName ?? ""
        };
        var bodyText = template != null ? _renderer.Render(template.BodyText, variables) : $"Verify your email: {request.Token}";
        var bodyHtml = template?.BodyHtml != null ? _renderer.Render(template.BodyHtml, variables) : bodyText;
        var subject = template?.Subject ?? "Verify your email";

        var log = new NotificationLog { Channel = "EMAIL", Recipient = MaskEmail(request.To), Purpose = "VERIFY", Status = "QUEUED" };
        _logRepo.Insert(log);
        await _uow.SaveChangesAsync();

        try
        {
            log.ProviderId = await _emailSender.SendAsync(request.To, subject, bodyHtml, bodyText);
            log.Status = "SENT"; log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex) { log.Status = "FAILED"; log.ErrorMessage = ex.Message; }

        _logRepo.Update(log);
        await _uow.SaveChangesAsync();
        return new NotificationResult { NotificationId = log.Id, Channel = "EMAIL", Status = log.Status };
    }

    public async Task<NotificationResult> SendPasswordResetAsync(SendPasswordResetRequest request)
    {
        var template = await _templateRepo.GetByCodeAndLanguageAsync("PASSWORD_RESET", request.Language ?? "en")
                    ?? await _templateRepo.GetByCodeAndLanguageAsync("PASSWORD_RESET", "en");
        var variables = new Dictionary<string, string>
        {
            ["token"] = request.Token,
            ["displayName"] = request.DisplayName ?? ""
        };
        var bodyText = template != null ? _renderer.Render(template.BodyText, variables) : $"Reset your password: {request.Token}";
        var bodyHtml = template?.BodyHtml != null ? _renderer.Render(template.BodyHtml, variables) : bodyText;
        var subject = template?.Subject ?? "Reset your password";

        var log = new NotificationLog { Channel = "EMAIL", Recipient = MaskEmail(request.To), Purpose = "PASSWORD_RESET", Status = "QUEUED" };
        _logRepo.Insert(log);
        await _uow.SaveChangesAsync();

        try
        {
            log.ProviderId = await _emailSender.SendAsync(request.To, subject, bodyHtml, bodyText);
            log.Status = "SENT"; log.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex) { log.Status = "FAILED"; log.ErrorMessage = ex.Message; }

        _logRepo.Update(log);
        await _uow.SaveChangesAsync();
        return new NotificationResult { NotificationId = log.Id, Channel = "EMAIL", Status = log.Status };
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        return email[..2] + new string('*', at - 2) + email[at..];
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length < 6) return phone;
        return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
    }
}
```

---

## Infrastructure Layer

### src/Infrastructure/Configurations/EnvironmentOptions.cs

```csharp
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DodeeEdge.Core.Notification.Infrastructure.Configurations;

[ExcludeFromCodeCoverage]
public class EnvironmentOptions
{
    [Required] public string POSTGRES_CONNECTIONSTRING { get; init; }
    [Required] public string REDIS_CONNECTIONSTRING { get; init; }
    [Required] public string INTERNAL_API_KEY { get; init; }        // shared with core-auth-api
    [Required] public string SENDGRID_API_KEY { get; init; }
    [Required] public string SENDGRID_FROM_EMAIL { get; init; }
    [Required] public string SENDGRID_FROM_NAME { get; init; }
    public string? SMTP_HOST { get; init; }
    public int SMTP_PORT { get; init; } = 587;
    public string? SMTP_USERNAME { get; init; }
    public string? SMTP_PASSWORD { get; init; }
    public string? TWILIO_ACCOUNT_SID { get; init; }
    public string? TWILIO_AUTH_TOKEN { get; init; }
    public string? TWILIO_FROM_NUMBER { get; init; }
    public string EMAIL_PROVIDER { get; init; } = "SENDGRID"; // SENDGRID | SMTP
    public string SMS_PROVIDER { get; init; } = "TWILIO";    // TWILIO
}
```

### src/Infrastructure/DbContexts/NotificationDbContext.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.Notification.Infrastructure.DbContexts.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DodeeEdge.Core.Notification.Infrastructure.DbContexts;

public class NotificationDbContext : DbContext
{
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new NotificationLogEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationTemplateEntityTypeConfiguration());
    }
}

[ExcludeFromCodeCoverage]
public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../API"))
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseNpgsql(config["POSTGRES_CONNECTIONSTRING"]);
        return new NotificationDbContext(optionsBuilder.Options);
    }
}
```

### src/Infrastructure/DbContexts/EntityTypeConfigurations/NotificationLogEntityTypeConfiguration.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DodeeEdge.Core.Notification.Infrastructure.DbContexts.EntityTypeConfigurations;

public class NotificationLogEntityTypeConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Status).HasDefaultValue("QUEUED");
        builder.HasIndex(m => new { m.Status, m.CreatedAt });
        builder.HasIndex(m => m.Recipient);
    }
}
```

### src/Infrastructure/DbContexts/EntityTypeConfigurations/NotificationTemplateEntityTypeConfiguration.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DodeeEdge.Core.Notification.Infrastructure.DbContexts.EntityTypeConfigurations;

public class NotificationTemplateEntityTypeConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.TemplateCode, m.Language }).IsUnique();
        builder.Property(m => m.Variables).HasColumnType("text[]");
    }
}
```

### src/Infrastructure/Repositories/NotificationLogRepository.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.Notification.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DodeeEdge.Core.Notification.Infrastructure.Repositories;

public interface INotificationLogRepository : IRepository<NotificationLog> // use DodeeEdge.Core.DotNet base
{
    Task<List<NotificationLog>> GetListAsync(string? channel, string? status, DateTime? from, DateTime? to, int page, int limit);
}

public class NotificationLogRepository : BaseRepository<NotificationDbContext, NotificationLog>, INotificationLogRepository
{
    public NotificationLogRepository(NotificationDbContext context) : base(context) { }

    public async Task<List<NotificationLog>> GetListAsync(string? channel, string? status, DateTime? from, DateTime? to, int page, int limit)
    {
        var query = Context.NotificationLogs.AsQueryable();
        if (!string.IsNullOrEmpty(channel)) query = query.Where(m => m.Channel == channel);
        if (!string.IsNullOrEmpty(status)) query = query.Where(m => m.Status == status);
        if (from.HasValue) query = query.Where(m => m.CreatedAt >= from);
        if (to.HasValue) query = query.Where(m => m.CreatedAt <= to);
        return await query.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * limit).Take(limit).ToListAsync();
    }
}
```

### src/Infrastructure/Repositories/NotificationTemplateRepository.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.Notification.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DodeeEdge.Core.Notification.Infrastructure.Repositories;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<NotificationTemplate?> GetByCodeAndLanguageAsync(string code, string language);
}

public class NotificationTemplateRepository : BaseRepository<NotificationDbContext, NotificationTemplate>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(NotificationDbContext context) : base(context) { }

    public async Task<NotificationTemplate?> GetByCodeAndLanguageAsync(string code, string language)
        => await Context.NotificationTemplates
            .FirstOrDefaultAsync(m => m.TemplateCode == code && m.Language == language && m.IsActive);
}
```

### src/Infrastructure/Email/SendGridEmailSender.cs

```csharp
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using DodeeEdge.Core.Notification.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DodeeEdge.Core.Notification.Infrastructure.Email;

public class SendGridEmailSender : IEmailSender
{
    private readonly EnvironmentOptions _opts;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(EnvironmentOptions opts, ILogger<SendGridEmailSender> logger)
        => (_opts, _logger) = (opts, logger);

    public async Task<string?> SendAsync(string to, string subject, string htmlBody, string textBody)
    {
        var client = new SendGridClient(_opts.SENDGRID_API_KEY);
        var from = new EmailAddress(_opts.SENDGRID_FROM_EMAIL, _opts.SENDGRID_FROM_NAME);
        var msg = MailHelper.CreateSingleEmail(from, new EmailAddress(to), subject, textBody, htmlBody);
        var response = await client.SendEmailAsync(msg);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync();
            _logger.LogWarning("SendGrid failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"SendGrid error: {response.StatusCode}");
        }

        response.Headers.TryGetValues("X-Message-Id", out var ids);
        return ids?.FirstOrDefault();
    }
}
```

### src/Infrastructure/Sms/TwilioSmsSender.cs

```csharp
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using DodeeEdge.Core.Notification.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace DodeeEdge.Core.Notification.Infrastructure.Sms;

public class TwilioSmsSender : ISmsSender
{
    private readonly EnvironmentOptions _opts;
    private readonly ILogger<TwilioSmsSender> _logger;

    public TwilioSmsSender(EnvironmentOptions opts, ILogger<TwilioSmsSender> logger)
    {
        _opts = opts;
        _logger = logger;
        TwilioClient.Init(opts.TWILIO_ACCOUNT_SID, opts.TWILIO_AUTH_TOKEN);
    }

    public async Task<string?> SendAsync(string toPhone, string message)
    {
        var msg = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_opts.TWILIO_FROM_NUMBER),
            to: new Twilio.Types.PhoneNumber(toPhone));

        if (msg.ErrorCode.HasValue)
        {
            _logger.LogWarning("Twilio error: {Code} {Message}", msg.ErrorCode, msg.ErrorMessage);
            throw new InvalidOperationException($"Twilio error: {msg.ErrorCode} {msg.ErrorMessage}");
        }

        return msg.Sid;
    }
}
```

### src/Infrastructure/Rendering/HandlebarsTemplateRenderer.cs

```csharp
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using HandlebarsDotNet;

namespace DodeeEdge.Core.Notification.Infrastructure.Rendering;

public class HandlebarsTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, Dictionary<string, string> variables)
    {
        var compiled = Handlebars.Compile(template);
        return compiled(variables);
    }
}
```

### src/Infrastructure/Services/NotificationCleanupService.cs

```csharp
using DodeeEdge.Core.Notification.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DodeeEdge.Core.Notification.Infrastructure.Services;

// Purges old notification logs (>30 days) to prevent unbounded table growth
public sealed class NotificationCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationCleanupService> _logger;

    public NotificationCleanupService(IServiceScopeFactory scopeFactory, ILogger<NotificationCleanupService> logger)
        => (_scopeFactory, _logger) = (scopeFactory, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                var cutoff = DateTime.UtcNow.Subtract(Retention);
                var deleted = await db.NotificationLogs.Where(m => m.CreatedAt < cutoff).ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                    _logger.LogInformation("Notification log cleanup: deleted {Count} records", deleted);
            }
            catch (Exception ex) { _logger.LogError(ex, "Notification log cleanup failed"); }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
```

### src/Infrastructure/Extensions/HttpClientFactoryExtension.cs

```csharp
using System.Diagnostics.CodeAnalysis;
using DodeeEdge.Core.Notification.Infrastructure.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace DodeeEdge.Core.Notification.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class HttpClientFactoryExtension
{
    public static IServiceCollection AddCustomHttpClients(this IServiceCollection services, EnvironmentOptions options)
    {
        // add external HTTP clients here if needed
        return services;
    }
}
```

---

## API Layer

### src/API/Extensions/ServiceCollectionExtensions.cs

```csharp
using DodeeEdge.Core.Notification.Domain.Services;
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using DodeeEdge.Core.Notification.Infrastructure.Configurations;
using DodeeEdge.Core.Notification.Infrastructure.DbContexts;
using DodeeEdge.Core.Notification.Infrastructure.Email;
using DodeeEdge.Core.Notification.Infrastructure.Extensions;
using DodeeEdge.Core.Notification.Infrastructure.Rendering;
using DodeeEdge.Core.Notification.Infrastructure.Repositories;
using DodeeEdge.Core.Notification.Infrastructure.Services;
using DodeeEdge.Core.Notification.Infrastructure.Sms;
using DodeeEdge.Core.DotNet.Infrastructure;
using DodeeEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DodeeEdge.Core.Notification.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomConfigurations(this IServiceCollection services, EnvironmentOptions options)
    {
        services.AddDbContextPool<NotificationDbContext>(opt =>
            opt.UseNpgsql(options.POSTGRES_CONNECTIONSTRING, sql =>
            {
                sql.MigrationsAssembly(typeof(NotificationDbContext).GetTypeInfo().Assembly.GetName().Name);
                sql.EnableRetryOnFailure(10, TimeSpan.FromMinutes(10), null);
            }));

        services.AddScoped<DbContext>(m => m.GetService<NotificationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddCustomHttpClients(options);

        // Repositories
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

        // Infrastructure services
        services.AddSingleton<ITemplateRenderer, HandlebarsTemplateRenderer>();

        if (options.EMAIL_PROVIDER == "SMTP")
            services.AddScoped<IEmailSender>(m => new SmtpEmailSender(options,
                m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmtpEmailSender>>()));
        else
            services.AddScoped<IEmailSender>(m => new SendGridEmailSender(options,
                m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SendGridEmailSender>>()));

        services.AddScoped<ISmsSender>(m => new TwilioSmsSender(options,
            m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TwilioSmsSender>>()));

        // Domain services
        services.AddScoped<INotificationService>(m => new NotificationService(
            m.GetRequiredService<IUnitOfWork>(),
            m.GetRequiredService<INotificationLogRepository>(),
            m.GetRequiredService<INotificationTemplateRepository>(),
            m.GetRequiredService<IEmailSender>(),
            m.GetRequiredService<ISmsSender>(),
            m.GetRequiredService<ITemplateRenderer>(),
            m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NotificationService>>()));

        services.AddHostedService<NotificationCleanupService>();

        return services;
    }
}
```

### src/API/Controllers/NotificationController.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.NotificationAggregate;
using DodeeEdge.Core.Notification.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DodeeEdge.Core.Notification.API.Controllers;

[Route("api/notifications")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
        => _notificationService = notificationService;

    // POST /api/notifications/otp/email
    [HttpPost("otp/email")]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
    {
        var result = await _notificationService.SendEmailOtpAsync(request);
        return Ok(result);
    }

    // POST /api/notifications/otp/sms
    [HttpPost("otp/sms")]
    public async Task<IActionResult> SendSmsOtp([FromBody] SendSmsOtpRequest request)
    {
        var result = await _notificationService.SendSmsOtpAsync(request);
        return Ok(result);
    }

    // POST /api/notifications/email/verify
    [HttpPost("email/verify")]
    public async Task<IActionResult> SendEmailVerification([FromBody] SendEmailVerificationRequest request)
    {
        var result = await _notificationService.SendEmailVerificationAsync(request);
        return Ok(result);
    }

    // POST /api/notifications/email/password-reset
    [HttpPost("email/password-reset")]
    public async Task<IActionResult> SendPasswordReset([FromBody] SendPasswordResetRequest request)
    {
        var result = await _notificationService.SendPasswordResetAsync(request);
        return Ok(result);
    }
}
```

### src/API/Controllers/TemplateController.cs

```csharp
using DodeeEdge.Core.Notification.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.Notification.Infrastructure.Repositories;
using DodeeEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DodeeEdge.Core.Notification.API.Controllers;

[Route("api/templates")]
[ApiController]
public class TemplateController : ControllerBase
{
    private readonly INotificationTemplateRepository _repo;
    private readonly IUnitOfWork _uow;

    public TemplateController(INotificationTemplateRepository repo, IUnitOfWork uow)
        => (_repo, _uow) = (repo, uow);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NotificationTemplate template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        _repo.Insert(template);
        await _uow.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NotificationTemplate body)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        item.Subject = body.Subject;
        item.BodyHtml = body.BodyHtml;
        item.BodyText = body.BodyText;
        item.Variables = body.Variables;
        item.IsActive = body.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        _repo.Update(item);
        await _uow.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        _repo.Delete(item);
        await _uow.SaveChangesAsync();
        return NoContent();
    }
}
```

### src/API/Controllers/LogController.cs

```csharp
using DodeeEdge.Core.Notification.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DodeeEdge.Core.Notification.API.Controllers;

[Route("api/logs")]
[ApiController]
public class LogController : ControllerBase
{
    private readonly INotificationLogRepository _repo;

    public LogController(INotificationLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var items = await _repo.GetListAsync(channel, status, from, to, page, limit);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }
}
```

### src/API/Program.cs

```csharp
using DodeeEdge.Core.Notification.API.Extensions;
using DodeeEdge.Core.Notification.Infrastructure.Configurations;
using DodeeEdge.Core.Notification.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var environmentOptions = builder.Services.AddCustomOptions<EnvironmentOptions>(builder.Configuration);

builder.Services.AddCustomConfigurations(environmentOptions);
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
    options.AddPolicy("NotificationCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    }));

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = environmentOptions.REDIS_CONNECTIONSTRING);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
    setup.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification API", Version = "v1" }));

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseHttpsRedirection();
app.UseCors("NotificationCors");

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Internal API Key middleware — protects /api/notifications/* from public access
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/notifications"))
    {
        if (!ctx.Request.Headers.TryGetValue("X-Internal-Api-Key", out var key) ||
            key != environmentOptions.INTERNAL_API_KEY)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});

app.MapControllers();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { AllowCachingResponses = false });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
```

### src/API/appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "POSTGRES_CONNECTIONSTRING": "Host=192.168.1.99; Port=5432; User ID=postgres; Password=1234; Database=notification-dev; Pooling=true; Connection Idle Lifetime=10; Minimum Pool Size=2; Maximum Pool Size=20;",
  "REDIS_CONNECTIONSTRING": "localhost:6379",
  "INTERNAL_API_KEY": "change-me-in-production",
  "SENDGRID_API_KEY": "SG.xxx",
  "SENDGRID_FROM_EMAIL": "noreply@yourdomain.com",
  "SENDGRID_FROM_NAME": "DodeeEdge",
  "EMAIL_PROVIDER": "SENDGRID",
  "SMS_PROVIDER": "TWILIO",
  "TWILIO_ACCOUNT_SID": "",
  "TWILIO_AUTH_TOKEN": "",
  "TWILIO_FROM_NUMBER": "",
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": []
  }
}
```

---

## หมายเหตุสำคัญ

1. `BaseEntity`, `IRepository<T>`, `BaseRepository<TContext, TEntity>`, `IUnitOfWork`, `UnitOfWork` มาจาก NuGet package `DodeeEdge.Core.DotNet.Infrastructure` — ต้องเพิ่ม reference เดียวกับ core-auth-api
2. `AddCustomOptions<T>()` มาจาก `DodeeEdge.Core.DotNet` — extension method ใน Program.cs
3. SMTP fallback: สร้าง `SmtpEmailSender` เพิ่มเติมโดยใช้ `System.Net.Mail.SmtpClient` หรือ `MailKit`
4. Build ต้องผ่าน 0 errors ก่อน commit

This service is part of a microservice ecosystem. Its only responsibility is **delivering notifications** (email and SMS). It does **not** generate OTP codes — it only delivers what it receives.

---

## SYSTEM CONTEXT

This service sits alongside:

| Service         | Purpose                                                                                            |
| --------------- | -------------------------------------------------------------------------------------------------- |
| `core-auth-api` | OAuth 2.1 Identity Server — calls this service to deliver OTPs, email verification, password reset |
| `bp-api`        | Business Profile API — may call this service for transactional notifications                       |

---

## TECHNOLOGY STACK

| Layer          | Technology                                                       |
| -------------- | ---------------------------------------------------------------- |
| Framework      | .NET 9 / ASP.NET Core                                            |
| Architecture   | Clean Architecture (Domain → Application → Infrastructure → API) |
| Database       | PostgreSQL + EF Core (for logs and templates)                    |
| Email Provider | Primary: **SendGrid**; Fallback: **SMTP**                        |
| SMS Provider   | Primary: **Twilio**; Fallback: **AWS SNS**                       |
| Queue          | **RabbitMQ** (async delivery via events)                         |
| Cache          | Redis (rate limiting, deduplication)                             |
| Auth           | Bearer JWT (validate token issued by `core-auth-api`)            |

---

## API ENDPOINTS

Implement all of these:

### OTP Delivery

```
POST /api/notifications/otp/email
POST /api/notifications/otp/sms
```

### Email Notifications

```
POST /api/notifications/email/verify          -- email verification link
POST /api/notifications/email/password-reset  -- password reset link
POST /api/notifications/email/welcome         -- welcome after registration
POST /api/notifications/email/generic         -- generic template by name
```

### SMS Notifications

```
POST /api/notifications/sms/generic           -- SMS by template name
```

### Template Management (Admin)

```
GET    /api/templates
GET    /api/templates/{id}
POST   /api/templates
PUT    /api/templates/{id}
DELETE /api/templates/{id}
```

### Delivery Logs (Admin)

```
GET /api/logs?page=1&limit=20&channel=EMAIL&status=FAILED&from=...&to=...
GET /api/logs/{id}
```

### Health

```
GET /health/ready
GET /health/live
```

---

## REQUEST / RESPONSE CONTRACTS

### POST /api/notifications/otp/email

```json
{
  "to": "user@example.com",
  "otp": "847291",
  "purpose": "LOGIN",
  "displayName": "John Doe",
  "language": "en"
}
```

### POST /api/notifications/otp/sms

```json
{
  "to": "+6681234567",
  "otp": "847291",
  "purpose": "MFA"
}
```

### POST /api/notifications/email/verify

```json
{
  "to": "user@example.com",
  "token": "abc123verifytoken",
  "displayName": "John Doe",
  "language": "th"
}
```

### POST /api/notifications/email/password-reset

```json
{
  "to": "user@example.com",
  "token": "reset-token-here",
  "displayName": "John Doe",
  "language": "en"
}
```

All endpoints respond with:

```json
{
  "notificationId": "uuid",
  "channel": "EMAIL",
  "status": "QUEUED",
  "queuedAt": "2026-01-01T12:00:00Z"
}
```

---

## DATABASE SCHEMA

### Table: `notification_templates`

```sql
CREATE TABLE notification_templates (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_code   VARCHAR(100) NOT NULL UNIQUE,  -- e.g. "OTP_EMAIL_LOGIN"
    channel         VARCHAR(10) NOT NULL,           -- EMAIL | SMS
    language        VARCHAR(10) NOT NULL DEFAULT 'en',
    subject         VARCHAR(255),                   -- Email subject (null for SMS)
    body_html       TEXT,                           -- Email HTML body
    body_text       TEXT NOT NULL,                  -- Plain text / SMS body
    variables       TEXT[],                         -- e.g. ['{{otp}}', '{{displayName}}']
    is_active       BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (template_code, language)
);
```

### Table: `notification_logs`

```sql
CREATE TABLE notification_logs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_code   VARCHAR(100),
    channel         VARCHAR(10) NOT NULL,    -- EMAIL | SMS
    recipient       VARCHAR(320) NOT NULL,   -- email or phone (masked in SELECT)
    purpose         VARCHAR(50),             -- LOGIN | REGISTER | VERIFY | PASSWORD_RESET
    provider        VARCHAR(30),             -- SENDGRID | SMTP | TWILIO | AWS_SNS
    provider_id     VARCHAR(255),            -- provider's message ID for tracking
    status          VARCHAR(20) NOT NULL,    -- QUEUED | SENT | DELIVERED | FAILED | BOUNCED
    error_message   TEXT,
    retry_count     INT NOT NULL DEFAULT 0,
    sent_at         TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_notification_logs_recipient (recipient),
    INDEX idx_notification_logs_status (status, created_at DESC),
    INDEX idx_notification_logs_purpose (purpose, created_at DESC)
);
```

### Table: `notification_rate_limits` (fallback if Redis is unavailable)

```sql
CREATE TABLE notification_rate_limits (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    target      VARCHAR(320) NOT NULL,  -- email or phone
    purpose     VARCHAR(50) NOT NULL,
    count       INT NOT NULL DEFAULT 1,
    window_end  TIMESTAMPTZ NOT NULL,

    UNIQUE (target, purpose, window_end)
);
```

---

## DOMAIN LAYER

### Entities

```csharp
// NotificationLog.cs
public class NotificationLog
{
    public Guid Id { get; set; }
    public string? TemplateCode { get; set; }
    public string Channel { get; set; }      // EMAIL | SMS
    public string Recipient { get; set; }
    public string? Purpose { get; set; }
    public string? Provider { get; set; }
    public string? ProviderId { get; set; }
    public string Status { get; set; }       // QUEUED | SENT | DELIVERED | FAILED
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// NotificationTemplate.cs
public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; }  // e.g. "OTP_EMAIL_LOGIN"
    public string Channel { get; set; }
    public string Language { get; set; } = "en";
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }
    public string BodyText { get; set; }
    public string[] Variables { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### Service Interfaces

```csharp
public interface IEmailSender
{
    Task<string?> SendAsync(string to, string subject, string htmlBody, string textBody);
}

public interface ISmsSender
{
    Task<string?> SendAsync(string toPhone, string message);
}

public interface ITemplateRenderer
{
    string Render(string template, Dictionary<string, string> variables);
}

public interface INotificationService
{
    Task<NotificationResult> SendEmailOtpAsync(SendEmailOtpRequest request);
    Task<NotificationResult> SendSmsOtpAsync(SendSmsOtpRequest request);
    Task<NotificationResult> SendEmailVerificationAsync(SendEmailVerificationRequest request);
    Task<NotificationResult> SendPasswordResetAsync(SendPasswordResetRequest request);
}
```

---

## INFRASTRUCTURE LAYER

### Email Providers

Implement both with the same interface — switch via config:

```csharp
// SendGridEmailSender.cs — primary
public class SendGridEmailSender : IEmailSender
{
    // Use SendGrid NuGet: SendGrid
    // POST https://api.sendgrid.com/v3/mail/send
}

// SmtpEmailSender.cs — fallback
public class SmtpEmailSender : IEmailSender
{
    // Use System.Net.Mail.SmtpClient or MailKit
}
```

### SMS Providers

```csharp
// TwilioSmsSender.cs — primary
public class TwilioSmsSender : ISmsSender
{
    // Use Twilio NuGet: Twilio
}

// AwsSnsSmsSender.cs — fallback
public class AwsSnsSmsSender : ISmsSender
{
    // Use AWSSDK.SimpleNotificationService NuGet
}
```

### Template Renderer (Handlebars)

```csharp
// Use HandlebarsDotNet NuGet for template rendering
public class HandlebarsTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, Dictionary<string, string> variables)
    {
        var compiled = Handlebars.Compile(template);
        return compiled(variables);
    }
}
```

---

## SECURITY

1. **Authentication**: Validate Bearer JWT from `core-auth-api` on all write endpoints
2. **Rate Limiting**: Max 3 OTP sends per 15 minutes per recipient (Redis-backed)
3. **Input Validation**: Validate email format, phone E.164 format
4. **Log Masking**: Mask recipient in logs: `user***@example.com`, `+66***4567`
5. **Internal-Only Write Endpoints**: Restrict `/api/notifications/otp/*` to internal service calls only (API Key or mTLS)
6. **Idempotency**: Accept `Idempotency-Key` header — deduplicate sends within 5 minutes

---

## ENVIRONMENT VARIABLES

```env
POSTGRES_CONNECTIONSTRING=Host=...;Database=notification-db;
REDIS_CONNECTIONSTRING=localhost:6379
SENDGRID_API_KEY=SG.xxx
SENDGRID_FROM_EMAIL=noreply@yourdomain.com
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USERNAME=...
SMTP_PASSWORD=...
TWILIO_ACCOUNT_SID=ACxxx
TWILIO_AUTH_TOKEN=xxx
TWILIO_FROM_NUMBER=+1234567890
AWS_SNS_REGION=ap-southeast-1
RABBITMQ_HOST=localhost
AUTH_JWKS_ENDPOINT=https://auth.yourdomain.com/.well-known/jwks.json
INTERNAL_API_KEY=your-internal-service-key
```

---

## PROJECT STRUCTURE

```
notification-api/
├── DodeeEdge.Core.Notification.sln
└── src/
    ├── API/
    │   ├── API.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── Controllers/
    │   │   ├── NotificationController.cs
    │   │   ├── TemplateController.cs
    │   │   └── LogController.cs
    │   └── Extensions/
    │       └── ServiceCollectionExtensions.cs
    ├── Domain/
    │   ├── Domain.csproj
    │   ├── Entities/
    │   │   ├── NotificationLog.cs
    │   │   └── NotificationTemplate.cs
    │   └── Services/
    │       └── Interface/
    │           ├── IEmailSender.cs
    │           ├── ISmsSender.cs
    │           ├── ITemplateRenderer.cs
    │           └── INotificationService.cs
    ├── Application/
    │   ├── Application.csproj
    │   └── Services/
    │       └── NotificationService.cs
    └── Infrastructure/
        ├── Infrastructure.csproj
        ├── DbContexts/
        │   └── NotificationDbContext.cs
        ├── Repositories/
        │   ├── NotificationLogRepository.cs
        │   └── NotificationTemplateRepository.cs
        ├── Email/
        │   ├── SendGridEmailSender.cs
        │   └── SmtpEmailSender.cs
        ├── Sms/
        │   ├── TwilioSmsSender.cs
        │   └── AwsSnsSmsSender.cs
        └── Rendering/
            └── HandlebarsTemplateRenderer.cs
```

---

## DELIVERABLES

1. Full working ASP.NET Core 9 project with the structure above
2. EF Core migrations for all tables
3. SendGrid + SMTP email sender implementations
4. Twilio SMS sender implementation
5. Handlebars template renderer
6. All 5 controllers with full DI wiring
7. Rate limiting middleware
8. Seed data for default templates:
   - `OTP_EMAIL_LOGIN` (EN + TH)
   - `OTP_EMAIL_REGISTER` (EN + TH)
   - `OTP_SMS_LOGIN` (EN + TH)
   - `EMAIL_VERIFY` (EN + TH)
   - `PASSWORD_RESET` (EN + TH)
9. Unit tests for `NotificationService` and template renderer
10. `appsettings.json` with all environment variable bindings
11. Build must succeed with 0 errors
