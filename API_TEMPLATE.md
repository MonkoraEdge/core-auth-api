# API Template For New Projects

เอกสารนี้เป็น template สำหรับตั้งต้น API ใหม่ โดยอิงแนวทางจากโปรเจคนี้: `API / Domain / Infrastructure` และคงหลัก separation of concerns แบบที่ใช้อยู่ใน `core-auth-api`

---

## Goals

- ให้โปรเจคใหม่เริ่มได้เร็ว แต่ไม่ผูกกับ shared layer เกินจำเป็น
- ให้ Domain เป็นเจ้าของ use case, contracts, และ business rules
- ให้ Infrastructure รับผิดชอบ persistence, external services, crypto, background jobs
- ให้ API layer เป็นแค่ transport layer

---

## Recommended First-Day Workflow

1. ตั้งชื่อ solution, assembly name, root namespace ให้ตรงกับ bounded context ของระบบ
2. สร้าง 3 projects หลัก: `API`, `Domain`, `Infrastructure`
3. วาง dependency direction ให้ถูกตั้งแต่วันแรก
4. สร้าง `README.md` พร้อม architecture summary, config keys, run commands
5. สร้าง health checks, error handling, Swagger, authentication skeleton ให้ครบก่อนเริ่มแตก feature
6. ค่อยเริ่ม feature แรกโดยสร้าง aggregate, repository contract, service interface, และ controller แบบบาง
7. ก่อนเปิด PR แรก ต้องให้ build ผ่าน, config ไม่มี secret, และ README ตรงกับโค้ด

---

## Recommended Solution Structure

```text
YourCompany.YourProduct.YourApi.sln
README.md
src/
  API/
    API.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    Controllers/
    Controllers/Models/
    Extensions/
    Properties/
  Domain/
    Domain.csproj
    AggregatesModel/
      <Feature>Aggregate/
        Interfaces/
      EntityAggregate/
    Repositories/
      IRepository.cs
    Services/
      Interface/
    Validations/
  Infrastructure/
    Infrastructure.csproj
    Configurations/
    DbContexts/
    ExternalApis/
    Repositories/
    Services/
    Extensions/
```

---

## Layer Rules

### API

- รับ request, validate transport model, map เป็น Domain request
- เรียก Domain service หรือ application orchestration เท่านั้น
- ไม่ใส่ business rule, SQL, token generation, hashing, หรือ workflow สำคัญไว้ใน controller

### Domain

- เก็บ entities, repository contracts, service contracts, use cases, and domain rules
- Domain ควรเป็นเจ้าของ abstractions ที่ตัวเองใช้ เช่น `IUnitOfWork`, `IRepository<TEntity>`, service interfaces
- หลีกเลี่ยงการผูกกับ HTTP concern โดยตรง
- หลีกเลี่ยงการผูกกับ EF Core implementation โดยตรง

### Infrastructure

- implement repository contracts ของ Domain
- implement crypto, token, hashing, outbound API clients, file storage, mail, sms
- contain EF Core `DbContext`, mappings, migrations, background services
- เป็นจุดที่ map Domain abstractions ไปยัง implementation จริงผ่าน DI

---

## Bootstrap Checklist

1. สร้าง solution และ 3 projects: `API`, `Domain`, `Infrastructure`
2. ให้ `Infrastructure` reference `Domain`
3. ให้ `API` reference `Infrastructure`
4. อย่าให้ `Domain` reference `Infrastructure`
5. เก็บ repository contracts และ unit-of-work contract ไว้ใน `Domain`
6. เก็บ repository implementations และ external service implementations ไว้ใน `Infrastructure`
7. ลงทะเบียน DI ทั้งหมดที่ composition root ใน `API`
8. เขียน `README.md` ให้เป็น source of truth ตั้งแต่วันแรก

---

## Project Naming Convention

- Solution: `YourCompany.YourProduct.YourApi.sln`
- API project: `YourCompany.YourProduct.API`
- Domain project: `YourCompany.YourProduct.Domain`
- Infrastructure project: `YourCompany.YourProduct.Infrastructure`
- Namespace root ควรตรงกับ assembly name เพื่อให้ search และ refactor ง่าย

ตัวอย่าง:

```text
MonkoraEdge.Payments.sln
src/
  API/             -> MonkoraEdge.Payments.API
  Domain/          -> MonkoraEdge.Payments.Domain
  Infrastructure/  -> MonkoraEdge.Payments.Infrastructure
```

---

## Suggested Folder Pattern By Feature

```text
Domain/
  AggregatesModel/
    CustomerAggregate/
      Interfaces/
        ICustomerRepository.cs
      CustomerModels.cs
    OrderAggregate/
      Interfaces/
        IOrderRepository.cs
      OrderModels.cs
    EntityAggregate/
      Customer.cs
      Order.cs
  Repositories/
    IRepository.cs
  Services/
    Interface/
      ICustomerService.cs
      IOrderService.cs
    CustomerService.cs
    OrderService.cs
```

