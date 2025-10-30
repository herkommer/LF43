using ClaimApp.Domain.Enums;

namespace ClaimApp.Domain.Entities;

/// <summary>
/// Abstract base class för alla skadeanmälningar
/// 
/// DESIGN DECISION: ARV (INHERITANCE)
/// 
/// Varför abstract class över interface?
/// + Code reuse: Alla claims har Id, Description, ReportedDate, Status
/// + Tvingande struktur: Alla subklasser MÅSTE implementera vissa saker
/// + Shared behavior: UpdateStatus() är samma för alla
/// 
/// Varför arv över composition?
/// + Natural "is-a" relationship: VehicleClaim IS-A Claim
/// + Polymorfi: List&lt;Claim&gt; kan innehålla alla typer
/// + Domain modeling: Matchar hur affären tänker ("en fordonsskada ÄR en skada")
/// 
/// TRADE-OFFS:
/// + Enkel att förstå (traditional OOP)
/// + Polymorphism fungerar naturligt
/// - Kan bara ärva från EN klass (limitation i C#)
/// - Tight coupling mellan base och subclass
/// - Svårt att hantera "hybrid" claims (t.ex. fordon OCH egendom)
/// 
/// ALTERNATIV APPROACH - COMPOSITION:
/// <code>
/// public class Claim
/// {
///     public VehicleDetails? VehicleDetails { get; set; }
///     public PropertyDetails? PropertyDetails { get; set; }
///     public TravelDetails? TravelDetails { get; set; }
/// }
/// </code>
/// 
/// Trade-offs:
/// + Flexibel: Kan kombinera flera typer
/// + Looser coupling
/// - Mer komplex validering (måste säkerställa att exakt EN är satt)
/// - Mer null checks i koden
/// 
/// DISKUSSIONSFRÅGOR:
/// - Om affären vill lägga till "HybridClaim" (både fordon och egendom) - hur enkelt?
/// - Är arv en code smell i moderna applikationer?
/// - När är composition bättre än inheritance?
/// </summary>
public abstract class Claim
{
    /// <summary>
    /// Unikt ID för skadeanmälan
    /// 
    /// DESIGN: Guid over int
    /// + Kan genereras client-side (inte beroende av DB)
    /// + Globalt unik (kan merge data från flera källor)
    /// + Inga information leakage (int sekvens kan avslöja volym)
    /// - Tar mer plats (16 bytes vs 4 bytes)
    /// - Inte human-readable
    /// 
    /// Private setter: Kan inte ändras utifrån
    /// Sätts i constructor till nytt Guid
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Beskrivning av skadan (minst 20 tecken enligt BR4)
    /// Private setter + validation i SetDescription method
    /// = Garanterar alltid korrekt state
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Datum då skadan rapporterades
    /// </summary>
    public DateTime ReportedDate { get; private set; }

    /// <summary>
    /// Aktuell status (Pending, Approved, etc)
    /// </summary>
    public ClaimStatus Status { get; private set; }

    /// <summary>
    /// Typ av skada (Vehicle, Property, Travel)
    /// Protected set: Subklasser sätter sin specifika typ
    /// </summary>
    public ClaimType ClaimType { get; protected set; }

    /// <summary>
    /// Protected constructor - kan bara kallas från subklasser
    /// Enforces: Alla claims MÅSTE ha description och reportedDate
    /// 
    /// DESIGN PATTERN: Constructor Validation
    /// Objektet kan inte skapas i invalid state
    /// Fail fast: Exception vid skapande, inte senare
    /// </summary>
    protected Claim(string description, DateTime reportedDate)
    {
        Id = Guid.NewGuid(); // Generera unikt ID
        SetDescription(description); // Validerar description
        ReportedDate = reportedDate;
        Status = ClaimStatus.Pending; // Default status
    }

    /// <summary>
    /// Sätter beskrivning med validering (BR4)
    /// 
    /// Business Rule BR4: Description måste vara minst 20 tecken
    /// 
    /// DESIGN DECISION: Validering i Domain
    /// Fördel: Garanterat korrekt på domain level
    /// Nackdel: Exception kan vara "dyrt", måste catchas i UI
    /// 
    /// Alternativ: Validation layer i Application eller Presentation
    /// Trade-off: Mer flexibel men ingen garanti på domain level
    /// </summary>
    private void SetDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Beskrivning får inte vara tom",
                nameof(value));

        if (value.Trim().Length < 20)
            throw new ArgumentException(
                "Beskrivning måste vara minst 20 tecken långt " +
                $"(nuvarande: {value.Trim().Length} tecken)",
                nameof(value));

        Description = value.Trim();
    }

    /// <summary>
    /// Uppdaterar status (t.ex. från Pending till Approved)
    /// 
    /// DESIGN: Method över public setter
    /// Fördel: Kan lägga till validation/business rules här framöver
    /// Exempel: Kanske vissa status-transitions inte är tillåtna?
    /// 
    /// Framtida utökning:
    /// <code>
    /// public void UpdateStatus(ClaimStatus newStatus)
    /// {
    ///     // Business rule: Kan inte gå från Rejected tillbaka till Pending
    ///     if (Status == ClaimStatus.Rejected && newStatus == ClaimStatus.Pending)
    ///         throw new InvalidOperationException("...");
    ///     
    ///     Status = newStatus;
    /// }
    /// </code>
    /// </summary>
    public void UpdateStatus(ClaimStatus newStatus)
    {
        Status = newStatus;
    }

    /// <summary>
    /// Beräknar hur gammal anmälan är i dagar
    /// Används för BR1: >30 dagar kräver manuell granskning
    /// 
    /// DESIGN DECISION: Behavior på entity
    /// Detta är en "Rich Domain Model" approach
    /// Entity har både data OCH behavior
    /// 
    /// Alternativ: Anemic domain model - bara data, logic i service
    /// </summary>
    public int AgeInDays()
    {
        return (DateTime.Now - ReportedDate).Days;
    }
}
