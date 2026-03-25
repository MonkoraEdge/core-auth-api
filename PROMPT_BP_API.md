# Prompt: สร้าง bp-api (Business Profile API)

---

## บทบาท

คุณคือ Senior .NET Backend Architect

สร้าง **bp-api** microservice ด้วย **C# .NET 9 / ASP.NET Core** โดยใช้โครงสร้างเดียวกับ `core-auth-api` ทุกประการ

---

## โครงสร้าง Solution ที่ต้องสร้าง

```
DodeeEdge.Core.BP.sln
README.md
src/
    API/
        API.csproj
        Program.cs
        appsettings.json
        appsettings.Development.json
        Controllers/
            CustomerSyncController.cs
            CustomerController.cs
            OrganizationController.cs
            AdminController.cs
        Extensions/
            ServiceCollectionExtensions.cs
        Properties/
            launchSettings.json
    Domain/
        Domain.csproj
        AggregatesModel/
            EntityAggregate/
                Customer.cs
                CustomerLinkedIdentity.cs
                Organization.cs
                OrganizationAddress.cs
                OrganizationMember.cs
                CustomerAuditLog.cs
            CustomerAggregate/
                CustomerModels.cs
            OrganizationAggregate/
                OrganizationModels.cs
        Services/
            CustomerService.cs
            OrganizationService.cs
            Interface/
                ICustomerService.cs
                IOrganizationService.cs
    Infrastructure/
        Infrastructure.csproj
        Configurations/
            EnvironmentOptions.cs
        DbContexts/
            BpDbContext.cs
            EntityTypeConfigurations/
                CustomerEntityTypeConfiguration.cs
                CustomerLinkedIdentityEntityTypeConfiguration.cs
                OrganizationEntityTypeConfiguration.cs
                OrganizationAddressEntityTypeConfiguration.cs
                OrganizationMemberEntityTypeConfiguration.cs
                CustomerAuditLogEntityTypeConfiguration.cs
        Migrations/
        Repositories/
            CustomerRepository.cs
            OrganizationRepository.cs
        Extensions/
            HttpClientFactoryExtension.cs
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
    <AssemblyName>DodeeEdge.Core.BP.API</AssemblyName>
    <RootNamespace>DodeeEdge.Core.BP.API</RootNamespace>
    <PackageId>DodeeEdge.Core.BP.API</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>DO DEE 365 ONE Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.2" />
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
    <AssemblyName>DodeeEdge.Core.BP.Domain</AssemblyName>
    <RootNamespace>DodeeEdge.Core.BP.Domain</RootNamespace>
    <PackageId>DodeeEdge.Core.BP.Domain</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>DO DEE 365 ONE Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
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
    <AssemblyName>DodeeEdge.Core.BP.Infrastructure</AssemblyName>
    <RootNamespace>DodeeEdge.Core.BP.Infrastructure</RootNamespace>
    <PackageId>DodeeEdge.Core.BP.Infrastructure</PackageId>
    <Authors>Boonhome Wongsuwan</Authors>
    <Company>DO DEE 365 ONE Co., Ltd.</Company>
    <Version Condition="'$(Version)' == ''">1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
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

- API layer: `DodeeEdge.Core.BP.API.*`
- Domain layer: `DodeeEdge.Core.BP.Domain.*`
- Infrastructure layer: `DodeeEdge.Core.BP.Infrastructure.*`

---

## Domain Layer

### src/Domain/AggregatesModel/EntityAggregate/Customer.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.Interfaces;
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class Customer : BaseEntity, ISoftDelete
{
    public Guid UserId { get; set; }          // auth server identity key (sub claim)
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public string Status { get; set; } = "ACTIVE";   // ACTIVE | SUSPENDED | DELETED
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/AggregatesModel/EntityAggregate/CustomerLinkedIdentity.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class CustomerLinkedIdentity : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string ProviderCode { get; set; }    // GOOGLE | FACEBOOK | APPLE | LINE | MICROSOFT
    public string ExternalUserId { get; set; }
    public string? ExternalEmail { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

### src/Domain/AggregatesModel/EntityAggregate/Organization.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.Interfaces;
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class Organization : BaseEntity, ISoftDelete
{
    public string OrganizationName { get; set; }
    public string? OrganizationSlug { get; set; }  // URL-safe, unique
    public string? TaxId { get; set; }
    public string? BusinessType { get; set; }      // COMPANY | INDIVIDUAL | NGO
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/AggregatesModel/EntityAggregate/OrganizationAddress.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class OrganizationAddress : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string AddressType { get; set; } = "MAIN";  // MAIN | BILLING | SHIPPING
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }   // ISO 3166-1 alpha-2
    public string? PostalCode { get; set; }
    public bool IsPrimary { get; set; } = true;
}
```

### src/Domain/AggregatesModel/EntityAggregate/OrganizationMember.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class OrganizationMember : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CustomerId { get; set; }
    public string Role { get; set; } = "MEMBER"; // OWNER | ADMIN | MEMBER
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
}
```

### src/Domain/AggregatesModel/EntityAggregate/CustomerAuditLog.cs

```csharp
using DodeeEdge.Core.DotNet.Domain.SeedWork;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

