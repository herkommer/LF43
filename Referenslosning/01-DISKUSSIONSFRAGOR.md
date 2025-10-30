# Diskussionsfrågor för Uppföljning

**Syfte:** Dessa frågor är avsedda att skapa reflektion och diskussion under uppföljningen. De har inga "rätta svar" - målet är att utforska trade-offs och olika perspektiv.

---

## 1. Domain Design

### Arv vs Composition

**Öppna frågor:**

- "Du som valde arv - hur enkelt skulle det vara att lägga till en 'Hybridskada' som är både fordon OCH egendom?"
- "Du som valde composition - hur hanterar du gemensam funktionalitet (t.ex. Age calculation)?"
- "När är arv 'rätt' och när är det en code smell?"

**Tillkommande scenario:**

> "Affären vill nu att reseskador KAN ha ett fordon kopplat till sig (hyrbi vid resan). Hur påverkar det din design?"

---

### Value Objects

**Öppna frågor:**

- "Du som skapade `RegistrationNumber` som Value Object - är det 'worth it' för 10 rader validering?"
- "Du som använde bara `string` - hur säkerställer du att registreringsnummer alltid är giltigt?"
- "Var går gränsen mellan 'primitive obsession' och 'over-engineering'?"

**Kod att diskutera:**

```csharp
// Alternativ 1: Value Object
public class RegistrationNumber
{
    private readonly string _value;
    public RegistrationNumber(string value)
    {
        if (!IsValid(value)) throw new ArgumentException();
        _value = value;
    }
    // ... validation logic
}

// Alternativ 2: Just string
public string RegistrationNumber { get; set; }

// Alternativ 3: Property med validering
private string _registrationNumber;
public string RegistrationNumber
{
    get => _registrationNumber;
    set
    {
        if (!IsValid(value)) throw new ArgumentException();
        _registrationNumber = value;
    }
}
```

**Fråga:** "Vilket approach föredrar du och varför? Vilka är trade-offs?"

---

### Anemic vs Rich Domain Models

**Öppna frågor:**

- "Har du logik på dina entity-klasser, eller är de bara 'data containers'?"
- "Vad är skillnaden mellan anemic och rich domain models?"
- "När är anemic OK, och när är det problematiskt?"

**Exempel att diskutera:**

```csharp
// Anemic
public class Claim
{
    public Guid Id { get; set; }
    public DateTime ReportedDate { get; set; }
    public bool RequiresManualReview { get; set; } // Just data
}

// Rich
public class Claim
{
    public Guid Id { get; private set; }
    public DateTime ReportedDate { get; private set; }

    public bool RequiresManualReview() // Behavior!
    {
        return (DateTime.Now - ReportedDate).TotalDays > 30;
    }
}
```

**Diskussion:** "Vilken approach använde du? Varför? Vad är fördelarna?"

---

## 2. Affärsregler - Placering

### Var ska affärslogik leva?

**Scenario 1: Sen rapportering (30 dagar)**

Tre alternativ:

**A) På Entity:**

```csharp
public class VehicleClaim : Claim
{
    public bool IsLateReport()
    {
        return (DateTime.Now - ReportedDate).TotalDays > 30;
    }
}
```

**B) Domain Service:**

```csharp
public class ClaimBusinessRules
{
    public bool RequiresManualReview(Claim claim)
    {
        return claim is VehicleClaim &&
               (DateTime.Now - claim.ReportedDate).TotalDays > 30;
    }
}
```

**C) Application Service:**

```csharp
public class ClaimService
{
    public async Task<Claim> CreateClaim(Claim claim)
    {
        if (claim is VehicleClaim &&
            (DateTime.Now - claim.ReportedDate).TotalDays > 30)
        {
            claim.Status = ClaimStatus.RequiresManualReview;
        }
        // ...
    }
}
```

**Frågor:**

- "Vilket valde du? Varför?"
- "Vad är fördelar/nackdelar med varje approach?"
- "Hur påverkar det testbarhet?"
- "Hur enkelt är det att hitta alla affärsregler i din kodbase?"

---

### Rapporteringsfrist för reseskador (14 dagar)

**Denna är annorlunda** - vi vill FÖRHINDRA skapande, inte bara flagga.

**Alternativ:**

**A) Validering i Application Service:**

