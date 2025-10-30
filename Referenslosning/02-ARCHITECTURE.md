# Arkitektur Deep-Dive

**Syfte:** Detta dokument går på djupet i de tekniska design-besluten i referenslösningen. Det är menat som en teknisk grund för diskussioner, inte som "The One True Way".

---

## Översikt

Denna lösning implementerar Clean Architecture med fyra lager:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                    (ClaimApp.Web)                       │
│              Blazor Components + Program.cs             │
└─────────────────────────────────────────────────────────┘
                           ↓ depends on
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│                (ClaimApp.Application)                   │
│          Services, Interfaces, Business Logic           │
└─────────────────────────────────────────────────────────┘
                           ↓ depends on
┌─────────────────────────────────────────────────────────┐
│                     Domain Layer                         │
│                  (ClaimApp.Domain)                      │
│       Entities, Value Objects, Domain Services          │
└─────────────────────────────────────────────────────────┘
                           ↑ implemented by
┌─────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                     │
│              (ClaimApp.Infrastructure)                  │
│           Repositories, External Services               │
└─────────────────────────────────────────────────────────┘
```

**Dependency Rule:** Inre lager känner ALDRIG till yttre lager. Dependencies pekar alltid inåt.

---

## Domain Layer - Design Rationale

### 1. Abstract Base Class: `Claim`

**Design:**

```csharp
public abstract class Claim
{
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public DateTime ReportedDate { get; private set; }
    public ClaimStatus Status { get; private set; }
    public ClaimType ClaimType { get; protected set; }

    protected Claim(string description, DateTime reportedDate)
    {
        Id = Guid.NewGuid();
        SetDescription(description);
        ReportedDate = reportedDate;
        Status = ClaimStatus.Pending;
    }

    private void SetDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 20)
            throw new ArgumentException("Description måste vara minst 20 tecken");

        Description = value;
    }

    public void UpdateStatus(ClaimStatus newStatus)
    {
        Status = newStatus;
    }
}
```

**Tekniska beslut:**

1. **Private setters** - Encapsulation. Förhindrar invalid state:

   ```csharp
   // ❌ Går inte
   claim.Id = Guid.NewGuid();

   // ✅ Måste gå via constructor eller method
   var claim = new VehicleClaim(...);
   ```

2. **Protected set för ClaimType** - Subklasser måste kunna sätta sin typ:

   ```csharp
   public class VehicleClaim : Claim
   {
       public VehicleClaim(...) : base(...)
       {
           ClaimType = ClaimType.Vehicle; // Måste vara 'protected set'
       }
   }
   ```

3. **Validation i constructor/setter** - Domain integrity säkerställs redan vid skapande:

   ```csharp
   // Går inte att skapa invalid claim
   var claim = new VehicleClaim("kort"); // ❌ Exception!
   ```

4. **Abstract class, inte interface** - Vi har gemensam implementation (shared state):

   ```csharp
   // Med abstract class: Code reuse
   public abstract class Claim { ... }

   // Med interface: Måste duplicera
   public interface IClaim { ... }
   public class VehicleClaim : IClaim { /* all fields duplicated */ }
   ```

**Trade-offs:**

| Aspekt              | Fördel                       | Nackdel                                     |
| ------------------- | ---------------------------- | ------------------------------------------- |
| Abstract class      | Code reuse, tvingar struktur | Kan bara ärva från EN klass                 |
| Private setters     | Immutability, säkerhet       | Mer kod (behöver methods för ändringar)     |
| Validation i domain | Early failure, garanti       | Exceptions kan vara "dyra", behöver catchas |

---

### 2. Value Objects: `RegistrationNumber`

**Design:**

```csharp
public sealed class RegistrationNumber
{
    private readonly string _value;

    public RegistrationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Registreringsnummer får inte vara tomt");

        var normalized = value.Replace(" ", "").Replace("-", "").ToUpper();

        if (!IsValidSwedishFormat(normalized))
            throw new ArgumentException($"Ogiltigt svenskt registreringsnummer: {value}");