public class CustomerAuditLog : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public string Action { get; set; }        // PROFILE_UPDATED | ORG_CREATED | STATUS_CHANGED
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }      // JSON string
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### src/Domain/AggregatesModel/CustomerAggregate/CustomerModels.cs

```csharp
namespace DodeeEdge.Core.BP.Domain.AggregatesModel.CustomerAggregate;

public class EnsureCustomerRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? Locale { get; set; }
}

public class LinkIdentityRequest
{
    public Guid UserId { get; set; }
    public string ProviderCode { get; set; }
    public string ExternalUserId { get; set; }
    public string? ExternalEmail { get; set; }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Locale { get; set; }
    public string? Timezone { get; set; }
}

public class CustomerResult
{
    public Guid CustomerId { get; set; }
    public bool IsNew { get; set; }
}

public class CustomerProfile
{
    public Guid CustomerId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; }
    public string Timezone { get; set; }
    public string Status { get; set; }
    public List<string> LinkedProviders { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
```

### src/Domain/AggregatesModel/OrganizationAggregate/OrganizationModels.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;

namespace DodeeEdge.Core.BP.Domain.AggregatesModel.OrganizationAggregate;

public class CreateOrganizationRequest
{
    public string OrganizationName { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public OrganizationAddress? Address { get; set; }
}

public class UpdateOrganizationRequest
{
    public string? OrganizationName { get; set; }
    public string? TaxId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
}

public class OrganizationResult
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public string? OrganizationSlug { get; set; }
    public string Status { get; set; }
}

public class MemberResult
{
    public Guid CustomerId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

### src/Domain/Services/Interface/ICustomerService.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.CustomerAggregate;

namespace DodeeEdge.Core.BP.Domain.Services.Interface;

public interface ICustomerService
{
    Task<CustomerResult> EnsureAsync(EnsureCustomerRequest request);
    Task LinkIdentityAsync(LinkIdentityRequest request);
    Task<CustomerProfile> GetByUserIdAsync(Guid userId);
    Task<CustomerProfile> UpdateAsync(Guid userId, UpdateProfileRequest request);
    Task RequestDeletionAsync(Guid userId);
}
```

### src/Domain/Services/Interface/IOrganizationService.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.OrganizationAggregate;

namespace DodeeEdge.Core.BP.Domain.Services.Interface;

public interface IOrganizationService
{
    Task<OrganizationResult> CreateAsync(Guid ownerUserId, CreateOrganizationRequest request);
    Task<OrganizationResult> GetByIdAsync(Guid id);
    Task UpdateAsync(Guid id, Guid requestingUserId, UpdateOrganizationRequest request);
    Task DeleteAsync(Guid id, Guid requestingUserId);
    Task AddMemberAsync(Guid organizationId, Guid requestingUserId, Guid targetCustomerId, string role);
    Task RemoveMemberAsync(Guid organizationId, Guid requestingUserId, Guid targetCustomerId);
    Task<List<MemberResult>> GetMembersAsync(Guid organizationId);
}
```

### src/Domain/Services/CustomerService.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.CustomerAggregate;
using DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.BP.Domain.Services.Interface;
using DodeeEdge.Core.BP.Infrastructure.Repositories;
using DodeeEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace DodeeEdge.Core.BP.Domain.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly ICustomerRepository _customerRepo;
    private readonly ICustomerLinkedIdentityRepository _identityRepo;
    private readonly ICustomerAuditLogRepository _auditRepo;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        IUnitOfWork uow,
        ICustomerRepository customerRepo,
        ICustomerLinkedIdentityRepository identityRepo,
        ICustomerAuditLogRepository auditRepo,
        ILogger<CustomerService> logger)
    {
        _uow = uow;
        _customerRepo = customerRepo;
        _identityRepo = identityRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    public async Task<CustomerResult> EnsureAsync(EnsureCustomerRequest request)
    {
        var existing = await _customerRepo.GetByUserIdAsync(request.UserId);
        if (existing != null)
        {
            // Update fields if changed
            existing.Email = request.Email;
            if (!string.IsNullOrEmpty(request.DisplayName)) existing.DisplayName = request.DisplayName;
            if (!string.IsNullOrEmpty(request.Phone)) existing.Phone = request.Phone;
            existing.UpdatedAt = DateTime.UtcNow;
            _customerRepo.Update(existing);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Customer updated. UserId={UserId}", request.UserId);
            return new CustomerResult { CustomerId = existing.Id, IsNew = false };
        }

        var customer = new Customer
        {
            UserId = request.UserId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Phone = request.Phone,
            Locale = request.Locale ?? "en",
            Status = "ACTIVE",
            IsActive = true
        };
        _customerRepo.Insert(customer);

        _auditRepo.Insert(new CustomerAuditLog
        {
            CustomerId = customer.Id,
            Action = "CUSTOMER_CREATED",
            ResourceType = "Customer",
            ResourceId = customer.Id
        });

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Customer created. UserId={UserId} CustomerId={CustomerId}", request.UserId, customer.Id);
        return new CustomerResult { CustomerId = customer.Id, IsNew = true };
    }

    public async Task LinkIdentityAsync(LinkIdentityRequest request)
    {
        var customer = await _customerRepo.GetByUserIdAsync(request.UserId);
        if (customer == null) return;

        var existingLink = await _identityRepo.GetByProviderAndExternalIdAsync(
            request.ProviderCode, request.ExternalUserId);
        if (existingLink != null)
        {
            existingLink.LastLoginAt = DateTime.UtcNow;
            _identityRepo.Update(existingLink);
        }
        else
        {
            _identityRepo.Insert(new CustomerLinkedIdentity
            {
                CustomerId = customer.Id,
                ProviderCode = request.ProviderCode,
                ExternalUserId = request.ExternalUserId,
                ExternalEmail = request.ExternalEmail
            });
        }

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Identity linked. UserId={UserId} Provider={Provider}", request.UserId, request.ProviderCode);
    }

    public async Task<CustomerProfile> GetByUserIdAsync(Guid userId)
    {
        var customer = await _customerRepo.GetByUserIdWithIdentitiesAsync(userId);
        if (customer == null) throw new KeyNotFoundException($"Customer not found for userId={userId}");
        return MapToProfile(customer);
    }

    public async Task<CustomerProfile> UpdateAsync(Guid userId, UpdateProfileRequest request)
    {
        var customer = await _customerRepo.GetByUserIdAsync(userId);
        if (customer == null) throw new KeyNotFoundException($"Customer not found for userId={userId}");

        if (!string.IsNullOrEmpty(request.DisplayName)) customer.DisplayName = request.DisplayName;
        if (!string.IsNullOrEmpty(request.Phone)) customer.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.AvatarUrl)) customer.AvatarUrl = request.AvatarUrl;
        if (!string.IsNullOrEmpty(request.Locale)) customer.Locale = request.Locale;
        if (!string.IsNullOrEmpty(request.Timezone)) customer.Timezone = request.Timezone;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepo.Update(customer);
        _auditRepo.Insert(new CustomerAuditLog { CustomerId = customer.Id, Action = "PROFILE_UPDATED", PerformedBy = userId });
        await _uow.SaveChangesAsync();
        return MapToProfile(customer);
    }

    public async Task RequestDeletionAsync(Guid userId)
    {
        var customer = await _customerRepo.GetByUserIdAsync(userId);
        if (customer == null) return;
        customer.Status = "DELETED";
        customer.DeletedAt = DateTime.UtcNow;
        _customerRepo.Update(customer);
        _auditRepo.Insert(new CustomerAuditLog { CustomerId = customer.Id, Action = "ACCOUNT_DELETION_REQUESTED", PerformedBy = userId });
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Account deletion requested. UserId={UserId}", userId);
    }

    private static CustomerProfile MapToProfile(Customer c) => new()
    {
        CustomerId = c.Id,
        UserId = c.UserId,
        Email = c.Email,
        DisplayName = c.DisplayName,
        Phone = c.Phone,
        AvatarUrl = c.AvatarUrl,
        Locale = c.Locale,
        Timezone = c.Timezone,
        Status = c.Status,
        CreatedAt = c.CreatedAt
    };
}
```

---

## Infrastructure Layer

### src/Infrastructure/Configurations/EnvironmentOptions.cs

```csharp
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace DodeeEdge.Core.BP.Infrastructure.Configurations;

