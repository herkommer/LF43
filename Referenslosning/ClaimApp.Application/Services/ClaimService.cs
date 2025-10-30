using ClaimApp.Application.Exceptions;
using ClaimApp.Application.Interfaces;
using ClaimApp.Domain.Entities;
using ClaimApp.Domain.Services;

namespace ClaimApp.Application.Services;

/// <summary>
/// Application Service för claim-relaterade use cases
/// 
/// ANSVAR:
/// 1. Orchestration - koordinerar domain services + repository
/// 2. Transaction boundaries - vad sparas tillsammans
/// 3. Application-level validation (BR3)
/// 4. Use case flow
/// 
/// INTE ANSVAR:
/// - Business logic (det är i Domain Layer)
/// - Data access details (det är i Infrastructure)
/// - UI logic (det är i Presentation)
/// </summary>
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _repository;
    private readonly ClaimBusinessRules _businessRules;

    public ClaimService(IClaimRepository repository, ClaimBusinessRules businessRules)
    {
        _repository = repository;
        _businessRules = businessRules;
    }

    /// <summary>
    /// Skapar ny skadeanmälan med all business rule validation
    /// 
    /// FLOW:
    /// 1. Validera BR3 (reporting deadline för travel)
    /// 2. Applicera BR1 (late report) om applicable
    /// 3. Applicera BR2 (high value) om applicable
    /// 4. Applicera BR5 (suspicious pattern) om applicable
    /// 5. Spara till repository
    /// 
    /// DISKUSSIONSPUNKT: Ordning av rules
    /// - Spelar det roll vilken ordning vi kör BR1, BR2, BR5?
    /// - Vad händer om flera rules triggar (kan ha flera statuses?)
    /// - I vår impl: Sista satt status vinner (potentiellt problem!)
    /// </summary>
    public async Task<Claim> CreateClaim(Claim claim)
    {
        // BR3: Rapporteringsfrist för reseskador (BLOCKING RULE)
        // Denna är i Application Layer för den FÖRHINDRAR skapande
        if (claim is TravelClaim travelClaim)
        {
            if (travelClaim.IsTravelCompleted())
            {
                var daysSinceReturn = travelClaim.DaysSinceReturn();
                if (daysSinceReturn.HasValue && daysSinceReturn.Value > 14)
                {
                    throw new BusinessRuleViolationException(
                        $"Reseskador måste rapporteras inom 14 dagar efter hemkomst. " +
                        $"Det har gått {daysSinceReturn.Value} dagar sedan resan avslutades.",
                        "BR3");
                }
            }
        }

        // BR1: Sen rapportering (FLAGGING RULE)
        if (_businessRules.RequiresManualReview(claim))
        {
            claim.UpdateStatus(Domain.Enums.ClaimStatus.RequiresManualReview);
        }

        // BR2: Höga belopp (FLAGGING RULE)
        if (_businessRules.RequiresEscalation(claim))
        {
            claim.UpdateStatus(Domain.Enums.ClaimStatus.Escalated);
        }

        // BR5: Misstänkt mönster (FLAGGING RULE)
        // Hämta alla existerande claims för pattern detection
        var existingClaims = await _repository.GetAll();
        if (await _businessRules.IsSuspiciousPattern(claim, existingClaims))
        {
            claim.UpdateStatus(Domain.Enums.ClaimStatus.RequiresManualReview);
        }

        // Spara till repository
        return await _repository.Save(claim);
    }

    public async Task<Claim?> GetClaimById(Guid id)
    {
        return await _repository.GetById(id);
    }

    public async Task<IEnumerable<Claim>> GetAllClaims()
    {
        return await _repository.GetAll();
    }

    public async Task<IEnumerable<Claim>> GetClaimsByRegistrationNumber(string registrationNumber)
    {
        return await _repository.GetByRegistrationNumber(registrationNumber);
    }
}
