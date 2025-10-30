using ClaimApp.Domain.Entities;

namespace ClaimApp.Application.Interfaces;

/// <summary>
/// Application Service interface för claim use cases
/// 
/// USE CASE-DRIVEN DESIGN:
/// Metoder mappas till user stories/use cases, inte CRUD operations
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// FR1: Registrera ny skadeanmälan
    /// Applicerar business rules (BR1, BR2, BR3, BR5)
    /// </summary>
    Task<Claim> CreateClaim(Claim claim);

    /// <summary>
    /// FR3: Hämta specifik skadeanmälan för detaljvy
    /// </summary>
    Task<Claim?> GetClaimById(Guid id);

    /// <summary>
    /// FR2: Lista alla skadeanmälningar
    /// </summary>
    Task<IEnumerable<Claim>> GetAllClaims();

    /// <summary>
    /// Query: Hitta alla claims för ett specifikt fordon
    /// </summary>
    Task<IEnumerable<Claim>> GetClaimsByRegistrationNumber(string registrationNumber);
}
