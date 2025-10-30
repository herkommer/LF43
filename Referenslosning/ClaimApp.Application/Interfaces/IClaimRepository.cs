using ClaimApp.Domain.Entities;

namespace ClaimApp.Application.Interfaces;

/// <summary>
/// Repository interface för skadeanmälningar
/// 
/// DESIGN DECISION: Specific repository (not generic)
/// 
/// Varför inte IRepository&lt;T&gt;?
/// + Specifika metoder (GetByRegistrationNumber)
/// + Tydligt interface (no confusion om vad som stöds)
/// + Lätt att mocka i tester
/// 
/// DISKUSSIONSFRÅGA:
/// - Generic repository - anti-pattern eller pragmatiskt?
/// - När är specific repositories bättre?
/// </summary>
public interface IClaimRepository
{
    /// <summary>
    /// Sparar en claim (create eller update)
    /// </summary>
    Task<Claim> Save(Claim claim);

    /// <summary>
    /// Hämtar claim by ID
    /// </summary>
    Task<Claim?> GetById(Guid id);

    /// <summary>
    /// Hämtar alla claims
    /// </summary>
    Task<IEnumerable<Claim>> GetAll();

    /// <summary>
    /// Hämtar fordonsskador för specifikt registreringsnummer
    /// Specifik metod - hade inte passat i generic repository
    /// </summary>
    Task<IEnumerable<Claim>> GetByRegistrationNumber(string registrationNumber);
}
