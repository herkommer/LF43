using ClaimApp.Domain.Entities;
using ClaimApp.Domain.Enums;

namespace ClaimApp.Domain.Services;

/// <summary>
/// Domain Service för centraliserade affärsregler
/// 
/// VARFÖR DOMAIN SERVICE?
/// 
/// Vissa affärsregler passar inte naturligt på en single entity:
/// 1. Involverar flera entities (BR5: Flera claims för samma fordon)
/// 2. Är cross-cutting (BR1, BR2: Gäller flera claim types)
/// 3. För komplex för att leva på entity (håller entity clean)
/// 
/// DOMAIN SERVICE vs APPLICATION SERVICE:
/// 
/// Domain Service:
/// - Ren business logic
/// - INGA I/O operations (no database, no external APIs)
/// - INGA dependencies på Infrastructure
/// - 100% testbart utan mocks
/// - Kan användas av flera Application Services
/// 
/// Application Service:
/// - Orchestration (koordinerar domain + infrastructure)
/// - GÖR I/O operations (save to repository, send email, etc)
/// - Har dependencies (IRepository, IEmailService)
/// - Behöver mocks för testing
/// 
/// DESIGN DECISION: Centraliserad business logic
/// 
/// Fördelar:
/// + Single Source of Truth - alla regler på ett ställe
/// + Enkelt att hitta och ändra regler
/// + Återanvändbar - kan användas från flera services
/// + Testbar - inga dependencies
/// 
/// Nackdelar:
/// - Kan bli en "god class" om för mycket logik hamnar här
/// - Mindre encapsulation än att ha logic på entity
/// 
/// ALTERNATIV APPROACH: Logic på entities
/// <code>
/// public class VehicleClaim : Claim
/// {
///     public bool RequiresManualReview()
///     {
///         return AgeInDays() > 30;
///     }
/// }
/// </code>
/// 
/// Trade-offs:
/// + Encapsulation - entity "vet" sina regler
/// + Rich Domain Model
/// - Regler sprids ut över flera klasser
/// - Svårare att få översikt över alla regler
/// - Svårt för cross-entity rules (BR5)
/// 
/// DISKUSSIONSFRÅGOR:
/// - När ska logic vara på entity vs domain service?
/// - Är denna service en "god class"?
/// - Hur skulle ni organisera 50+ business rules?
/// </summary>
public class ClaimBusinessRules
{
    /// <summary>
    /// BR1: Sen rapportering kräver manuell granskning
    /// 
    /// REGEL: Fordonsskador rapporterade >30 dagar efter incident
    ///        kräver manuell granskning
    /// 
    /// RATIONALE:
    /// - Längre tid = svårare att verifiera
    /// - Risk för fraud increases
    /// - Behöver extra dokumentation
    /// 
    /// IMPLEMENTATION CHOICE:
    /// - Returnerar bool (flaggar, hindrar inte)
    /// - Application Service sätter status till RequiresManualReview
    /// - Claimget skapas fortfarande, bara flaggas
    /// 
    /// ALTERNATIV:
    /// - Throw exception här (förhindrar skapande)
    ///   Problem: För strängt, affären kanske vill tillåta men flagga
    /// - Validering i Application Layer
    ///   Problem: Är det business logic eller app logic? Gråzon
    /// 
    /// Exempel användning:
    /// <code>
    /// if (_businessRules.RequiresManualReview(claim))
    /// {
    ///     claim.UpdateStatus(ClaimStatus.RequiresManualReview);
    /// }
    /// </code>
    /// </summary>
    /// <param name="claim">Skadeanmälan att kontrollera</param>
    /// <returns>True om manuell granskning krävs</returns>
    public bool RequiresManualReview(Claim claim)
    {
        // BR1 gäller bara fordonsskador
        if (claim is not VehicleClaim)
            return false;

        // Kontrollera om >30 dagar gammalt
        return claim.AgeInDays() > 30;
    }

    /// <summary>
    /// BR2: Höga belopp kräver eskalering
    /// 
    /// REGEL: Egendomsskador >100,000 kr kräver eskalering till senior handläggare
    /// 
    /// RATIONALE:
    /// - Stora utbetalningar behöver extra granskning
    /// - Risk mitigation (fraud, felbedömning)
    /// - Budgetansvar på senior nivå
    /// 
    /// IMPLEMENTATION CHOICE:
    /// - Hard-coded threshold (100,000 kr)
    /// - Returnerar bool (flaggar)
    /// 
    /// ALTERNATIV:
    /// - Threshold från config/database
    ///   + Flexibelt, kan ändras utan deployment
    ///   - Mer komplext, behöver Infrastructure dependency
    ///   - För vår demo: YAGNI (You Aren't Gonna Need It)
    /// 
    /// - Olika thresholds per DamageType
    ///   Exempel: Fire always escalate, Water only >200k
    ///   + Mer granulär control
    ///   - Mycket mer komplex
    /// 
    /// DISKUSSIONSFRÅGA:
    /// - Är hard-coded threshold OK eller code smell?
    /// - När ska business rules vara configurable?
    /// - Vem äger business rules - developers eller business users?
    /// </summary>
    /// <param name="claim">Skadeanmälan att kontrollera</param>
    /// <returns>True om eskalering krävs</returns>
    public bool RequiresEscalation(Claim claim)
    {
        // BR2 gäller bara egendomsskador
        if (claim is not PropertyClaim propertyClaim)
            return false;

        // Kontrollera estimated value
        const decimal escalationThreshold = 100_000m;
        return propertyClaim.EstimatedValue > escalationThreshold;
    }

