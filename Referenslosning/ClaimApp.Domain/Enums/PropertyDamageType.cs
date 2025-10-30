namespace ClaimApp.Domain.Enums;

/// <summary>
/// Specifika skadetyper för egendomsskador
/// 
/// Design rationale:
/// - Domain-specific taxonomy (affärsspråk)
/// - Kan användas för olika utbetalningsnivåer, franchises etc
/// - Möjliggör business rules per damage type
/// 
/// Diskussionsfrågor:
/// - Är detta för specifikt? Borde det vara konfigurerbart?
/// - Vad händer om affären vill lägga till nya typer dynamiskt?
/// - Skulle detta vara bättre som en lookup table i databas?
/// </summary>
public enum PropertyDamageType
{
    Fire = 0,
    Water = 1,
    Theft = 2,
    Vandalism = 3
}