[ExcludeFromCodeCoverage]
public class EnvironmentOptions
{
    [Required] public string POSTGRES_CONNECTIONSTRING { get; init; }
    [Required] public string REDIS_CONNECTIONSTRING { get; init; }
    [Required] public string INTERNAL_API_KEY { get; init; }        // shared with core-auth-api
    [Required] public string AUTH_ISSUER { get; init; }             // https://auth.yourdomain.com
    [Required] public string AUTH_JWKS_ENDPOINT { get; init; }     // JWKS URL for JWT validation
}
```

### src/Infrastructure/DbContexts/BpDbContext.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.BP.Infrastructure.DbContexts.EntityTypeConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DodeeEdge.Core.BP.Infrastructure.DbContexts;

public class BpDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerLinkedIdentity> CustomerLinkedIdentities { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationAddress> OrganizationAddresses { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<CustomerAuditLog> CustomerAuditLogs { get; set; }

    public BpDbContext(DbContextOptions<BpDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerLinkedIdentityEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationAddressEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMemberEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerAuditLogEntityTypeConfiguration());
    }
}

[ExcludeFromCodeCoverage]
public class BpDbContextFactory : IDesignTimeDbContextFactory<BpDbContext>
{
    public BpDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../API"))
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        var optionsBuilder = new DbContextOptionsBuilder<BpDbContext>();
        optionsBuilder.UseNpgsql(config["POSTGRES_CONNECTIONSTRING"]);
        return new BpDbContext(optionsBuilder.Options);
    }
}
```

### EntityTypeConfigurations (สร้างทุกไฟล์)