```csharp
public async Task<Claim> CreateClaim(Claim claim)
{
    if (claim is TravelClaim travel &&
        travel.EndDate.HasValue &&
        (DateTime.Now - travel.EndDate.Value).TotalDays > 14)
    {
        throw new BusinessRuleViolationException("För sent!");
    }
    // ...
}
```

**B) Domain Service som kastar exception:**

```csharp
public void ValidateReportingDeadline(TravelClaim claim)
{
    if (claim.EndDate.HasValue &&
        (DateTime.Now - claim.EndDate.Value).TotalDays > 14)
    {
        throw new DomainException("För sent!");
    }
}
```

**C) Constructor validation:**

```csharp
public class TravelClaim : Claim
{
    public TravelClaim(DateTime reportedDate, DateTime? endDate)
    {
        if (endDate.HasValue &&
            (reportedDate - endDate.Value).TotalDays > 14)
        {
            throw new ArgumentException("För sent!");
        }
        // ...
    }
}
```

**Diskussion:**

- "När ska affärsregler validera vs flagga?"
- "Är exceptions rätt sätt att hantera affärsregler?"
- "Hur påverkar detta användarupplevelsen?"

---

## 3. Validation Strategy

### Multi-layer validation - Duplication eller Defense in Depth?

**Scenario:** "Beskrivning måste vara minst 20 tecken"

**Tre lager:**

- **UI:** `required minlength="20"` på `<textarea>`
- **Application:** FluentValidation eller manual check
- **Domain:** Validering i setter eller constructor

**Frågor:**

- "Har du validering på alla tre nivåer? Varför/varför inte?"
- "Är det 'duplication' eller 'defense in depth'?"
- "Om du bara validerar i UI - vad händer om vi lägger till ett API?"
- "Om du validerar överallt - hur undviker du inconsistency?"

**Alternativt perspektiv:**

> "Client-side validering är inte säkerhet - det är UX. Real validation ska vara server-side."

**Motargument:**

> "Men om vi har validering i Domain och Application, är det inte overkill?"

**Diskussion:** Vad tycker du? Var går gränsen?

---

### FluentValidation vs Data Annotations vs Domain Validation

**Tre alternativ:**

**A) Data Annotations:**

```csharp
public class Claim
{
    [Required]
    [MinLength(20)]
    public string Description { get; set; }
}
```

**B) FluentValidation:**

```csharp
public class ClaimValidator : AbstractValidator<Claim>
{
    public ClaimValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20);
    }
}
```

**C) Domain Validation:**

```csharp
public class Claim
{
    private string _description;
    public string Description
    {
        get => _description;
        set
        {
            if (value?.Length < 20)
                throw new ArgumentException();
            _description = value;
        }
    }
}
```

**Frågor:**

- "Vilket approach använder du? Varför?"
- "Hur påverkar det testbarhet?"
- "Vad händer med Domain Layer purity?"

---

## 4. Repository Pattern

### Generic vs Specific

**Scenario:** Vi har `Claim`, och kanske i framtiden `Customer`, `Policy`, etc.

**Alternativ A: Specific Repository:**

```csharp
public interface IClaimRepository
{
    Task<Claim> Save(Claim claim);
    Task<Claim?> GetById(Guid id);
    Task<IEnumerable<Claim>> GetAll();
    Task<IEnumerable<Claim>> GetByRegistrationNumber(string regNr);
}
```

**Alternativ B: Generic Repository:**

```csharp
public interface IRepository<T> where T : class
{
    Task<T> Save(T entity);
    Task<T?> GetById(Guid id);
    Task<IEnumerable<T>> GetAll();
    Task<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate);
}
```

**Frågor:**

- "Vilket valde du? Varför?"
- "Om generic - hur hanterar du entity-specifika queries?"
- "Om specific - är det 'duplication' när alla repositories har Save/GetById?"
- "Hur påverkar det testbarhet och mockability?"

**Reflektionsfråga:** "Generic Repository är en antipattern enligt vissa. Håller du med?"

---

### Unit of Work Pattern

**Fråga:** "Har någon implementerat Unit of Work? Varför/varför inte?"

**Scenario:**

```csharp
// Utan Unit of Work
await claimRepository.Save(claim);
await auditRepository.Save(audit);
// Vad händer om andra callen failar?

// Med Unit of Work
unitOfWork.Claims.Save(claim);
unitOfWork.Audits.Save(audit);
await unitOfWork.CommitAsync();
// Transactional consistency
```

