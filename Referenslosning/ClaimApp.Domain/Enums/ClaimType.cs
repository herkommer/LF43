namespace ClaimApp.Domain.Enums;

/// <summary>
/// Typ av skadeanmälan
/// 
/// Design rationale:
/// - Enum möjliggör exhaustive pattern matching i switch statements
/// - Compiler varnar om vi missar ett case
/// - Self-documenting code (clear intent)
/// 
/// Alternativ approach:
/// - String discriminator: Mer flexibelt men ingen compile-time safety
/// - Type hierarchy utan enum: OCP-friendly men mer komplex
/// 
/// Trade-off:
/// - Enum = lätt att lägga till nytt case, men måste recompilera
/// - String = kan komma från config/DB, men kan ha typos
/// </summary>
public enum ClaimType
{
    Vehicle = 0,
    Property = 1,
    Travel = 2
}
