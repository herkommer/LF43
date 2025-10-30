namespace ClaimApp.Domain.Enums;

/// <summary>
/// Status för en skadeanmälan genom hela lifecycle
/// 
/// Design rationale:
/// - Enum för compile-time safety (kan inte ha typos som med strings)
/// - Explicit int values för framtida database persistence
/// - Starter med Pending som default (0)
/// 
/// Diskussionsfrågor:
/// - Varför enum över string constants?
/// - Vad händer om vi behöver dynamiska statuses från databas?
/// - Är detta extensible när affärskrav ändras?
/// </summary>
public enum ClaimStatus
{
    /// <summary>
    /// Nyligen skapad, väntar på handläggning
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Godkänd för utbetalning
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Avslagen
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Kräver manuell granskning (t.ex. sen rapportering)
    /// Triggad av BR1: >30 dagar för fordonsskador
    /// </summary>
    RequiresManualReview = 3,

    /// <summary>
    /// Eskalerad till senior handläggare
    /// Triggad av BR2: Höga belopp >100k
    /// </summary>
    Escalated = 4
}