        _value = normalized;
    }

    private static bool IsValidSwedishFormat(string value)
    {
        // ABC123 eller ABC12D format
        return Regex.IsMatch(value, @"^[A-Z]{3}\d{2}[A-Z\d]$");
    }

    public string Value => _value;

    // Value object equality
    public override bool Equals(object? obj)
        => obj is RegistrationNumber other && _value == other._value;

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value;
}
```

**Tekniska beslut:**

1. **Sealed class** - Förhindrar arv (value objects ska vara final):

   ```csharp
   // ❌ Går inte
   public class CustomRegistrationNumber : RegistrationNumber { }
   ```

2. **Readonly field** - True immutability:

   ```csharp
   // Går inte ändra efter skapande
   var regNr = new RegistrationNumber("ABC123");
   // regNr._value = "XYZ"; ❌ Readonly field
   ```

3. **Validation + Normalization** - Konsistent format:

   ```csharp
   var r1 = new RegistrationNumber("ABC 123"); // Normaliseras
   var r2 = new RegistrationNumber("abc123");  // Normaliseras
   r1.Equals(r2); // ✅ true - samma värde
   ```

4. **Value equality** - Jämförs på innehåll, inte referens:

   ```csharp
   var r1 = new RegistrationNumber("ABC123");
   var r2 = new RegistrationNumber("ABC123");

   // Reference equality: false (olika objekt)
   ReferenceEquals(r1, r2); // false

   // Value equality: true (samma värde)
   r1.Equals(r2); // true
   ```

**Varför inte bara `string`?**

Jämför:

```csharp
// ❌ Med string - kan vara invalid
public class VehicleClaim : Claim
{
    public string RegistrationNumber { get; set; }
}

// Ingen garanti:
claim.RegistrationNumber = ""; // Valid!
claim.RegistrationNumber = "INVALID"; // Valid!
claim.RegistrationNumber = null; // Valid!

// ✅ Med Value Object - alltid valid
public class VehicleClaim : Claim
{
    public RegistrationNumber RegistrationNumber { get; private set; }
}

// Garanterad korrekthet:
claim.RegistrationNumber = new RegistrationNumber(""); // ❌ Exception
claim.RegistrationNumber = new RegistrationNumber("INVALID"); // ❌ Exception
```

**Trade-offs:**

| Aspekt       | Fördel                                                     | Nackdel                                    |
| ------------ | ---------------------------------------------------------- | ------------------------------------------ |
| Value Object | Garanti för korrekthet, encapsulation                      | Mer kod, lite overhead                     |
| String       | Enkelt, alla känner till                                   | Ingen validation, måste valideras överallt |
| Record       | Kortare syntax (`record RegistrationNumber(string Value)`) | Mindre control över validation             |

**Alternativ approach - Record:**

```csharp
public sealed record RegistrationNumber
{
    public string Value { get; init; }

    public RegistrationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Ogiltigt registreringsnummer");

        Value = value.ToUpper();
    }
}
```

Kortare, men mindre explicit. Diskutera trade-offs!

---

### 3. Domain Service: `ClaimBusinessRules`

**Design:**

```csharp
public class ClaimBusinessRules
{
    // BR1: Sen rapportering (>30 dagar) kräver manuell granskning
    public bool RequiresManualReview(Claim claim)
    {
        if (claim is not VehicleClaim)
            return false;

        return (DateTime.Now - claim.ReportedDate).TotalDays > 30;
    }

    // BR2: Höga belopp (>100k) kräver eskalering
    public bool RequiresEscalation(Claim claim)
    {
        if (claim is PropertyClaim propertyClaim)
            return propertyClaim.EstimatedValue > 100_000;

        return false;
    }

    // BR5: Misstänkt mönster - flera claims på kort tid
    public async Task<bool> IsSuspiciousPattern(
        Claim newClaim,
        IEnumerable<Claim> existingClaims)
    {
        if (newClaim is not VehicleClaim vehicleClaim)
            return false;

        var recentClaims = existingClaims
            .OfType<VehicleClaim>()
            .Where(c => c.RegistrationNumber.Equals(vehicleClaim.RegistrationNumber))
            .Where(c => (DateTime.Now - c.ReportedDate).TotalDays <= 90)
            .Count();

        return recentClaims >= 3; // 3+ claims på 90 dagar = misstänkt
    }
}
```

**Varför Domain Service?**

Vissa affärsregler **involverar flera entities** eller **behöver external state**:

```csharp
// ❌ Går inte på entity - behöver access till ANDRA claims
public class VehicleClaim : Claim
{
    public bool IsSuspiciousPattern()
    {
        // Hur får vi tag i alla andra claims? 🤔
        // Vi vill inte injicera repository i entity!
    }
}

// ✅ Domain Service - kan ta flera entities som input
public class ClaimBusinessRules
{
    public bool IsSuspiciousPattern(Claim newClaim, IEnumerable<Claim> existing)
    {
        // Nu har vi access till alla claims!
    }
}
```

**Varför inte Application Service?**

Domain Service = **domain logic utan external dependencies**:

```csharp
// Domain Service - ren logik, inga I/O operations
public class ClaimBusinessRules
{
    public bool RequiresManualReview(Claim claim) { ... }
}