---

## DI Composition Pattern

```csharp
services.AddScoped<DbContext>(m => m.GetRequiredService<AppDbContext>());

services.AddScoped<UnitOfWork>();
services.AddScoped<DomainIUnitOfWork, DomainUnitOfWorkAdapter>();

services.AddScoped<ICustomerRepository, CustomerRepository>();
services.AddScoped<IOrderRepository, OrderRepository>();

services.AddScoped<ICustomerService, CustomerService>();
services.AddScoped<IOrderService, OrderService>();
```

หลักคือ `API` เป็น composition root และเป็นที่เดียวที่รู้ว่า implementation จริงคืออะไร

---

## Recommended API Bootstrap Snippet

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddCustomConfigurations(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();
```

สิ่งที่ควรมีตั้งแต่รอบแรก:

- error handling middleware
- correlation id หรือ trace id
- health endpoints
- Swagger/OpenAPI
- configuration binding
- database connection registration
- authentication skeleton ถ้าระบบมี auth ตั้งแต่ต้น

---

## Coding Rules For New APIs

- Controller ต้องบาง
- Business orchestration ให้อยู่ใน Domain services
- อย่าให้ Domain เรียก `HttpContext`, `ControllerBase`, `IActionResult`, หรือ EF-specific API โดยตรง
- อย่าใช้ generic shared repository เป็น default ถ้ายังไม่แน่ใจว่าจำเป็นจริง
- ถ้าต้องมี shared library ให้แชร์เฉพาะของที่ stable และ cross-cutting จริง
- config สำคัญต้องตั้งชื่อชัด เช่น issuer, audience, key source, connection string, external endpoints

---

## Configuration Checklist

ควรตั้ง key ให้ชัดและแยก concern ตั้งแต่แรก เช่น:

```json
{
  "ConnectionStrings": {
    "Default": "..."
  },
  "Cors": {
    "AllowedOrigins": ["https://app.example.com"]
  },
  "Auth": {
    "Issuer": "https://auth.example.com",
    "Audience": "payments-api"
  },
  "Redis": {
    "ConnectionString": "..."
  },
  "ExternalApis": {
    "NotificationBaseUrl": "https://notification.example.com"
  }
}
```

หลักการ:

- อย่าตั้งชื่อกว้างเกินไป เช่น `URL`, `KEY`, `ENDPOINT`
- secret ให้ใช้ environment variables, secret store, หรือ pipeline secret เท่านั้น
- appsettings ควรเป็น safe defaults ไม่ใช่ production secret

---

## Environment Variables Guide

ตัวอย่างค่าที่โปรเจคใหม่มักต้องมี:

| Key                                 | Purpose                                                  |
| ----------------------------------- | -------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`            | ระบุ environment เช่น Development / Staging / Production |
| `CONNECTIONSTRINGS__DEFAULT`        | database connection string                               |
| `AUTH__ISSUER`                      | token issuer                                             |
| `AUTH__AUDIENCE`                    | token audience                                           |
| `REDIS__CONNECTIONSTRING`           | redis connection string                                  |
| `EXTERNALAPIS__NOTIFICATIONBASEURL` | notification service base URL                            |
| `SERILOG__MINIMUMLEVEL__DEFAULT`    | log level                                                |

ถ้ามี OAuth/OIDC เพิ่มเติม:

| Key                                | Purpose                      |
| ---------------------------------- | ---------------------------- |
| `AUTH__JWKSENDPOINT`               | public key metadata endpoint |
| `AUTH__SIGNEDPRIVATEKEY`           | signing key source           |
| `AUTH__ACCESSTOKENLIFETIMESECONDS` | token lifetime               |

---

## Database And Migration Flow

แนะนำให้ตกลงแนวทางตั้งแต่ต้นว่าจะใช้:

- auto-migrate ตอน startup เฉพาะ development/staging
- migration via CI/CD สำหรับ production

คำสั่งพื้นฐาน:

```powershell
dotnet restore
dotnet build
dotnet ef migrations add InitialCreate --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj
dotnet run --project src/API/API.csproj
```

ข้อแนะนำ:

- ให้ migration assembly อยู่ Infrastructure
- startup project เป็น API
- อย่าปล่อยให้ production database update อัตโนมัติโดยไม่มี policy ชัดเจน

---

## Observability Baseline

ควรมีอย่างน้อย:

1. structured logging
2. request trace id / correlation id
3. health endpoints
4. audit logs สำหรับ action สำคัญ
5. error response ที่สม่ำเสมอ

ถ้าระบบโตขึ้น ควรเพิ่ม:

1. metrics เช่น request duration, error rate, auth failures
2. distributed tracing
3. dashboard สำหรับ application health และ background jobs

---

## Authentication Strategy Matrix

เลือกให้ชัดว่า API ใหม่อยู่กลุ่มไหน:

| Scenario                | Recommended Approach                     |
| ----------------------- | ---------------------------------------- |
| Internal service API    | JWT bearer with fixed audience           |
| User-facing API         | OAuth2/OIDC with access token validation |
| Machine-to-machine      | client credentials flow                  |
| Admin console API       | JWT + role/permission policy             |
| Public webhook endpoint | signature verification or mTLS           |

ถ้า API ใหม่ไม่ใช่ auth server เอง:

- อย่า embed token issuing logic ใน API นั้น
- ให้ validate token และ enforce authorization policy เท่านั้น

---

## Testing Baseline

ควรมีอย่างน้อย 3 ระดับ:

1. domain/service tests
2. repository or integration tests สำหรับ query สำคัญ
3. API smoke tests สำหรับ endpoint หลัก

แนวปฏิบัติ:

- test business rule ที่ Domain ก่อน
- test mapping/query ที่ Infrastructure แยกจาก controller
- controller tests เอาไว้เช็ค HTTP contract มากกว่า business logic

ตัวอย่างขั้นต่ำก่อน merge feature แรก:

1. health endpoint works
2. authentication/authorization happy path works
3. invalid request returns expected error schema
4. core repository query returns expected data

---

## CI/CD Minimum Checklist

Pipeline ขั้นต่ำควรมี:

1. restore
2. build
3. test
4. optional lint/analyzer step
5. package artifact หรือ docker image
6. deploy step แยกตาม environment

ก่อน deploy production:

1. config ถูก inject จาก secret store
2. migration strategy ถูกยืนยัน
3. health check route ถูก monitor
4. rollback plan ถูกกำหนด

---

## Release Checklist

1. build และ tests ผ่าน
2. migration reviewed แล้ว
3. environment variables ครบทุก environment
4. logging และ alerts ครบสำหรับ feature ใหม่
5. breaking changes ถูกบันทึกใน README หรือ release note
6. API contract changes ถูกแจ้ง consumer แล้ว

---

## Feature Delivery Checklist

ทุก feature ใหม่ควรมีอย่างน้อย:

1. request/response contract
2. domain service/use case
3. repository contract ถ้าต้องอ่านเขียนข้อมูล
4. infrastructure implementation
5. DI registration
6. error handling path
7. tests ระดับที่เหมาะสม
8. README update ถ้ามี external behavior เปลี่ยน

---

## Security Baseline

- แยก secret ออกจาก source control
- ใช้ exact redirect URI matching ถ้ามี OAuth
- บังคับ PKCE `S256` ถ้ามี authorization code flow
- แยก issuer ออกจาก JWKS/public metadata endpoint
- log security events ที่สำคัญ เช่น failed login, refresh reuse, invalid client, permission change
- กำหนด policy สำหรับ token rotation, revocation, and cleanup ตั้งแต่แรก

---

## README Minimum Content

README ของ API ใหม่ควรมีอย่างน้อย:

1. ภาพรวม architecture
2. project structure
3. dependency direction
4. environment variables / config keys สำคัญ
5. endpoint groups สำคัญ
6. auth/security behavior
7. วิธี run, migrate, build, และ test
8. workflow rule ถ้ามีข้อบังคับพิเศษใน repo

---

## First PR Checklist

1. solution build ผ่าน
2. dependency direction ถูกต้อง
3. README ตรงกับ code
4. config ไม่มี secret hardcoded
5. controller ไม่มี business logic หลัก
6. repository contracts อยู่ใน Domain
7. implementations อยู่ใน Infrastructure
8. health endpoint และ error handling พร้อมใช้งาน

---

## Copy-Ready README Skeleton

````md
# Your API Name

## Overview

- Purpose:
- Framework:
- Architecture:

## Project Structure

- API
- Domain
- Infrastructure

## Configuration

- Required environment variables:
- appsettings notes:

## Run

```powershell
dotnet restore
dotnet build
dotnet run --project src/API/API.csproj
```
````

## Database

```powershell
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/API/API.csproj
```

## Security

- Authentication approach:
- Authorization approach:
- Secret handling:

## Operational Notes

- Health endpoints:
- Logging:
- Known constraints:

```

---

## Notes From This Project

- ถ้าจะใช้ shared layer ให้ระวังอย่าให้ Domain ผูกกับ shared infrastructure abstractions มากเกินไป
- ถ้ามี compatibility endpoint เก่า ให้ทำเป็น facade ที่ส่งต่อเข้ flow หลัก แทนการ duplicate logic
- crypto/token/password implementations ควรอยู่ Infrastructure ไม่ใช่ Domain
- refactor แบบ incremental จะปลอดภัยกว่าการ rewrite ทั้งระบบ
- ถ้าทำ template สำหรับโปรเจคใหม่ ให้เตรียม onboarding checklist และ runbook ไว้ใน repo ตั้งแต่วันแรก จะช่วยลด drift ภายหลัง
```
