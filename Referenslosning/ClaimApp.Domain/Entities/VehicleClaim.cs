using ClaimApp.Domain.Enums;
using ClaimApp.Domain.ValueObjects;

namespace ClaimApp.Domain.Entities;

/// <summary>
/// Skadeanmälan för fordonsskador
/// 
/// SPECIFIKA KRAV (från krav.md):
/// - Registreringsnummer (svenskt format) - REQUIRED
/// - Polisanmälan nummer - REQUIRED
/// 
/// BUSINESS RULES:
/// - BR1: Sen rapportering (>30 dagar) kräver manuell granskning
/// - BR5: Misstänkt mönster (3+ claims på samma fordon inom 90 dagar)
/// 
/// DESIGN: Simplaste claim-typen (baseline complexity)
/// </summary>
public class VehicleClaim : Claim
{
    /// <summary>
    /// Registreringsnummer som Value Object
    /// 
    /// VARFÖR VALUE OBJECT?
    /// - Garanterad validering: Kan inte skapa invalid VehicleClaim
    /// - Value equality: Kan jämföra två reg.nummer enkelt
    /// - Encapsulation: All regex-logik är gömd
    /// 
    /// Exempel från constructor:
    /// new VehicleClaim(..., new RegistrationNumber("ABC123"), ...)
    /// 
    /// Om invalid format:
    /// new RegistrationNumber("INVALID") -> ArgumentException
    /// = VehicleClaim kan aldrig ha invalid reg.nummer!
    /// </summary>
    public RegistrationNumber RegistrationNumber { get; private set; }

    /// <summary>
    /// Polisanmälan nummer (fritext)
    /// 
    /// DESIGN DECISION: String över Value Object
    /// Rationale: Inget specifikt format att validera
    /// Kan vara "POL123", "2024-12345", etc (varierar per polismyndighet)
    /// 
    /// DISKUSSIONSFRÅGA:
    /// - Skulle detta vara en Value Object om vi hade strikt format?
    /// - När är Value Objects "worth it" och när är de over-engineering?
    /// </summary>
    public string PoliceReportNumber { get; private set; }

    /// <summary>
    /// Skapar en fordonsskada med validering
    /// 
    /// Anropar base constructor (Claim) för gemensam validering:
    /// - Description minst 20 tecken (BR4)
    /// - ReportedDate required
    /// 
    /// Validerar vehicle-specifika fält:
    /// - RegistrationNumber via Value Object
    /// - PoliceReportNumber via argument check
    /// 
    /// DESIGN: Constructor validation pattern
    /// Objektet kan inte skapas i invalid state
    /// = "Make invalid states unrepresentable"
    /// 
    /// Exempel:
    /// <code>
    /// var claim = new VehicleClaim(
    ///     "Stenskott på motorhuv efter körning på E4",
    ///     DateTime.Today,
    ///     new RegistrationNumber("ABC123"),
    ///     "POL-2024-001"
    /// );
    /// </code>
    /// </summary>
    public VehicleClaim(
        string description,
        DateTime reportedDate,
        RegistrationNumber registrationNumber,
        string policeReportNumber)
        : base(description, reportedDate) // Call base constructor
    {
        // Validera vehicle-specifika fält
        if (registrationNumber == null)
            throw new ArgumentNullException(
                nameof(registrationNumber),
                "Registreringsnummer måste anges för fordonsskador");

        if (string.IsNullOrWhiteSpace(policeReportNumber))
            throw new ArgumentException(
                "Polisanmälan nummer måste anges för fordonsskador",
                nameof(policeReportNumber));

        // Sätt properties
        RegistrationNumber = registrationNumber;
        PoliceReportNumber = policeReportNumber.Trim();

        // Sätt claim type (protected set på base class)
        ClaimType = ClaimType.Vehicle;
    }

    // DISKUSSIONSPUNKT: Behavior på entity?
    // 
    // Skulle vi kunna ha methods här som:
    // 
    // public bool IsLateReport()
    // {
    //     return AgeInDays() > 30; // BR1 logic
    // }
    //
    // TRADE-OFFS:
    // + Rich domain model - entity vet sina egna regler
    // + Easy to test - no dependencies
    // - Business rules sprids ut (svårt att hitta alla regler)
    // - Mindre Single Responsibility (data + logic)
    //
    // I vår lösning: BR1 logik är i ClaimBusinessRules (Domain Service)
    // Rationale: Centraliserad plats för alla business rules
}