// Application Service - orchestration + I/O
public class ClaimService
{
    public async Task<Claim> CreateClaim(Claim claim)
    {
        // Använder domain service
        if (_businessRules.RequiresManualReview(claim))
            claim.UpdateStatus(ClaimStatus.ManualReview);

        // Sen gör I/O
        await _repository.Save(claim);
    }
}
```

**Trade-offs:**

| Approach            | Fördel                               | Nackdel                                  |
| ------------------- | ------------------------------------ | ---------------------------------------- |
| Domain Service      | Single place, easy to test, reusable | Extra klass, indirektion                 |
| På Entity           | Nära data, OOP-mindset               | Svårt om man behöver andra entities      |
| Application Service | Pragmatiskt, enkelt                  | Blandar orchestration och business logic |

---

### 4. Enums vs String Constants

**Design:**

```csharp
public enum ClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    RequiresManualReview = 3,
    Escalated = 4
}

public enum ClaimType
{
    Vehicle = 0,
    Property = 1,
    Travel = 2
}
```

**Varför Enums?**

1. **Compile-time safety:**

   ```csharp
   // ❌ Med string
   claim.Status = "Aproved"; // Typo! Runtime error

   // ✅ Med enum
   claim.Status = ClaimStatus.Aproved; // ❌ Kompileringsfel!
   ```

2. **IntelliSense:**

   ```csharp
   claim.Status = ClaimStatus. // IDE visar alla möjliga värden
   ```

3. **Switch exhaustiveness:**
   ```csharp
   switch (claim.ClaimType)
   {
       case ClaimType.Vehicle:
           break;
       case ClaimType.Property:
           break;
       // ⚠️ Warning: ClaimType.Travel not handled
   }
   ```

**Trade-offs:**

| Approach          | Fördel                         | Nackdel                                          |
| ----------------- | ------------------------------ | ------------------------------------------------ |
| Enum              | Type safety, IntelliSense      | Svårt att ändra (serialization), inte extensible |
| String constants  | Flexibelt, JSON-friendly       | Ingen type safety, kan ha typos                  |
| Lookup table (DB) | Runtime ändringar, data-driven | Mer komplext, DB dependency                      |

---

## Application Layer - Orchestration

### 1. Service Interface: `IClaimService`

**Design:**

```csharp
public interface IClaimService
{
    Task<Claim> CreateClaim(Claim claim);
    Task<Claim?> GetClaimById(Guid id);
    Task<IEnumerable<Claim>> GetAllClaims();
    Task<IEnumerable<Claim>> GetClaimsByRegistrationNumber(string registrationNumber);
}
```

**Varför inte fler metoder?**

Denna interface följer **use case-driven design**:

- `CreateClaim` = FR1 (Registrera skadeanmälan)
- `GetAllClaims` = FR2 (Lista skadeanmälningar)
- `GetClaimById` = FR3 (Detaljerad vy)
- `GetClaimsByRegistrationNumber` = Extra query för fordon

**Varför inte generic CRUD?**

```csharp
// ❌ Generic CRUD - inte use case-driven
public interface ICrudService<T>
{
    Task<T> Create(T entity);
    Task<T> Update(T entity);
    Task Delete(Guid id);
    Task<T> Get(Guid id);
    Task<IEnumerable<T>> GetAll();
}

// Problem: Affären pratar inte i "CRUD" - de pratar i use cases!
// "Vi vill registrera en skadeanmälan" != "Vi vill create:a en entity"
```

**Use case-driven design:**

```csharp
// ✅ Tydliga use cases
public interface IClaimService
{
    Task<Claim> RegisterNewClaim(Claim claim);      // Affärsspråk!
    Task<Claim> ApproveClaim(Guid id);              // Affärsspråk!
    Task<Claim> RejectClaim(Guid id, string reason); // Affärsspråk!
}
```

I vår enklare version har vi `CreateClaim` istället för `RegisterNewClaim`, men principen är densamma.

---

### 2. Service Implementation: `ClaimService`

**Design:**

```csharp
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _repository;
    private readonly ClaimBusinessRules _businessRules;

    public ClaimService(
        IClaimRepository repository,
        ClaimBusinessRules businessRules)
    {
        _repository = repository;
        _businessRules = businessRules;
    }

    public async Task<Claim> CreateClaim(Claim claim)
    {
        // BR3: Validera rapporteringsfrist för reseskador
        if (claim is TravelClaim travelClaim)
        {
            if (travelClaim.EndDate.HasValue &&
                (DateTime.Now - travelClaim.EndDate.Value).TotalDays > 14)
            {
                throw new BusinessRuleViolationException(
                    "Reseskador måste rapporteras inom 14 dagar");
            }
        }

        // BR1: Kontrollera om manuell granskning krävs
        if (_businessRules.RequiresManualReview(claim))
        {
            claim.UpdateStatus(ClaimStatus.RequiresManualReview);
        }

        // BR2: Kontrollera om eskalering krävs
        if (_businessRules.RequiresEscalation(claim))
        {
            claim.UpdateStatus(ClaimStatus.Escalated);
        }

        // BR5: Kontrollera misstänkt mönster
        var existing = await _repository.GetAll();
        if (await _businessRules.IsSuspiciousPattern(claim, existing))
        {
            claim.UpdateStatus(ClaimStatus.RequiresManualReview);
        }

        return await _repository.Save(claim);
    }

    // ... andra metoder
}
```

**Ansvarsfördelning:**

```
┌──────────────────────────────────────────────────────────┐
│                    ClaimService                          │
│                                                          │
│  Ansvar:                                                 │
│  1. Orchestration (koordinerar domain + repo)           │
│  2. Transaction boundaries (vad sparas tillsammans)      │
│  3. Application-level validation                        │
│  4. Use case coordination                               │
└──────────────────────────────────────────────────────────┘
                        ↓ använder
