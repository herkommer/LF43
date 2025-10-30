using ClaimApp.Domain.Enums;
using ClaimApp.Domain.ValueObjects;

namespace ClaimApp.Domain.Entities;

/// <summary>
/// Skadeanmälan för reseskador
/// 
/// SPECIFIKA KRAV (från krav.md):
/// - Destination - REQUIRED
/// - Startdatum - REQUIRED
/// - Slutdatum - OPTIONAL (resa kan pågå)
/// - Typ av incident (LostLuggage, FlightCancellation, MedicalEmergency) - REQUIRED
/// 
/// BUSINESS RULES:
/// - BR3: Måste rapporteras inom 14 dagar efter hemkomst (EndDate)
///   * Detta valideras i Application Layer (ClaimService)
///   * Kan inte skapas om för sent rapporterat
/// 
/// DESIGN: Högst complexity
/// - DateRange Value Object för start/end dates
/// - Optional end date (pågående resa)
/// - Extra validation logic i Application Layer
/// </summary>
public class TravelClaim : Claim
{
    /// <summary>
    /// Destination (land/stad)
    /// 
    /// DESIGN: Simple string
    /// Rationale: Fritext från användare, ingen standardisering behövs
    /// 
    /// ALTERNATIV:
    /// - Country enum: För restriktiv, finns många länder
    /// - Lookup table: Bättre för autocompletion men over-engineering här
    /// - GPS coordinates: Over-kill för insurance domain
    /// 
    /// DISKUSSIONSFRÅGA:
    /// - Om vi vill göra statistik per land - behöver vi strukturerad data?
    /// - Hur hanterar man olika stavningar ("USA" vs "United States")?
    /// </summary>
    public string Destination { get; private set; }

    /// <summary>
    /// Resperiod som DateRange Value Object
    /// 
    /// VARFÖR VALUE OBJECT?
    /// - Encapsulation: Start/End hör ihop logiskt
    /// - Validation: End kan inte vara före Start
    /// - Convenience: DurationInDays(), IsCompleted, etc
    /// - Business logic: Beräkna reporting deadline (BR3)
    /// 
    /// ALTERNATIV 1: Två separata properties
    /// <code>
    /// public DateTime StartDate { get; private set; }
    /// public DateTime? EndDate { get; private set; }
    /// </code>
    /// 
    /// Problem:
    /// - Måste validera End > Start överallt
    /// - Ingen self-documenting methods
    /// - Duplication av logic (DurationInDays beräknas på flera ställen)
    /// 
    /// ALTERNATIV 2: Tuple
    /// <code>
    /// public (DateTime Start, DateTime? End) TravelPeriod { get; private set; }
    /// </code>
    /// 
    /// Problem:
    /// - Ingen validation
    /// - Mindre readable (.Start, .End vs .TravelPeriod.Start)
    /// - Ingen behavior
    /// 
    /// DateRange Value Object vinner pga encapsulation + behavior
    /// </summary>
    public DateRange TravelPeriod { get; private set; }

    /// <summary>
    /// Convenience properties för att undvika .TravelPeriod.Start överallt
    /// Gör API:t mer användarvänligt
    /// 
    /// Exempel:
    /// <code>
    /// // Med convenience properties
    /// if (claim.StartDate > someDate) { ... }
    /// 
    /// // Utan (mer verbose)
    /// if (claim.TravelPeriod.Start > someDate) { ... }
    /// </code>
    /// 
    /// TRADE-OFF:
    /// + Enklare API
    /// - Viss duplication (finns på både claim och TravelPeriod)
    /// </summary>
    public DateTime StartDate => TravelPeriod.Start;
    public DateTime? EndDate => TravelPeriod.End;

    /// <summary>
    /// Typ av incident (enum)
    /// 
    /// VARFÖR ENUM?
    /// - Begränsad set av värden (domain constraint)
    /// - Kan mappas till olika utbetalningsnivåer
    ///   Exempel:
    ///   - LostLuggage: Max 20,000 kr
    ///   - FlightCancellation: Max 5,000 kr
    ///   - MedicalEmergency: Ingen gräns
    /// 
    /// FRAMTIDA UTÖKNING:
    /// - AccidentalInjury
    /// - PassportLoss
    /// - TripCancellation (innan resa)
    /// </summary>
    public TravelIncidentType IncidentType { get; private set; }