```csharp
// CustomerEntityTypeConfiguration.cs
public class CustomerEntityTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("bp_customers");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.UserId).IsUnique();
        builder.HasIndex(m => m.Email);
        builder.HasIndex(m => m.Status);
        builder.Property(m => m.Status).HasDefaultValue("ACTIVE");
    }
}

// CustomerLinkedIdentityEntityTypeConfiguration.cs
public class CustomerLinkedIdentityEntityTypeConfiguration : IEntityTypeConfiguration<CustomerLinkedIdentity>
{
    public void Configure(EntityTypeBuilder<CustomerLinkedIdentity> builder)
    {
        builder.ToTable("bp_customer_linked_identities");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.ProviderCode, m.ExternalUserId });
    }
}

// OrganizationEntityTypeConfiguration.cs
public class OrganizationEntityTypeConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("bp_organizations");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.OrganizationSlug).IsUnique();
        builder.Property(m => m.Status).HasDefaultValue("ACTIVE");
    }
}

// OrganizationAddressEntityTypeConfiguration.cs
public class OrganizationAddressEntityTypeConfiguration : IEntityTypeConfiguration<OrganizationAddress>
{
    public void Configure(EntityTypeBuilder<OrganizationAddress> builder)
    {
        builder.ToTable("bp_organization_addresses");
        builder.HasKey(m => m.Id);
    }
}

// OrganizationMemberEntityTypeConfiguration.cs
public class OrganizationMemberEntityTypeConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("bp_organization_members");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.OrganizationId, m.CustomerId }).IsUnique();
    }
}

// CustomerAuditLogEntityTypeConfiguration.cs
public class CustomerAuditLogEntityTypeConfiguration : IEntityTypeConfiguration<CustomerAuditLog>
{
    public void Configure(EntityTypeBuilder<CustomerAuditLog> builder)
    {
        builder.ToTable("bp_customer_audit_logs");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.CustomerId, m.CreatedAt });
    }
}
```

### src/Infrastructure/Repositories/CustomerRepository.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.BP.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DodeeEdge.Core.BP.Infrastructure.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(Guid userId);
    Task<Customer?> GetByUserIdWithIdentitiesAsync(Guid userId);
}

public class CustomerRepository : BaseRepository<BpDbContext, Customer>, ICustomerRepository
{
    public CustomerRepository(BpDbContext context) : base(context) { }

    public async Task<Customer?> GetByUserIdAsync(Guid userId)
        => await Context.Customers.FirstOrDefaultAsync(m => m.UserId == userId && m.DeletedAt == null);

    public async Task<Customer?> GetByUserIdWithIdentitiesAsync(Guid userId)
        => await Context.Customers
            .Include(m => m.LinkedIdentities)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.DeletedAt == null);
}

public interface ICustomerLinkedIdentityRepository : IRepository<CustomerLinkedIdentity>
{
    Task<CustomerLinkedIdentity?> GetByProviderAndExternalIdAsync(string providerCode, string externalUserId);
}

public class CustomerLinkedIdentityRepository : BaseRepository<BpDbContext, CustomerLinkedIdentity>, ICustomerLinkedIdentityRepository
{
    public CustomerLinkedIdentityRepository(BpDbContext context) : base(context) { }

    public async Task<CustomerLinkedIdentity?> GetByProviderAndExternalIdAsync(string providerCode, string externalUserId)
        => await Context.CustomerLinkedIdentities
            .FirstOrDefaultAsync(m => m.ProviderCode == providerCode && m.ExternalUserId == externalUserId);
}

public interface ICustomerAuditLogRepository : IRepository<CustomerAuditLog> { }

public class CustomerAuditLogRepository : BaseRepository<BpDbContext, CustomerAuditLog>, ICustomerAuditLogRepository
{
    public CustomerAuditLogRepository(BpDbContext context) : base(context) { }
}
```

### src/Infrastructure/Repositories/OrganizationRepository.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.EntityAggregate;
using DodeeEdge.Core.BP.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DodeeEdge.Core.BP.Infrastructure.Repositories;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetByIdWithMembersAsync(Guid id);
}

public class OrganizationRepository : BaseRepository<BpDbContext, Organization>, IOrganizationRepository
{
    public OrganizationRepository(BpDbContext context) : base(context) { }

    public async Task<Organization?> GetByIdWithMembersAsync(Guid id)
        => await Context.Organizations
            .Include(m => m.Members)
            .Include(m => m.Addresses)
            .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);
}

public interface IOrganizationMemberRepository : IRepository<OrganizationMember>
{
    Task<OrganizationMember?> GetAsync(Guid organizationId, Guid customerId);
}

public class OrganizationMemberRepository : BaseRepository<BpDbContext, OrganizationMember>, IOrganizationMemberRepository
{
    public OrganizationMemberRepository(BpDbContext context) : base(context) { }

    public async Task<OrganizationMember?> GetAsync(Guid organizationId, Guid customerId)
        => await Context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.CustomerId == customerId && m.LeftAt == null);
}
```

---

## API Layer

### src/API/Extensions/ServiceCollectionExtensions.cs