┌──────────────────────────────────────────────────────────┐
│              ClaimBusinessRules (Domain Service)         │
│                                                          │
│  Ansvar:                                                 │
│  1. Business logic (BR1, BR2, BR5)                      │
│  2. Ren logik - inga I/O operations                     │
└──────────────────────────────────────────────────────────┘
                        ↓ använder
┌──────────────────────────────────────────────────────────┐
│           Claim Entities (Domain Models)                 │
│                                                          │
│  Ansvar:                                                 │
│  1. Data structure                                      │
│  2. Invariants (Description length, etc)                │
│  3. State transitions                                    │
└──────────────────────────────────────────────────────────┘
```

**Varför application-level validation här?**

BR3 (rapporteringsfrist) är en **blocking rule** - vi vill förhindra skapande:

```csharp
// Här i Application - kan throwa exception innan vi sparar
if (tooLate)
    throw new BusinessRuleViolationException(...);

// Om detta var på entity - claim är redan skapat
// Om detta var i domain service - måste man komma ihåg att kalla det
```

**Trade-offs:**

| Aspekt                    | Fördel                                | Nackdel                                                 |
| ------------------------- | ------------------------------------- | ------------------------------------------------------- |
| Orchestration i Service   | Clear separation, testable            | Kan bli "anemic domain" om för mycket logik flyttas hit |
| Exceptions för validation | Tydligt fail-fast                     | Exceptions är "dyra", inte alltid lämpligt för UX       |
| Status updates via domain | Domain objects kontrollerar sin state | Fler metoder på entity                                  |

---

### 3. Exception Design: `BusinessRuleViolationException`

**Design:**

```csharp
public class BusinessRuleViolationException : Exception
{
    public string RuleCode { get; }

    public BusinessRuleViolationException(string message, string ruleCode = "")
        : base(message)
    {
        RuleCode = ruleCode;
    }
}
```

**Varför custom exception?**

1. **Catch specific business errors:**

   ```csharp
   try
   {
       await claimService.CreateClaim(claim);
   }
   catch (BusinessRuleViolationException ex)
   {
       // Visa användarvänligt meddelande
       ShowError(ex.Message);
   }
   catch (Exception ex)
   {
       // System error - logga och visa generic message
       LogError(ex);
       ShowError("Något gick fel");
   }
   ```

2. **Rule identification:**

   ```csharp
   throw new BusinessRuleViolationException(
       "Reseskador måste rapporteras inom 14 dagar",
       "BR3");

   // Kan användas för telemetry, logging, etc
   ```

**Alternativ approach - Result pattern:**

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string Error { get; init; } = "";
}

public async Task<Result<Claim>> CreateClaim(Claim claim)
{
    if (tooLate)
        return new Result<Claim>
        {
            IsSuccess = false,
            Error = "För sent att rapportera"
        };

    var saved = await _repository.Save(claim);
    return new Result<Claim> { IsSuccess = true, Value = saved };
}
```

**Trade-offs:**

| Approach       | Fördel                                 | Nackdel                                      |
| -------------- | -------------------------------------- | -------------------------------------------- |
| Exceptions     | C# idiomatiskt, stack traces           | Performance overhead, flow control           |
| Result pattern | Explicit error handling, no exceptions | Verbositet, inte C# idiom                    |
| Either monad   | Functional approach                    | Kräver library (LanguageExt), learning curve |

---

## Infrastructure Layer - Implementation Details

### 1. Repository Implementation: `InMemoryClaimRepository`

**Design:**