    /// <summary>
    /// BR5: Misstänkt mönster - Flera claims på samma fordon
    /// 
    /// REGEL (CUSTOM): 3+ fordonsskador på samma registreringsnummer
    ///                 inom 90 dagar = misstänkt, kräver manuell granskning
    /// 
    /// RATIONALE:
    /// - Fraud detection
    /// - Ovanligt med många skador på kort tid
    /// - Kan vara legitimt (yrkesförare) men behöver verifieras
    /// 
    /// IMPLEMENTATION CHOICE:
    /// - Async metod (tar IEnumerable<Claim> som input)
    /// - Räknar existerande claims för samma reg.nummer
    /// - 90 dagar lookback window
    /// 
    /// DISKUSSIONSPUNKT: Varför inte repository här?
    /// 
    /// Domain Service ska INTE ha I/O dependencies:
    /// <code>
    /// // ❌ INTE SÅ HÄR
    /// public class ClaimBusinessRules
    /// {
    ///     private readonly IClaimRepository _repository;
    ///     
    ///     public async Task<bool> IsSuspiciousPattern(Claim claim)
    ///     {
    ///         var existing = await _repository.GetAll();
    ///         // ...
    ///     }
    /// }
    /// </code>
    /// 
    /// Problem med repository i Domain Service:
    /// - Domain Layer får dependency på Infrastructure
    /// - Breaks Clean Architecture dependency rule
    /// - Svårare att testa (måste mocka repository)
    /// 
    /// ISTÄLLET: Application Service hämtar data, skickar till Domain Service
    /// <code>
    /// // ✅ SÅ HÄR
    /// // I Application Service (ClaimService):
    /// var existingClaims = await _repository.GetAll();
    /// if (await _businessRules.IsSuspiciousPattern(newClaim, existingClaims))
    /// {
    ///     newClaim.UpdateStatus(ClaimStatus.RequiresManualReview);
    /// }
    /// </code>
    /// 
    /// Fördelar:
    /// + Domain Service har ZERO dependencies
    /// + Testbar utan mocks (bara skicka in en List<Claim>)
    /// + Clean Architecture compliance
    /// 
    /// Nackdelar:
    /// - Application Service måste "veta" att hämta alla claims
    /// - Mindre encapsulation (logiken vet inte hur den får sin data)
    /// 
    /// DISKUSSIONSFRÅGA:
    /// - Är denna approach bättre än repository in domain service?
    /// - Pragmatism vs purism - var går gränsen?
    /// - Vad händer om vi har miljontals claims? Performance?
    /// </summary>
    /// <param name="newClaim">Ny claim att kontrollera</param>
    /// <param name="existingClaims">Alla existerande claims i systemet</param>
    /// <returns>True om misstänkt mönster upptäcks</returns>
    public Task<bool> IsSuspiciousPattern(Claim newClaim, IEnumerable<Claim> existingClaims)
    {
        // BR5 gäller bara fordonsskador
        if (newClaim is not VehicleClaim newVehicleClaim)
            return Task.FromResult(false);

        // Räkna antal claims för samma registreringsnummer inom 90 dagar
        const int lookbackDays = 90;
        const int suspiciousThreshold = 3;

        var recentClaimsCount = existingClaims
            .OfType<VehicleClaim>() // Bara fordonsskador
            .Where(c => c.RegistrationNumber == newVehicleClaim.RegistrationNumber) // Samma reg.nr
            .Where(c => c.AgeInDays() <= lookbackDays) // Inom 90 dagar
            .Count();

        // Om 3+ tidigare claims + denna nya = misstänkt
        bool isSuspicious = recentClaimsCount >= suspiciousThreshold;

        return Task.FromResult(isSuspicious);
    }

    // FRAMTIDA UTÖKNING: Fler business rules
    //
    // public bool RequiresDocumentation(Claim claim)
    // {
    //     // Vissa claim types behöver extra dokumentation
    // }
    //
    // public decimal CalculateDeductible(Claim claim)
    // {
    //     // Beräkna självrisk baserat på claim type och amount
    // }
    //
    // public bool IsEligibleForFastTrack(Claim claim)
    // {
    //     // Små belopp, inga red flags = snabb handläggning
    // }
}

// REFLECTION: Domain Service Design
//
// Denna klass samlar BR1, BR2, BR5 (och potentiellt fler)
// 
// Fördelar med centralisering:
// + Lätt att hitta alla regler
// + Enkel översikt
// + Återanvändbar
// + Testbar
//
// Men vad händer om vi får 50+ rules?
// Då kanske vi vill dela upp:
//
// - VehicleClaimBusinessRules
// - PropertyClaimBusinessRules
// - TravelClaimBusinessRules
// - FraudDetectionRules
// - EscalationRules
//
// DISKUSSIONSFRÅGOR:
// - När är en Domain Service för stor?
// - Hur organiserar man många business rules?
// - Skulle Specification pattern vara bättre?
//
// SPECIFICATION PATTERN EXEMPEL:
// <code>
// public interface ISpecification<T>
// {
//     bool IsSatisfiedBy(T entity);
// }
//
// public class LateReportSpecification : ISpecification<Claim>
// {
//     public bool IsSatisfiedBy(Claim claim)
//     {
//         return claim is VehicleClaim && claim.AgeInDays() > 30;
//     }
// }
// </code>
//
// Trade-offs:
// + Mycket flexibelt (kan kombinera specs: AND, OR, NOT)
// + SOLID compliant (each spec = SRP)
// - Mer komplext
// - Många små klasser
// - Potential over-engineering för simpla use cases
//
// För vår scope: ClaimBusinessRules är lagom.
// Men diskutera alternatives!