    /// <summary>
    /// Skapar en reseskada med validering
    /// 
    /// VIKTIGT: BR3 (reporting deadline) valideras INTE här!
    /// - Den valideras i Application Layer (ClaimService.CreateClaim)
    /// - Rationale: Behöver förhindra skapande, inte bara flagga
    /// 
    /// Constructor validation här:
    /// - Destination required
    /// - StartDate via DateRange Value Object
    /// - EndDate optional (pågående resa OK)
    /// 
    /// Exempel:
    /// <code>
    /// // Avslutad resa
    /// var claim = new TravelClaim(
    ///     "Förlorade resväska på Arlanda efter hemkomst från Barcelona",
    ///     DateTime.Today,
    ///     "Barcelona, Spanien",
    ///     new DateRange(new DateTime(2024, 12, 1), new DateTime(2024, 12, 8)),
    ///     TravelIncidentType.LostLuggage
    /// );
    /// 
    /// // Pågående resa (EndDate = null)
    /// var claim2 = new TravelClaim(
    ///     "Medicinskt nödläge under pågående resa i Thailand",
    ///     DateTime.Today,
    ///     "Phuket, Thailand",
    ///     new DateRange(new DateTime(2024, 12, 15), null), // Pågående!
    ///     TravelIncidentType.MedicalEmergency
    /// );
    /// </code>
    /// </summary>
    public TravelClaim(
        string description,
        DateTime reportedDate,
        string destination,
        DateRange travelPeriod,
        TravelIncidentType incidentType)
        : base(description, reportedDate)
    {
        // Validera travel-specifika fält
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException(
                "Destination måste anges för reseskador",
                nameof(destination));

        if (travelPeriod == null)
            throw new ArgumentNullException(
                nameof(travelPeriod),
                "Reseperiod måste anges");

        // Sätt properties
        Destination = destination.Trim();
        TravelPeriod = travelPeriod;
        IncidentType = incidentType;

        // Sätt claim type
        ClaimType = ClaimType.Travel;
    }

    // DOMAIN BEHAVIOR: Rich Domain Model

    /// <summary>
    /// Kontrollerar om resan är avslutad
    /// Delegerar till TravelPeriod Value Object
    /// 
    /// Business use case:
    /// - BR3 gäller bara avslutade resor
    /// - Pågående resor kan inte valideras för reporting deadline
    /// 
    /// Exempel:
    /// <code>
    /// if (claim.IsTravelCompleted())
    /// {
    ///     // Kontrollera reporting deadline
    /// }
    /// </code>
    /// </summary>
    public bool IsTravelCompleted()
    {
        return TravelPeriod.IsCompleted;
    }

    /// <summary>
    /// Beräknar hur många dagar sedan resan avslutades
    /// Används för BR3: Måste rapporteras inom 14 dagar
    /// 
    /// Returns null om resan inte är avslutad
    /// 
    /// DESIGN: Method över property
    /// Rationale: Computation (inte enkel getter), returnerar null
    /// 
    /// Exempel användning (i ClaimService):
    /// <code>
    /// var daysSinceReturn = travelClaim.DaysSinceReturn();
    /// if (daysSinceReturn.HasValue && daysSinceReturn.Value > 14)
    /// {
    ///     throw new BusinessRuleViolationException("För sent att rapportera");
    /// }
    /// </code>
    /// </summary>
    public int? DaysSinceReturn()
    {
        if (!TravelPeriod.IsCompleted)
            return null;

        return (DateTime.Now - TravelPeriod.End!.Value).Days;
    }
}

// REFLECTION: Arv vs Composition för TravelClaim
//
// TravelClaim är mest komplex:
// - Har DateRange Value Object
// - Har extra validation i Application Layer
// - Har domain methods (IsTravelCompleted, DaysSinceReturn)
//
// Fungerar arv fortfarande?
// + Ja: Delar fortfarande Id, Description, ReportedDate, Status med base
// + Ja: Polymorfism fungerar (List<Claim> kan ha TravelClaim)
// + Ja: Natural "is-a" relationship
//
// Men vad om:
// - Vi får "BusinessTravelClaim" som både är Travel OCH har Company info?
// - Vi får "VehicleInTravelClaim" som är både Travel OCH Vehicle?
//
// Då kanske composition är bättre:
// <code>
// public class Claim
// {
//     public ClaimType Type { get; set; }
//     public VehicleDetails? Vehicle { get; set; }
//     public PropertyDetails? Property { get; set; }
//     public TravelDetails? Travel { get; set; }
// }
// </code>
//
// Men för NU, med 3 helt separata typer: Arv fungerar bra.
// 
// DISKUSSIONSFRÅGA:
// - När blir arv ett problem?
// - Hur refaktorerar man från arv till composition?
// - Är "YAGNI" (You Aren't Gonna Need It) relevant här?