```csharp
public class InMemoryClaimRepository : IClaimRepository
{
    private readonly List<Claim> _claims = new();

    public Task<Claim> Save(Claim claim)
    {
        // Kolla om det är update eller create
        var existing = _claims.FirstOrDefault(c => c.Id == claim.Id);
        if (existing != null)
        {
            _claims.Remove(existing);
        }

        _claims.Add(claim);
        return Task.FromResult(claim);
    }

    public Task<Claim?> GetById(Guid id)
    {
        var claim = _claims.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(claim);
    }

    public Task<IEnumerable<Claim>> GetAll()
    {
        return Task.FromResult<IEnumerable<Claim>>(_claims);
    }

    public Task<IEnumerable<Claim>> GetByRegistrationNumber(string registrationNumber)
    {
        var regNr = new RegistrationNumber(registrationNumber);

        var vehicleClaims = _claims
            .OfType<VehicleClaim>()
            .Where(c => c.RegistrationNumber.Equals(regNr));

        return Task.FromResult<IEnumerable<Claim>>(vehicleClaims);
    }
}
```

**Tekniska beslut:**

1. **List<Claim> som storage** - Enkelt för demo:

   ```csharp
   // In-memory = förlorar data vid restart
   // Men perfekt för demo och tester
   ```

2. **Task.FromResult()** - Async interface, sync implementation:

   ```csharp
   // Interface är async (för framtida SQL)
   Task<Claim> Save(Claim claim);

   // Implementation är sync (in-memory är snabbt)
   return Task.FromResult(claim);
   ```

3. **Save = Create AND Update** - Idempotent operation:

   ```csharp
   // Samma metod för både create och update
   var existing = _claims.FirstOrDefault(c => c.Id == claim.Id);
   if (existing != null)
       _claims.Remove(existing); // Update

   _claims.Add(claim); // Create eller re-add
   ```

**Migration till SQL:**

```csharp
public class SqlClaimRepository : IClaimRepository
{
    private readonly AppDbContext _context;

    public SqlClaimRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Claim> Save(Claim claim)
    {
        var existing = await _context.Claims
            .FirstOrDefaultAsync(c => c.Id == claim.Id);

        if (existing == null)
            _context.Claims.Add(claim);
        else
            _context.Entry(existing).CurrentValues.SetValues(claim);

        await _context.SaveChangesAsync();
        return claim;
    }

    // ... samma interface!
}
```

**Enda ändring i DI:**

```csharp
// Från
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();

// Till
builder.Services.AddScoped<IClaimRepository, SqlClaimRepository>();
builder.Services.AddDbContext<AppDbContext>(...);
```

**Trade-offs:**

| Aspekt      | In-Memory              | SQL Database                              |
| ----------- | ---------------------- | ----------------------------------------- |
| Setup       | 0 config, instant      | Connection string, migrations             |
| Performance | Mycket snabbt          | Network latency                           |
| Persistence | ❌ Förlorar data       | ✅ Persistent                             |
| Testing     | ✅ Enkelt, inget setup | Behöver testcontainers eller inmemory SQL |
| Scalability | ❌ En instans          | ✅ Flera instanser kan dela DB            |

---

## Presentation Layer - Blazor UI

### 1. Component Design: `CreateClaim.razor`

**Structure:**

