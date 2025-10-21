# Del 4: Live-demo genomgång

---

## Översikt av demon

Vi bygger en enkel skadeanmälningsapp med:

- Två skadetyper (Fordon & Egendom)
- Clean Architecture-struktur
- Blazor Server UI
- In-memory data storage

---

## Fas 1: Setup projekt

### Skapa Blazor Server-projekt

Se filen: `Demo/ForbereddaMaterial/01-projekt-setup.md` för kommandon

### Projektstruktur

```
ClaimDemo/
├── Components/
│   ├── Pages/           <- Våra sidor
│   └── Layout/          <- Layout-komponenter
├── Program.cs           <- DI-konfiguration
└── appsettings.json
```

Vi kommer skapa vår egen mappstruktur för Clean Architecture.

---

## Fas 2: Domain models

### Skapa mappstruktur

```
mkdir Domain
mkdir Domain/Models
mkdir Domain/Enums
```

### Kopiera in kod

Från: `Demo/ForbereddaMaterial/02-domain-models.md`

- ClaimType.cs (enum)
- Claim.cs (bas-klass)
- VehicleClaim.cs
- PropertyClaim.cs

### Översikt av modellerna

**ClaimType.cs:**

```csharp
public enum ClaimType
{
    Vehicle,
    Property
}
```

En enum för att representera de olika typerna. Typsäkert istället för strängar.

**Claim.cs:**

```csharp
public abstract class Claim
{
    public Guid Id { get; set; }
    public ClaimType Type { get; set; }
    public DateTime ReportedDate { get; set; }
    public string Description { get; set; } = "";
}
```

Abstract bas-klass - kan inte instansieras direkt, bara via subklasser.
Innehåller gemensamma properties för alla skadetyper.

**VehicleClaim.cs:**

```csharp
public class VehicleClaim : Claim
{
    public string RegistrationNumber { get; set; } = "";

    public VehicleClaim()
    {
        Type = ClaimType.Vehicle;
    }
}
```

Ärver från Claim och lägger till fordon-specifika fält.
Konstruktorn sätter Type automatiskt.

### Diskussionsfrågor

- Varför abstract bas-klass istället för interface?
- Var skulle vi lägga en regel som 'ReportedDate får inte vara i framtiden'?

---

## Fas 3: Repository

### IClaimRepository interface

Skapa mappen `Application/Interfaces` och fil `IClaimRepository.cs`

```csharp
public interface IClaimRepository
{
    Task<Claim> Save(Claim claim);
    Task<Claim?> GetById(Guid id);
    Task<IEnumerable<Claim>> GetAll();
}
```

**Viktiga detaljer:**

- Interface = kontrakt, beskriver vad vi kan göra
- Task = asynkront, även om in-memory är snabbt vill vi ha konsekvent API
- Claim? med frågetecken = nullable, GetById kanske inte hittar något

### InMemoryClaimRepository implementation

Skapa mappen `Infrastructure/Repositories` och fil `InMemoryClaimRepository.cs`

```csharp
public class InMemoryClaimRepository : IClaimRepository
{
    private readonly List<Claim> _claims = new();

    public Task<Claim> Save(Claim claim)
    {
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
}
```

**Förklaring:**

- `private readonly List<Claim> _claims` - Vår 'databas', en lista i minnet
- `Save` - Lägger till i listan, returnerar objektet
- `GetById` - FirstOrDefault returnerar null om inget hittas
- `GetAll` - Returnerar hela listan

### Diskussionsfrågor

- Varför interface + implementation istället för bara klassen?
- Vad händer när appen startas om?

---

## Fas 4: Service

### IClaimService interface

Skapa fil `Application/Interfaces/IClaimService.cs`

```csharp
public interface IClaimService
{
    Task<Claim> CreateClaim(Claim claim);
    Task<IEnumerable<Claim>> GetAllClaims();
}
```

### ClaimService implementation

Skapa mappen `Application/Services` och fil `ClaimService.cs`

```csharp
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _repository;

    public ClaimService(IClaimRepository repository)
    {
        _repository = repository;
    }

    public async Task<Claim> CreateClaim(Claim claim)
    {
        // Validering
        if (claim.ReportedDate > DateTime.Now)
        {
            throw new ArgumentException("Kan inte rapportera framtida skador");
        }

        // Sätt ID och spara
        claim.Id = Guid.NewGuid();
        return await _repository.Save(claim);
    }

    public async Task<IEnumerable<Claim>> GetAllClaims()
    {
        return await _repository.GetAll();
    }
}
```

**Förklaring:**

- **Constructor:** Dependency Injection - vi ber om IClaimRepository, får implementation från DI-container
- **CreateClaim:** Här kan vi lägga affärsregler - datum-validering, beräkningar, etc.
- Genererar nytt Guid för ID
- Använder repository för att faktiskt spara

### Diskussionsfrågor

- Var skulle vi lägga en regel: 'Fordonsskador äldre än 30 dagar måste granskas manuellt'?
- Varför async/await även för in-memory?

---

## Fas 5: DI Setup