```csharp
using DodeeEdge.Core.BP.Domain.Services;
using DodeeEdge.Core.BP.Domain.Services.Interface;
using DodeeEdge.Core.BP.Infrastructure.Configurations;
using DodeeEdge.Core.BP.Infrastructure.DbContexts;
using DodeeEdge.Core.BP.Infrastructure.Extensions;
using DodeeEdge.Core.BP.Infrastructure.Repositories;
using DodeeEdge.Core.DotNet.Infrastructure;
using DodeeEdge.Core.DotNet.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DodeeEdge.Core.BP.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomConfigurations(this IServiceCollection services, EnvironmentOptions options)
    {
        services.AddDbContextPool<BpDbContext>(opt =>
            opt.UseNpgsql(options.POSTGRES_CONNECTIONSTRING, sql =>
            {
                sql.MigrationsAssembly(typeof(BpDbContext).GetTypeInfo().Assembly.GetName().Name);
                sql.EnableRetryOnFailure(10, TimeSpan.FromMinutes(10), null);
            }));

        services.AddScoped<DbContext>(m => m.GetService<BpDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddCustomHttpClients(options);

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerLinkedIdentityRepository, CustomerLinkedIdentityRepository>();
        services.AddScoped<ICustomerAuditLogRepository, CustomerAuditLogRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();

        // Services
        services.AddScoped<ICustomerService>(m => new CustomerService(
            m.GetRequiredService<IUnitOfWork>(),
            m.GetRequiredService<ICustomerRepository>(),
            m.GetRequiredService<ICustomerLinkedIdentityRepository>(),
            m.GetRequiredService<ICustomerAuditLogRepository>(),
            m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CustomerService>>()));

        services.AddScoped<IOrganizationService>(m => new OrganizationService(
            m.GetRequiredService<IUnitOfWork>(),
            m.GetRequiredService<IOrganizationRepository>(),
            m.GetRequiredService<IOrganizationMemberRepository>(),
            m.GetRequiredService<ICustomerRepository>(),
            m.GetRequiredService<ICustomerAuditLogRepository>(),
            m.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OrganizationService>>()));

        return services;
    }
}
```

### src/API/Controllers/CustomerSyncController.cs (Internal — called by core-auth-api)

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.CustomerAggregate;
using DodeeEdge.Core.BP.Domain.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DodeeEdge.Core.BP.API.Controllers;

// Protected by X-Internal-Api-Key middleware in Program.cs
[Route("api/customers")]
[ApiController]
public class CustomerSyncController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerSyncController(ICustomerService customerService)
        => _customerService = customerService;

    // POST /api/customers/ensure
    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure([FromBody] EnsureCustomerRequest request)
    {
        var result = await _customerService.EnsureAsync(request);
        return Ok(result);
    }

    // POST /api/customers/link-identity
    [HttpPost("link-identity")]
    public async Task<IActionResult> LinkIdentity([FromBody] LinkIdentityRequest request)
    {
        await _customerService.LinkIdentityAsync(request);
        return Ok();
    }
}
```

### src/API/Controllers/CustomerController.cs (Public — requires JWT)

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.CustomerAggregate;
using DodeeEdge.Core.BP.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DodeeEdge.Core.BP.API.Controllers;

[Route("api/customers")]
[ApiController]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
        => _customerService = customerService;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException());

    // GET /api/customers/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var profile = await _customerService.GetByUserIdAsync(CurrentUserId);
        return Ok(profile);
    }

    // PUT /api/customers/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var profile = await _customerService.UpdateAsync(CurrentUserId, request);
        return Ok(profile);
    }

    // DELETE /api/customers/me
    [HttpDelete("me")]
    public async Task<IActionResult> RequestDeletion()
    {
        await _customerService.RequestDeletionAsync(CurrentUserId);
        return Ok(new { message = "Deletion request submitted." });
    }
}
```

### src/API/Controllers/OrganizationController.cs

```csharp
using DodeeEdge.Core.BP.Domain.AggregatesModel.OrganizationAggregate;
using DodeeEdge.Core.BP.Domain.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DodeeEdge.Core.BP.API.Controllers;

[Route("api/organizations")]
[ApiController]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _orgService;

    public OrganizationController(IOrganizationService orgService) => _orgService = orgService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _orgService.GetByIdAsync(id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request)
        => Ok(await _orgService.CreateAsync(CurrentUserId, request));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        await _orgService.UpdateAsync(id, CurrentUserId, request);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _orgService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
        => Ok(await _orgService.GetMembersAsync(id));

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        await _orgService.AddMemberAsync(id, CurrentUserId, request.CustomerId, request.Role ?? "MEMBER");
        return Ok();
    }

    [HttpDelete("{id}/members/{customerId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid customerId)
    {
        await _orgService.RemoveMemberAsync(id, CurrentUserId, customerId);
        return NoContent();
    }
}

public class AddMemberRequest { public Guid CustomerId { get; set; } public string? Role { get; set; } }
```

### src/API/Program.cs

