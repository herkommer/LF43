namespace ClaimApp.Application.Exceptions;

/// <summary>
/// Exception som kastas när en business rule violation upptäcks
/// 
/// VARFÖR CUSTOM EXCEPTION?
/// 
/// 1. Semantic clarity - tydligt att det är en business rule issue
/// 2. Catch specifikt - kan hantera business errors annorlunda än system errors
/// 3. Rule identification - RuleCode för logging/telemetry
/// 
/// ANVÄNDNING:
/// <code>
/// if (tooLate)
/// {
///     throw new BusinessRuleViolationException(
///         "Reseskador måste rapporteras inom 14 dagar",
///         "BR3");
/// }
/// 
/// // I UI:
/// try
/// {
///     await claimService.CreateClaim(claim);
/// }
/// catch (BusinessRuleViolationException ex)
/// {
///     ShowUserFriendlyError(ex.Message); // Användarvänligt
/// }
/// catch (Exception ex)
/// {
///     LogError(ex);
///     ShowGenericError(); // System error
/// }
/// </code>
/// 
/// ALTERNATIV: Result Pattern
/// 
/// Istället för exceptions:
/// <code>
/// public record Result<T>
/// {
///     public bool IsSuccess { get; init; }
///     public T? Value { get; init; }
///     public string Error { get; init; }
/// }
/// 
/// public async Task<Result<Claim>> CreateClaim(Claim claim)
/// {
///     if (violated)
///         return new Result<Claim> { IsSuccess = false, Error = "..." };
///     
///     var saved = await _repository.Save(claim);
///     return new Result<Claim> { IsSuccess = true, Value = saved };
/// }
/// </code>
/// 
/// TRADE-OFFS:
/// 
/// Exceptions:
/// + Idiomatisk C# (exceptions för exceptional cases)
/// + Stack trace (bra för debugging)
/// + Flow control (break execution immediately)
/// - Performance overhead (exceptions är "dyra")
/// - Kan missbrukas för flow control
/// 
/// Result Pattern:
/// + Explicit error handling (måste checka IsSuccess)
/// + No exceptions = better performance
/// + Functional programming style
/// - Verbositet (mer kod för error checking)
/// - Inte idiomatisk C# (mer vanligt i F#/Rust)
/// 
/// DISKUSSIONSFRÅGOR:
/// - Är business rule violations "exceptional" nog för exceptions?
/// - När är Result pattern bättre?
/// - Hur påverkar det användarupplevelsen?
/// </summary>
public class BusinessRuleViolationException : Exception
{
    /// <summary>
    /// Kod för business rule (t.ex. "BR3", "BR5")
    /// Används för:
    /// - Logging och telemetry
    /// - Gruppera errors i monitoring
    /// - A/B testing av regeländringar
    /// </summary>
    public string RuleCode { get; }

    /// <summary>
    /// Skapar exception med meddelande och rule code
    /// </summary>
    /// <param name="message">User-friendly error message</param>
    /// <param name="ruleCode">Business rule identifier (optional)</param>
    public BusinessRuleViolationException(string message, string ruleCode = "")
        : base(message)
    {
        RuleCode = ruleCode;
    }

    /// <summary>
    /// Skapar exception med meddelande, rule code och inner exception
    /// </summary>
    public BusinessRuleViolationException(string message, string ruleCode, Exception innerException)
        : base(message, innerException)
    {
        RuleCode = ruleCode;
    }
}