### Program.cs konfiguration

Öppna Program.cs och lägg till:

```csharp
// Längst upp
using ClaimDemo.Application.Interfaces;
using ClaimDemo.Application.Services;
using ClaimDemo.Infrastructure.Repositories;

// Efter builder.Services.AddRazorComponents()...
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
```

**Förklaring:**

**AddScoped vs AddSingleton:**

- **Singleton = en instans för hela appen**
  - Vår in-memory repository - vi vill att alla delar samma data
- **Scoped = en instans per request**
  - Service - ny instans varje gång användaren gör något

**Mapping:**

- När någon ber om IClaimService, ge dem ClaimService
- När någon ber om IClaimRepository, ge dem InMemoryClaimRepository

### Verifiera

```bash
dotnet build
```

---

## Fas 6: Blazor UI

### CreateClaim.razor

Skapa `Components/Pages/CreateClaim.razor`

**Basen:**

```razor
@page "/claims/create"
@inject IClaimService ClaimService
@inject NavigationManager Navigation

<h3>Skapa skadeanmälan</h3>
```

**Skadetyp-väljare:**

```razor
<div class="mb-3">
    <label>Typ av skada:</label>
    <select @bind="selectedType" class="form-select">
        <option value="">Välj typ</option>
        <option value="Vehicle">Fordon</option>
        <option value="Property">Egendom</option>
    </select>
</div>
```

**Gemensamma fält:**

```razor
<div class="mb-3">
    <label>Datum:</label>
    <input type="date" @bind="reportedDate" class="form-control" />
</div>

<div class="mb-3">
    <label>Beskrivning:</label>
    <textarea @bind="description" class="form-control" rows="4"></textarea>
</div>
```

**Conditional rendering:**

```razor
@if (selectedType == "Vehicle")
{
    <div class="mb-3">
        <label>Registreringsnummer:</label>
        <input type="text" @bind="registrationNumber" class="form-control" />
    </div>
}
else if (selectedType == "Property")
{
    <div class="mb-3">
        <label>Adress:</label>
        <input type="text" @bind="address" class="form-control" />
    </div>
}
```

Beroende på vad användaren väljer visas olika fält.

**Submit-knapp:**

```razor
<button @onclick="HandleSubmit" class="btn btn-primary" disabled="@string.IsNullOrEmpty(selectedType)">
    Skapa skadeanmälan
</button>

@if (!string.IsNullOrEmpty(message))
{
    <div class="alert alert-info mt-3">@message</div>
}
```

**Code-behind:**

```razor
@code {
    private string selectedType = "";
    private DateTime reportedDate = DateTime.Today;
    private string description = "";
    private string registrationNumber = "";
    private string address = "";
    private string message = "";

    private async Task HandleSubmit()
    {
        Claim claim = selectedType switch
        {
            "Vehicle" => new VehicleClaim
            {
                ReportedDate = reportedDate,
                Description = description,
                RegistrationNumber = registrationNumber
            },
            "Property" => new PropertyClaim
            {
                ReportedDate = reportedDate,
                Description = description,
                Address = address
            },
            _ => throw new InvalidOperationException("Ogiltig skadetyp")
        };

        await ClaimService.CreateClaim(claim);
        message = "Skadeanmälan skapad!";

        // Rensa formulär
        selectedType = "";
        description = "";
        registrationNumber = "";
        address = "";
    }
}
```

**Förklaring:**

- State-variabler för varje fält
- `HandleSubmit` skapar rätt typ av Claim baserat på valet
- Switch expression - kompakt sätt att skapa objekt
- Anropar service
- Visar bekräftelse och rensar formuläret

### Lägg till i navigationen

Öppna `Components/Layout/NavMenu.razor` och lägg till:

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="claims/create">
        <span class="bi bi-plus-square" aria-hidden="true"></span> Skapa skada
    </NavLink>
</div>
```

---

## Fas 7: Test

### Kör appen

```bash
dotnet run
```

Öppna https://localhost:5001 (eller den port som visas)

### Testa funktionalitet

1. Navigera till "Skapa skada"
2. Välj "Fordon" - se att registreringsnummer-fältet dyker upp
3. Byt till "Egendom" - se att adress-fältet visas istället
4. Fyll i och skicka
5. Verifiera bekräftelse-meddelandet

### Diskussionsfrågor

- Vad händer om vi startar om appen?
- Hur skulle vi visa en lista på alla inlämnade skador?
- Var skulle vi lägga validering för registreringsnummer-format?
- Hur testar vi detta?

---

## Sammanfattning

**Vad vi byggt:**

✅ **Domain Layer** - Claim-modeller med arv

✅ **Infrastructure Layer** - In-memory repository

✅ **Application Layer** - Service med affärslogik

✅ **Presentation Layer** - Blazor-komponent med conditional rendering

✅ **Dependency Injection** - Allt kopplat ihop i Program.cs

**Viktiga koncept:**

- Separation of Concerns
- Interface-baserad design
- Conditional rendering i Blazor
- Two-way data binding
- Async/await patterns