```csharp
using DodeeEdge.Core.BP.API.Extensions;
using DodeeEdge.Core.BP.Infrastructure.Configurations;
using DodeeEdge.Core.BP.Infrastructure.DbContexts;
using DodeeEdge.Core.DotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var environmentOptions = builder.Services.AddCustomOptions<EnvironmentOptions>(builder.Configuration);

builder.Services.AddCustomConfigurations(environmentOptions);
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// JWT validation against core-auth-api JWKS
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = environmentOptions.AUTH_ISSUER;
        options.MetadataAddress = environmentOptions.AUTH_ISSUER + "/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = environmentOptions.AUTH_ISSUER,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireSignedTokens = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddPolicy("BPCors", policy =>
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
    setup.SwaggerDoc("v1", new OpenApiInfo { Title = "BP API", Version = "v1" }));

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BpDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseHttpsRedirection();
app.UseCors("BPCors");

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Internal API Key middleware — protects /api/customers/ensure and /api/customers/link-identity
app.Use(async (ctx, next) =>
{
    var internalPaths = new[] { "/api/customers/ensure", "/api/customers/link-identity" };
    if (internalPaths.Any(p => ctx.Request.Path.StartsWithSegments(p)))
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

app.UseAuthentication();
app.UseAuthorization();
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
  "POSTGRES_CONNECTIONSTRING": "Host=192.168.1.99; Port=5432; User ID=postgres; Password=1234; Database=bp-dev; Pooling=true; Connection Idle Lifetime=10; Minimum Pool Size=2; Maximum Pool Size=50;",
  "REDIS_CONNECTIONSTRING": "localhost:6379",
  "INTERNAL_API_KEY": "change-me-in-production",
  "AUTH_ISSUER": "https://auth.yourdomain.com",
  "AUTH_JWKS_ENDPOINT": "https://auth.yourdomain.com/.well-known/jwks.json",
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": []
  }
}
```

---

## หมายเหตุสำคัญ

1. `BaseEntity`, `IRepository<T>`, `BaseRepository<TContext, TEntity>`, `IUnitOfWork`, `UnitOfWork` มาจาก NuGet package `DodeeEdge.Core.DotNet.Infrastructure` — เพิ่ม reference เดียวกับ core-auth-api
2. `AddCustomOptions<T>()` และ `ISoftDelete` มาจาก `DodeeEdge.Core.DotNet` package
3. สร้าง `OrganizationService.cs` ให้ implement `IOrganizationService` ครบทุก method (create, update, delete, member management) พร้อม ownership check (ต้องเป็น OWNER ถึงจะ delete ได้)
4. Entity `Customer` ต้องมี navigation property `LinkedIdentities` และ `Organization` ต้องมี `Members` + `Addresses`
5. Build ต้องผ่าน 0 errors ก่อน commit

This service manages **customer profiles, organizations, and business data**. It does **not** perform authentication — it trusts JWT tokens issued by `core-auth-api` and uses the `sub` claim (userId) as the identity key.

---

## SYSTEM CONTEXT

| Service            | Purpose                                                                                         |
| ------------------ | ----------------------------------------------------------------------------------------------- |
| `core-auth-api`    | OAuth 2.1 Identity Server — calls this service to sync customer records after user registration |
| `notification-api` | Notification service — may be called by this service for business events                        |

### Integration Flow

```
User registers in core-auth-api
    → core-auth-api calls POST /api/customers/ensure (user identity sync)
    → bp-api creates a Customer record linked to userId

User logs in via social provider
    → core-auth-api calls POST /api/customers/link-identity
    → bp-api maps provider ID to existing customer

Client app calls bp-api with Bearer JWT
    → bp-api validates JWT against core-auth-api JWKS
    → bp-api returns profile for the authenticated user
```

---

## TECHNOLOGY STACK

| Layer        | Technology                                                       |
| ------------ | ---------------------------------------------------------------- |
| Framework    | .NET 9 / ASP.NET Core                                            |
| Architecture | Clean Architecture (Domain → Application → Infrastructure → API) |
| Database     | PostgreSQL + EF Core                                             |
| Cache        | Redis                                                            |
| Auth         | Bearer JWT (validated against `core-auth-api` JWKS)              |
| File Storage | AWS S3 / local (for logo, profile images)                        |
| Queue        | RabbitMQ (for async events)                                      |

---

## API ENDPOINTS

### Customer (Identity Sync — called by core-auth-api, internal)

```
POST /api/customers/ensure          -- create or update customer by userId
POST /api/customers/link-identity   -- link social provider to customer
```

### Customer Profile (called by client apps, requires JWT)

```
GET    /api/customers/me                       -- get own profile
PUT    /api/customers/me                       -- update own profile
GET    /api/customers/me/organizations         -- list own organizations
DELETE /api/customers/me                       -- request account deletion
```

### Organization

```
GET    /api/organizations/{id}                 -- get organization
POST   /api/organizations                      -- create organization
PUT    /api/organizations/{id}                 -- update organization
DELETE /api/organizations/{id}                 -- soft-delete
POST   /api/organizations/{id}/members         -- add member
DELETE /api/organizations/{id}/members/{userId}-- remove member
GET    /api/organizations/{id}/members         -- list members
```

### Admin (requires admin scope)

```
GET    /api/admin/customers?page=1&limit=20&search=...
GET    /api/admin/customers/{id}
PUT    /api/admin/customers/{id}/status        -- ACTIVE | SUSPENDED | DELETED
GET    /api/admin/organizations
GET    /api/admin/organizations/{id}
```

### Health

```
GET /health/ready
GET /health/live
```

---

## REQUEST / RESPONSE CONTRACTS

### POST /api/customers/ensure

Called by `core-auth-api` after new user registration or social login.

Request:

```json
{
  "userId": "uuid-from-auth-server",
  "email": "user@example.com",
  "displayName": "John Doe",
  "phone": "+6681234567",
  "locale": "th"
}
```

Response:

```json
{
  "customerId": "uuid",
  "isNew": true
}
```

### POST /api/customers/link-identity

Called by `core-auth-api` after social login account linking.

Request:

```json
{
  "userId": "uuid-from-auth-server",
  "providerCode": "GOOGLE",
  "externalUserId": "google-sub-value",
  "externalEmail": "user@gmail.com"
}
```

### GET /api/customers/me

Response:

```json
{
  "customerId": "uuid",
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "John Doe",
  "phone": "+6681234567",
  "avatarUrl": "https://...",
  "locale": "th",
  "timezone": "Asia/Bangkok",
  "status": "ACTIVE",
  "linkedProviders": ["GOOGLE", "FACEBOOK"],
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### POST /api/organizations

```json
{
  "organizationName": "ACME Corp",
  "taxId": "0123456789012",
  "businessType": "COMPANY",
  "address": {
    "line1": "123 Main St",
    "city": "Bangkok",
    "country": "TH",
    "postalCode": "10110"
  },
  "phone": "+6621234567",
  "email": "contact@acme.com",
  "logoUrl": "https://..."
}
```

---

## DATABASE SCHEMA

### Table: `customers`

```sql
CREATE TABLE customers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL UNIQUE,      -- auth server userId
    email           VARCHAR(320) NOT NULL,
    display_name    VARCHAR(255),
    phone           VARCHAR(30),
    avatar_url      VARCHAR(500),
    locale          VARCHAR(10) DEFAULT 'en',
    timezone        VARCHAR(50) DEFAULT 'UTC',
    status          VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',  -- ACTIVE | SUSPENDED | DELETED
    deleted_at      TIMESTAMPTZ,
    metadata        JSONB,                     -- extensible key-value for custom fields
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_customers_user_id (user_id),
    INDEX idx_customers_email (email),
    INDEX idx_customers_status (status) WHERE deleted_at IS NULL
);
```

### Table: `customer_linked_identities`

```sql
CREATE TABLE customer_linked_identities (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID NOT NULL REFERENCES customers(id),
    provider_code   VARCHAR(30) NOT NULL,      -- GOOGLE | FACEBOOK | APPLE | LINE | MICROSOFT
    external_user_id VARCHAR(255) NOT NULL,
    external_email  VARCHAR(320),
    linked_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_at   TIMESTAMPTZ,

    UNIQUE (customer_id, provider_code),
    INDEX idx_linked_identities_external (provider_code, external_user_id)
);
```

### Table: `organizations`

```sql
CREATE TABLE organizations (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_name   VARCHAR(255) NOT NULL,
    organization_slug   VARCHAR(100) UNIQUE,   -- URL-safe identifier
    tax_id              VARCHAR(50),
    business_type       VARCHAR(50),           -- COMPANY | INDIVIDUAL | NGO
    logo_url            VARCHAR(500),
    website             VARCHAR(500),
    email               VARCHAR(320),
    phone               VARCHAR(30),
    status              VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    deleted_at          TIMESTAMPTZ,
    metadata            JSONB,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Table: `organization_addresses`

```sql
CREATE TABLE organization_addresses (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    address_type    VARCHAR(20) DEFAULT 'MAIN',   -- MAIN | BILLING | SHIPPING
    line1           VARCHAR(255),
    line2           VARCHAR(255),
    city            VARCHAR(100),
    state           VARCHAR(100),
    country         CHAR(2),                      -- ISO 3166-1 alpha-2
    postal_code     VARCHAR(20),
    is_primary      BOOLEAN DEFAULT true
);
```

### Table: `organization_members`

```sql
CREATE TABLE organization_members (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    customer_id     UUID NOT NULL REFERENCES customers(id),
    role            VARCHAR(50) NOT NULL DEFAULT 'MEMBER',  -- OWNER | ADMIN | MEMBER
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    left_at         TIMESTAMPTZ,

    UNIQUE (organization_id, customer_id),
    INDEX idx_org_members_customer (customer_id) WHERE left_at IS NULL
);
```

### Table: `customer_audit_logs`

```sql
CREATE TABLE customer_audit_logs (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID REFERENCES customers(id),
    action          VARCHAR(100) NOT NULL,     -- PROFILE_UPDATED | ORG_CREATED | STATUS_CHANGED
    resource_type   VARCHAR(50),
    resource_id     UUID,
    performed_by    UUID,                      -- userId who performed the action
    ip_address      VARCHAR(45),
    details         JSONB,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_audit_customer (customer_id, created_at DESC)
);
```

---

## DOMAIN LAYER

### Entities

```csharp
public class Customer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }           // auth server identity key
    public string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; } = "en";
    public string Timezone { get; set; } = "UTC";
    public string Status { get; set; } = "ACTIVE";
    public DateTime? DeletedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<CustomerLinkedIdentity> LinkedIdentities { get; set; } = new();
    public List<OrganizationMember> Organizations { get; set; } = new();
}

public class CustomerLinkedIdentity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string ProviderCode { get; set; }
    public string ExternalUserId { get; set; }
    public string? ExternalEmail { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}

public class Organization
{
    public Guid Id { get; set; }
    public string OrganizationName { get; set; }
    public string? OrganizationSlug { get; set; }
    public string? TaxId { get; set; }
    public string? BusinessType { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrganizationAddress> Addresses { get; set; } = new();
    public List<OrganizationMember> Members { get; set; } = new();
}
```

### Service Interfaces

```csharp
public interface ICustomerService
{
    Task<CustomerResult> EnsureAsync(EnsureCustomerRequest request);
    Task LinkIdentityAsync(LinkIdentityRequest request);
    Task<CustomerProfile> GetByUserIdAsync(Guid userId);
    Task<CustomerProfile> UpdateAsync(Guid userId, UpdateProfileRequest request);
    Task RequestDeletionAsync(Guid userId);
}

public interface IOrganizationService
{
    Task<OrganizationResult> CreateAsync(Guid ownerUserId, CreateOrganizationRequest request);
    Task<OrganizationResult> GetByIdAsync(Guid id);
    Task UpdateAsync(Guid id, Guid requestingUserId, UpdateOrganizationRequest request);
    Task DeleteAsync(Guid id, Guid requestingUserId);
    Task AddMemberAsync(Guid organizationId, Guid requestingUserId, Guid targetUserId, string role);
    Task RemoveMemberAsync(Guid organizationId, Guid requestingUserId, Guid targetUserId);
    Task<List<MemberResult>> GetMembersAsync(Guid organizationId);
}
```

---

## SECURITY

1. **JWT Validation**: Validate RS256 JWT from `core-auth-api`:
   - Fetch JWKS from `AUTH_JWKS_ENDPOINT`
   - Validate `iss`, `aud`, `exp`, `iat`
   - Extract `sub` as the current user's identity key

2. **Internal Endpoints**: `POST /api/customers/ensure` and `POST /api/customers/link-identity` must be protected with an **Internal API Key** header (not a user JWT). Only `core-auth-api` may call these.

3. **Ownership Enforcement**: A customer may only update their own profile. An organization OWNER or ADMIN can manage members.

4. **Input Validation**: Tax ID format per country, E.164 phone, ISO country code.

5. **Soft Delete Only**: Never hard-delete customer records. Set `deleted_at` and `status = DELETED`.

---

## JWT VALIDATION SETUP

```csharp
// In Program.cs:
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AUTH_ISSUER"];
        options.MetadataAddress = builder.Configuration["AUTH_JWKS_ENDPOINT"]
            .Replace("/jwks.json", "/.well-known/openid-configuration");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AUTH_ISSUER"],
            ValidateAudience = false,   // bp-api uses sub claim, not aud
            ValidateLifetime = true,
            RequireSignedTokens = true
        };
    });
```

---

## ENVIRONMENT VARIABLES

```env
POSTGRES_CONNECTIONSTRING=Host=...;Database=bp-db;
REDIS_CONNECTIONSTRING=localhost:6379
AUTH_ISSUER=https://auth.yourdomain.com
AUTH_JWKS_ENDPOINT=https://auth.yourdomain.com/.well-known/jwks.json
INTERNAL_API_KEY=your-internal-service-key-shared-with-core-auth-api
RABBITMQ_HOST=localhost
AWS_S3_BUCKET=bp-api-uploads
AWS_REGION=ap-southeast-1
```

---

## PROJECT STRUCTURE

```
bp-api/
├── DodeeEdge.Core.BP.sln
└── src/
    ├── API/
    │   ├── API.csproj
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── Controllers/
    │   │   ├── CustomerController.cs        -- /api/customers/me
    │   │   ├── CustomerSyncController.cs    -- /api/customers/ensure, link-identity (internal)
    │   │   ├── OrganizationController.cs    -- /api/organizations
    │   │   └── AdminController.cs           -- /api/admin/*
    │   ├── Middleware/
    │   │   └── InternalApiKeyMiddleware.cs  -- protects internal endpoints
    │   └── Extensions/
    │       └── ServiceCollectionExtensions.cs
    ├── Domain/
    │   ├── Domain.csproj
    │   ├── Entities/
    │   │   ├── Customer.cs
    │   │   ├── CustomerLinkedIdentity.cs
    │   │   ├── Organization.cs
    │   │   ├── OrganizationAddress.cs
    │   │   ├── OrganizationMember.cs
    │   │   └── CustomerAuditLog.cs
    │   └── Services/Interface/
    │       ├── ICustomerService.cs
    │       └── IOrganizationService.cs
    ├── Application/
    │   ├── Application.csproj
    │   └── Services/
    │       ├── CustomerService.cs
    │       └── OrganizationService.cs
    └── Infrastructure/
        ├── Infrastructure.csproj
        ├── DbContexts/
        │   └── BpDbContext.cs
        └── Repositories/
            ├── CustomerRepository.cs
            └── OrganizationRepository.cs
```

---

## DELIVERABLES

1. Full working ASP.NET Core 9 project with the structure above
2. EF Core migrations for all 5 tables
3. `CustomerService` and `OrganizationService` fully implemented
4. All 4 controllers with full DI wiring
5. JWT validation wired in `Program.cs` using JWKS from `core-auth-api`
6. `InternalApiKeyMiddleware` protecting `/api/customers/ensure` and `/api/customers/link-identity`
7. Ownership/authorization checks in all organization mutation endpoints
8. Pagination on all list endpoints (page + limit)
9. Soft-delete pattern on Customer and Organization
10. Audit logging on all mutation operations
11. `appsettings.json` with all environment variable bindings
12. Build must succeed with 0 errors