**Diskussion:** "Behöver vi det för in-memory? Vad händer när vi går till SQL?"

---

## 5. Dependency Injection Lifetimes

### Singleton vs Scoped vs Transient

**Scenario:** Du har registrerat:

```csharp
builder.Services.AddSingleton<IClaimRepository, InMemoryClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();
```

**Frågor:**

- "Varför Singleton för repository? Vad händer om vi byter till SQL?"
- "Varför Scoped för service? Skulle Transient fungera? Varför/varför inte?"
- "Vad händer om vi råkar injicera en Scoped service i en Singleton?"

**Tillkommande scenario:**

> "Någon råkar ändra Repository till Scoped. Applikationen kompilerar, men vad händer?"

**Diskussion:** "Hur upptäcker man sådana fel? Finns det guardrails?"

---

## 6. SOLID Principles

### Single Responsibility Principle

**Fråga:** "Peka ut en klass i din kod - vad är dess ENDA ansvar?"

**Tillkommande exempel:**

```csharp
public class ClaimService
{
    public async Task<Claim> CreateClaim(Claim claim)
    {
        // 1. Validering
        ValidateDescription(claim.Description);

        // 2. Affärsregel
        if (RequiresManualReview(claim))
            claim.Status = ClaimStatus.ManualReview;

        // 3. Spara
        await _repository.Save(claim);

        // 4. Skicka email
        await _emailService.SendConfirmation(claim);

        return claim;
    }
}
```

**Fråga:** "Hur många ansvarsområden har denna metod? Är det OK?"

**Diskussion:**

- "Var går gränsen mellan orchestration och too much responsibility?"
- "Skulle du refaktorera detta? Till vad?"

---

### Open/Closed Principle

**Scenario:** "Affären vill lägga till en 4:e skadetyp: 'Cyber' (IT-relaterade skador)"

**Frågor:**

- "Vilka filer måste du ändra?"
- "Vilka filer kan förbli oförändrade?"
- "Är ditt system 'open for extension, closed for modification'?"

**Alternativt scenario:**

```csharp
// Anti-pattern: Switch statement överallt
if (claim.Type == ClaimType.Vehicle) { ... }
else if (claim.Type == ClaimType.Property) { ... }
else if (claim.Type == ClaimType.Travel) { ... }
// Måste ändra VARJE switch vid ny typ
```

**vs polymorfism:**

```csharp
// Polymorphism - lägg bara till ny subclass
claim.Validate(); // Abstract method
```

---

### Liskov Substitution Principle

**Scenario:** Du har arv: `VehicleClaim : Claim`

**Fråga:** "Kan du ersätta `Claim` med `VehicleClaim` överallt utan att beteendet ändras?"

**Tillkommande exempel:**

```csharp
public class Claim
{
    public virtual decimal CalculateDeductible()
    {
        return 5000;
    }
}

public class PropertyClaim : Claim
{
    public override decimal CalculateDeductible()
    {
        throw new NotImplementedException(); // BRYTER LSP!
    }
}
```

**Diskussion:** "Har du några såna 'gotchas' i din kod?"

---

## 7. Blazor State Management

### Var lever state?

**Scenario:** Formulär för att skapa claim

**Alternativ:**

- **A) Component state:** Private fields i `.razor`-filen
- **B) Shared service:** `ClaimFormState` som singleton
- **C) Cascading parameters:** State i parent, propagerar ner

**Frågor:**

- "Vilket valde du? Varför?"
- "Vad händer om användaren öppnar två tabs?"
- "Hur hanterar du state mellan Create och List-sidorna?"

---

### Conditional Rendering - Duplication eller Abstraction?

**Scenario:** Olika fält för olika skadetyper

**Alternativ A: Conditional blocks:**

```razor
@if (selectedType == "Vehicle")
{
    <input @bind="registrationNumber" />
}
@if (selectedType == "Property")
{
    <input @bind="address" />
}
```

**Alternativ B: Component per typ:**

```razor
@if (selectedType == "Vehicle")
{
    <VehicleClaimForm @bind-Claim="claim" />
}
@if (selectedType == "Property")
{
    <PropertyClaimForm @bind-Claim="claim" />
}
```

**Frågor:**