```razor
@page "/create-claim"
@inject IClaimService ClaimService
@inject NavigationManager Navigation

<h3>Skapa Skadeanmälan</h3>

<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <!-- Claim Type Selection -->
    <div class="form-group">
        <label>Typ av skada:</label>
        <InputSelect @bind-Value="selectedType" class="form-control">
            <option value="">-- Välj typ --</option>
            <option value="Vehicle">Fordonsskada</option>
            <option value="Property">Egendomsskada</option>
            <option value="Travel">Reseskada</option>
        </InputSelect>
    </div>

    <!-- Common Fields -->
    <div class="form-group">
        <label>Beskrivning:</label>
        <InputTextArea @bind-Value="model.Description" class="form-control" rows="4" />
    </div>

    <div class="form-group">
        <label>Datum för skada:</label>
        <InputDate @bind-Value="model.ReportedDate" class="form-control" />
    </div>

    <!-- Conditional Fields per Type -->
    @if (selectedType == "Vehicle")
    {
        <div class="form-group">
            <label>Registreringsnummer:</label>
            <InputText @bind-Value="vehicleModel.RegistrationNumber" class="form-control" />
        </div>

        <div class="form-group">
            <label>Skadenummer (polisanmälan):</label>
            <InputText @bind-Value="vehicleModel.PoliceReportNumber" class="form-control" />
        </div>
    }

    @if (selectedType == "Property")
    {
        <div class="form-group">
            <label>Adress:</label>
            <InputText @bind-Value="propertyModel.Address" class="form-control" />
        </div>

        <div class="form-group">
            <label>Typ av skada:</label>
            <InputSelect @bind-Value="propertyModel.DamageType" class="form-control">
                <option value="Fire">Brand</option>
                <option value="Water">Vatten</option>
                <option value="Theft">Stöld</option>
                <option value="Vandalism">Skadegörelse</option>
            </InputSelect>
        </div>

        <div class="form-group">
            <label>Uppskattat värde:</label>
            <InputNumber @bind-Value="propertyModel.EstimatedValue" class="form-control" />
        </div>
    }

    @if (selectedType == "Travel")
    {
        <div class="form-group">
            <label>Destination:</label>
            <InputText @bind-Value="travelModel.Destination" class="form-control" />
        </div>

        <div class="form-group">
            <label>Startdatum:</label>
            <InputDate @bind-Value="travelModel.StartDate" class="form-control" />
        </div>

        <div class="form-group">
            <label>Slutdatum (om resan avslutats):</label>
            <InputDate @bind-Value="travelModel.EndDate" class="form-control" />
        </div>

        <div class="form-group">
            <label>Typ av incident:</label>
            <InputSelect @bind-Value="travelModel.IncidentType" class="form-control">
                <option value="LostLuggage">Förlorat bagage</option>
                <option value="FlightCancellation">Inställt flyg</option>
                <option value="MedicalEmergency">Medicinsk nödsituation</option>
            </InputSelect>
        </div>
    }

    <button type="submit" class="btn btn-primary">Skapa anmälan</button>
</EditForm>

@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="alert alert-danger mt-3">@errorMessage</div>
}

@code {
    private string selectedType = "";
    private string errorMessage = "";

    // Models för varje typ
    private ClaimFormModel model = new();
    private VehicleFormModel vehicleModel = new();
    private PropertyFormModel propertyModel = new();
    private TravelFormModel travelModel = new();

    private async Task HandleSubmit()
    {
        try
        {
            Claim claim = selectedType switch
            {
                "Vehicle" => CreateVehicleClaim(),
                "Property" => CreatePropertyClaim(),
                "Travel" => CreateTravelClaim(),
                _ => throw new InvalidOperationException("Välj en typ")
            };

            await ClaimService.CreateClaim(claim);
            Navigation.NavigateTo("/claims");
        }
        catch (BusinessRuleViolationException ex)
        {
            errorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            errorMessage = $"Ett fel inträffade: {ex.Message}";
        }
    }

    private VehicleClaim CreateVehicleClaim()
    {
        return new VehicleClaim(
            model.Description,
            model.ReportedDate,
            new RegistrationNumber(vehicleModel.RegistrationNumber),
            vehicleModel.PoliceReportNumber
        );
    }

    // ... andra factory methods

    // Form models (för binding)
    private class ClaimFormModel
    {
        public string Description { get; set; } = "";
        public DateTime ReportedDate { get; set; } = DateTime.Today;
    }

    private class VehicleFormModel
    {
        public string RegistrationNumber { get; set; } = "";
        public string PoliceReportNumber { get; set; } = "";
    }

    // ... andra form models
}
```

**Design rationale:**

1. **Conditional rendering** - Olika fält per typ:

   ```razor
   @if (selectedType == "Vehicle")
   {
       <!-- Vehicle-specifika fält -->
   }
   ```

   **Alternativ:** Separate components per type

   ```razor
   @if (selectedType == "Vehicle")
   {
       <VehicleClaimForm @bind-Claim="vehicleClaim" />
   }
   ```

2. **Form models vs Domain models** - Separation för binding:

   ```csharp
   // Form model - mutable, för @bind
   private class VehicleFormModel
   {
       public string RegistrationNumber { get; set; } = "";
   }

   // Domain model - immutable, för logic
   var claim = new VehicleClaim(...); // Constructor with validation
   ```

3. **Factory methods** - Konverterar form → domain:
   ```csharp
   private VehicleClaim CreateVehicleClaim()
   {
       return new VehicleClaim(
           model.Description,
           model.ReportedDate,
           new RegistrationNumber(vehicleModel.RegistrationNumber),
           vehicleModel.PoliceReportNumber
       );
   }
   ```

**Trade-offs:**

| Approach              | Fördel              | Nackdel                          |
| --------------------- | ------------------- | -------------------------------- |
| Conditional rendering | En komponent, enkel | Kan bli lång och komplex         |
| Component per type    | SRP, återanvändbara | Fler filer, mer overhead         |
| Form models           | Clean separation    | Extra mapping code               |
| Direct domain binding | Mindre kod          | Domain models måste vara mutable |

---

### 2. Dependency Injection: `Program.cs`

**Configuration:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Domain services - Singleton (no state, pure logic)
builder.Services.AddSingleton<ClaimBusinessRules>();

// Application services - Scoped (per request/circuit)
builder.Services.AddScoped<IClaimService, ClaimService>();

// Infrastructure - Singleton för in-memory (SKULLE vara Scoped för SQL!)
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

**Lifetime choices:**

| Service                        | Lifetime  | Rationale                                  |
| ------------------------------ | --------- | ------------------------------------------ |
| `ClaimBusinessRules`           | Singleton | Ingen state, ren logik - kan återanvändas  |
| `IClaimService`                | Scoped    | En instans per Blazor circuit              |
| `IClaimRepository` (in-memory) | Singleton | Delas mellan alla användare (shared state) |
| `IClaimRepository` (SQL)       | Scoped    | DbContext är scoped                        |

**OBS: Singleton anti-pattern för SQL:**

```csharp
// ❌ FARLIGT med SQL - DbContext är inte thread-safe!
builder.Services.AddSingleton<IClaimRepository, SqlClaimRepository>();

// ✅ RÄTT - Scoped per request/circuit
builder.Services.AddScoped<IClaimRepository, SqlClaimRepository>();
builder.Services.AddDbContext<AppDbContext>(...); // Scoped by default
```

---

## SOLID Principles - Konkreta Exempel

### Single Responsibility Principle (SRP)

**Exempel i vår kod:**

```csharp
// ✅ ClaimService - ENDAST orchestration
public class ClaimService : IClaimService
{
    public async Task<Claim> CreateClaim(Claim claim) { ... }
}

// ✅ ClaimBusinessRules - ENDAST business logic
public class ClaimBusinessRules
{
    public bool RequiresManualReview(Claim claim) { ... }
}

// ✅ InMemoryClaimRepository - ENDAST data access
public class InMemoryClaimRepository : IClaimRepository
{
    public Task<Claim> Save(Claim claim) { ... }
}
```

**Anti-exempel:**

```csharp
// ❌ God class - GÖR ALLT
public class ClaimManager
{
    public async Task<Claim> CreateClaim(Claim claim)
    {
        // Validation
        if (claim.Description.Length < 20) throw ...

        // Business rules
        if ((DateTime.Now - claim.ReportedDate).TotalDays > 30)
            claim.Status = ...;

        // Data access
        _claims.Add(claim);

        // Logging
        _logger.Log(...);

        // Email
        _emailService.Send(...);

        return claim;
    }
}
```

---

### Open/Closed Principle (OCP)

**Exempel: Lägg till ny skadetyp**

Tack vare arv och polymorfism:

```csharp
// Lägga till CyberClaim - inga ändringar i befintlig kod!
public class CyberClaim : Claim
{
    public string AffectedSystem { get; private set; }
    public DateTime BreachDate { get; private set; }

    public CyberClaim(string description, DateTime reportedDate,
                      string affectedSystem, DateTime breachDate)
        : base(description, reportedDate)
    {
        ClaimType = ClaimType.Cyber;
        AffectedSystem = affectedSystem;
        BreachDate = breachDate;
    }
}

// Repository behöver ingen ändring - jobbar med Claim base class
// Service behöver ingen ändring - orchestration är densamma
// UI behöver konditionell rendering - men ingen ändring i shared logic
```

---

### Liskov Substitution Principle (LSP)

**Exempel i vår kod:**

```csharp
// Alla subklasser kan användas där Claim förväntas
public void ProcessClaim(Claim claim) // Accepterar base class
{
    Console.WriteLine(claim.Description); // Fungerar för alla subklasser
    claim.UpdateStatus(ClaimStatus.Approved); // Fungerar för alla
}

// Fungerar med VehicleClaim, PropertyClaim, TravelClaim
var vehicle = new VehicleClaim(...);
ProcessClaim(vehicle); // ✅ Fungerar

var property = new PropertyClaim(...);
ProcessClaim(property); // ✅ Fungerar
```

**Anti-exempel:**

```csharp
// ❌ BRYTER LSP
public class BrokenPropertyClaim : Claim
{
    public override void UpdateStatus(ClaimStatus newStatus)
    {
        throw new NotImplementedException(); // Bryter kontrakt!
    }
}

// Går inte att använda där Claim förväntas
ProcessClaim(new BrokenPropertyClaim(...)); // 💥 Exception!
```

---

### Interface Segregation Principle (ISP)

**Exempel:**

```csharp
// ✅ Specifika interfaces - klienter behöver bara vad de använder
public interface IClaimRepository
{
    Task<Claim> Save(Claim claim);
    Task<Claim?> GetById(Guid id);
    Task<IEnumerable<Claim>> GetAll();
    Task<IEnumerable<Claim>> GetByRegistrationNumber(string registrationNumber);
}

// Om vi bara behöver läsa:
public interface IReadOnlyClaimRepository
{
    Task<Claim?> GetById(Guid id);
    Task<IEnumerable<Claim>> GetAll();
}
```