- "Vilket approach använde du?"
- "Vad är trade-off mellan DRY och readability?"
- "Hur hanterar du gemensamma fält (Description, Date)?"

---

## 8. DTOs vs Domain Models

### Använder du samma objekt överallt?

**Scenario:** `Claim` entity används både i Domain OCH Blazor-komponenter

**Frågor:**

- "Hur känns det? Är det tight coupling eller pragmatisk enkelhet?"
- "Vad händer om du vill ändra Domain Model - påverkar det UI?"
- "När skulle du introducera DTOs/ViewModels?"

**Tillkommande scenario:**

> "Affären vill att UI:t ska visa 'Dagar sedan rapportering' - lägger du till property på `Claim`?"

**Alternativ:**

- **A:** Ja, lägg till calculated property på `Claim`
- **B:** Nej, skapa ViewModel med den propertyn
- **C:** Nej, beräkna det i komponenten

**Diskussion:** "Vilket skulle du välja? Varför?"

---

## 9. Testbarhet

### Hur testbar är din lösning?

**Frågor:**

- "Hur skulle du skriva ett unit test för affärsregel 'sen rapportering'?"
- "Vilka dependencies behöver du mocka?"
- "Kan du testa Domain Layer utan Infrastructure?"

**Exempel att diskutera:**

```csharp
// Svårt att testa
public class ClaimService
{
    public async Task<Claim> CreateClaim(Claim claim)
    {
        var repo = new InMemoryClaimRepository(); // Hard dependency!
        if ((DateTime.Now - claim.ReportedDate).TotalDays > 30) // DateTime.Now svårt att testa!
        {
            claim.Status = ClaimStatus.ManualReview;
        }
        return await repo.Save(claim);
    }
}

// Lätt att testa
public class ClaimService
{
    private readonly IClaimRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider; // Abstraction för tid

    public ClaimService(IClaimRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Claim> CreateClaim(Claim claim)
    {
        if ((_dateTimeProvider.Now - claim.ReportedDate).TotalDays > 30)
        {
            claim.Status = ClaimStatus.ManualReview;
        }
        return await _repository.Save(claim);
    }
}
```

**Diskussion:** "Vilket approach har du? Hur påverkar det testbarhet?"

---

## 10. Future-Proofing

### Byta till SQL Database

**Scenario:** "Nu vill affären att data ska sparas i SQL Server istället för in-memory"

**Frågor:**

- "Vilka filer måste du ändra?"
- "Kan du återanvända ditt `IClaimRepository`-interface?"
- "Vad behöver ändras i DI-konfigurationen?"
- "Hur påverkar det lifetime (Singleton → Scoped)?"

**Diskussionspunkt:**

> "Är interface-design verkligen 'future-proof', eller är det 'premature optimization'?"

---

### Lägg till API-lager

**Scenario:** "Affären vill ha ett REST API så mobilappar kan skicka claims"

**Frågor:**

- "Hur enkelt skulle det vara att lägga till ett API-projekt?"
- "Kan du återanvända `ClaimService`?"
- "Behöver du ändra Domain Layer?"
- "Vad är skillnad mellan Blazor-specifik kod och återanvändbar kod?"

---

### Microservices

**Reflektionsfråga:** "Vad skulle hända om vi delade upp detta i microservices?"

**Scenario:**

- `ClaimService` → En microservice
- `CustomerService` → En annan microservice
- `PolicyService` → En tredje microservice

**Frågor:**

- "Hur skulle din arkitektur påverkas?"
- "Vad blir svårare? Vad blir enklare?"
- "När är microservices rätt val? När är det over-engineering?"

---

## 11. Trade-offs & Reflektion

### Vad skulle du göra annorlunda?

**Öppna frågor:**

- "Om du började om från scratch idag - vad skulle du ändra?"
- "Var är största 'technical debt' i din lösning?"
- "Vad är du mest nöjd med i din design?"
- "Vilken designbeslut ångrar du mest?"

---

### Pragmatism vs Purism

**Olika perspektiv:**

> "Clean Architecture är overkill för små applikationer"

> "YAGNI (You Aren't Gonna Need It) vs preparedness - var går gränsen?"

> "Perfect is the enemy of good - när är det OK att ta genvägar?"

**Diskussion:** "Vad tycker du? Hur balanserar man pragmatism och best practices?"