**Anti-exempel:**

```csharp
// ❌ Fat interface - klumpar ihop allt
public interface IClaimRepository
{
    // Claims
    Task<Claim> SaveClaim(Claim claim);
    Task DeleteClaim(Guid id);

    // Customers
    Task<Customer> SaveCustomer(Customer customer);
    Task DeleteCustomer(Guid id);

    // Policies
    Task<Policy> SavePolicy(Policy policy);

    // Rapporter
    Task<byte[]> GenerateClaimReport(Guid id);
    Task<byte[]> GenerateCustomerReport(Guid id);
}

// Problem: Varje klient måste implementera ALLT, även om de bara behöver en del
```

---

### Dependency Inversion Principle (DIP)

**Exempel i vår kod:**

```csharp
// ✅ High-level module (Application) beror på abstraction
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _repository; // Interface!

    public ClaimService(IClaimRepository repository)
    {
        _repository = repository;
    }
}

// Low-level module (Infrastructure) implementerar abstraction
public class InMemoryClaimRepository : IClaimRepository { ... }
public class SqlClaimRepository : IClaimRepository { ... }

// Dependency injection container binder ihop
builder.Services.AddScoped<IClaimRepository, InMemoryClaimRepository>();
```

**Utan DIP:**

```csharp
// ❌ High-level module beror på low-level implementation
public class ClaimService
{
    private readonly InMemoryClaimRepository _repository; // Konkret klass!

    public ClaimService()
    {
        _repository = new InMemoryClaimRepository(); // Hårdkodat!
    }
}

// Problem: Kan inte byta till SQL utan att ändra ClaimService
// Problem: Svårt att testa - kan inte mocka repository
```

---

## Testing Strategy

### Unit Testing Domain Layer

**Exempel:**

```csharp
[Fact]
public void VehicleClaim_WithInvalidRegistrationNumber_ThrowsException()
{
    // Arrange
    var description = "Beskrivning som är längre än 20 tecken";
    var reportedDate = DateTime.Today;

    // Act & Assert
    Assert.Throws<ArgumentException>(() =>
        new VehicleClaim(
            description,
            reportedDate,
            new RegistrationNumber("INVALID"), // Ogiltigt format
            "POL123"
        ));
}

[Fact]
public void ClaimBusinessRules_LateReport_RequiresManualReview()
{
    // Arrange
    var rules = new ClaimBusinessRules();
    var claim = new VehicleClaim(
        "Beskrivning som är längre än 20 tecken",
        DateTime.Today.AddDays(-35), // 35 dagar sen
        new RegistrationNumber("ABC123"),
        "POL123"
    );

    // Act
    var result = rules.RequiresManualReview(claim);

    // Assert
    Assert.True(result);
}
```

**Fördelar med vår arkitektur för testing:**

- Domain Layer har INGA dependencies - 100% testbart utan mocks
- Business rules isolerade i ClaimBusinessRules - enkelt att testa

---

### Unit Testing Application Layer

**Exempel med mocks:**

```csharp
[Fact]
public async Task CreateClaim_WithValidClaim_SavesAndReturns()
{
    // Arrange
    var mockRepo = new Mock<IClaimRepository>();
    var businessRules = new ClaimBusinessRules();
    var service = new ClaimService(mockRepo.Object, businessRules);

    var claim = new VehicleClaim(
        "Beskrivning som är längre än 20 tecken",
        DateTime.Today,
        new RegistrationNumber("ABC123"),
        "POL123"
    );

    mockRepo.Setup(r => r.Save(It.IsAny<Claim>()))
            .ReturnsAsync(claim);

    mockRepo.Setup(r => r.GetAll())
            .ReturnsAsync(new List<Claim>());

    // Act
    var result = await service.CreateClaim(claim);

    // Assert
    Assert.Equal(claim.Id, result.Id);
    mockRepo.Verify(r => r.Save(claim), Times.Once);
}
```

---

## Sammanfattning - Key Takeaways

1. **Clean Architecture** - Dependency Rule måste följas strikt
2. **Domain Layer** - Inga dependencies, ren business logic
3. **Application Layer** - Orchestration, koordinerar domain + infra
4. **Infrastructure Layer** - Implementation details, kan bytas ut
5. **SOLID Principles** - Konkreta exempel i koden, inte bara teori
6. **Testing** - Arkitekturen stödjer testbarhet utan mocks på domain layer
7. **Trade-offs** - Varje designval har pros/cons - medvetenhet viktigt

**Remember:** Detta är EN möjlig implementation. Det finns MÅNGA andra sätt att designa detta. Det viktiga är att förstå trade-offs och kunna motivera val.
