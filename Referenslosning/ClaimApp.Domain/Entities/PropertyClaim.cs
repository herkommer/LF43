using ClaimApp.Domain.Enums;

namespace ClaimApp.Domain.Entities;

/// <summary>
/// Skadeanmälan för egendomsskador
/// 
/// SPECIFIKA KRAV (från krav.md):
/// - Adress där skadan inträffade - REQUIRED
/// - Typ av skada (Fire, Water, Theft, Vandalism) - REQUIRED
/// - Uppskattat värde på skadan - REQUIRED
/// 
/// BUSINESS RULES:
/// - BR2: Höga belopp (>100 000 kr) kräver eskalering
/// 
/// DESIGN: Medium complexity
/// - Mer fält än VehicleClaim
/// - Enum för damage type (domain-specific taxonomy)
/// - Decimal för money (finansiella beräkningar kräver precision)
/// </summary>
public class PropertyClaim : Claim
{
    /// <summary>
    /// Adress där skadan inträffade
    /// 
    /// DESIGN DECISION: String över Value Object
    /// 
    /// Rationale:
    /// - Ingen strikt validering behövs (fritext från användare)
    /// - Kan vara "Storgatan 1, 123 45 Stockholm" eller bara "Hemadress"
    /// - Inte värt komplexiteten av Value Object här
    /// 
    /// ALTERNATIV:
    /// - Address Value Object med Street, City, PostalCode
    ///   + Strukturerad data, lätt att sortera/söka
    ///   - Over-engineering för detta use case
    ///   - Användare kanske inte vet alla detaljer vid rapportering
    /// 
    /// DISKUSSIONSFRÅGA:
    /// - Om vi behöver integrera med kartAPI senare - behöver vi refaktorera?
    /// - Är "strängare är bättre" alltid sant?
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// Typ av skada (enum)
    /// 
    /// VARFÖR ENUM?
    /// - Begränsad set av värden (domain constraint)
    /// - Compile-time safety
    /// - Kan användas för olika business logic per typ
    ///   Exempel: Fire damage = alltid eskalera oavsett belopp
    /// 
    /// FRAMTIDA UTÖKNING:
    /// Olika franchises per damage type:
    /// - Fire: 0 kr (försäkring täcker allt)
    /// - Water: 5000 kr självrisk
    /// - Theft: 3000 kr självrisk
    /// </summary>
    public PropertyDamageType DamageType { get; private set; }

    /// <summary>
    /// Uppskattat värde på skadan i SEK
    /// 
    /// DESIGN DECISION: Decimal över double/float
    /// 
    /// Rationale: Financial calculations kräver precision
    /// - Decimal: Exact representation (no rounding errors)
    /// - Double: Floating point (potential rounding errors)
    /// 
    /// Exempel problem med double:
    /// <code>
    /// double value = 100000.01;
    /// bool isOver = value > 100000; // Kan ge fel resultat pga rounding!
    /// </code>
    /// 
    /// Med decimal: Inga sådana problem
    /// 
    /// Trade-off:
    /// + Precision (critical för pengar!)
    /// - Lite långsammare än double (negligible för vår use case)
    /// </summary>
    public decimal EstimatedValue { get; private set; }

    /// <summary>
    /// Skapar en egendomsskada med validering
    /// 
    /// Business rule enforcement:
    /// - Address required (not null/empty)
    /// - EstimatedValue > 0 (kan inte skada för 0 kr)
    /// 
    /// Exempel:
    /// <code>
    /// var claim = new PropertyClaim(
    ///     "Vattenläcka förstörde parkettgolv i vardagsrum",
    ///     DateTime.Today.AddDays(-5),
    ///     "Storgatan 15, 123 45 Stockholm",
    ///     PropertyDamageType.Water,
    ///     75000m // 75,000 kr
    /// );
    /// </code>
    /// </summary>
    public PropertyClaim(
        string description,
        DateTime reportedDate,
        string address,
        PropertyDamageType damageType,
        decimal estimatedValue)
        : base(description, reportedDate)
    {
        // Validera property-specifika fält
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException(
                "Adress måste anges för egendomsskador",
                nameof(address));

        if (estimatedValue <= 0)
            throw new ArgumentException(
                "Uppskattat värde måste vara större än 0",
                nameof(estimatedValue));

        // Sätt properties
        Address = address.Trim();
        DamageType = damageType;
        EstimatedValue = estimatedValue;

        // Sätt claim type
        ClaimType = ClaimType.Property;
    }

    // DISKUSSIONSPUNKT: Business logic här eller i Domain Service?
    //
    // Skulle vi kunna ha:
    //
    // public bool RequiresEscalation()
    // {
    //     return EstimatedValue > 100_000; // BR2
    // }
    //
    // TRADE-OFF:
    // + Encapsulation - logiken är "nära" datan
    // + Self-documenting - kan anropa claim.RequiresEscalation()
    // - Harder to find all business rules (sprids ut över entities)
    // - Less flexible - vad om regel ändras till att inkludera fler kriterier?
    //
    // I vår lösning: BR2 är i ClaimBusinessRules
    // Rationale: Centraliserad business logic, lätt att hitta och ändra

    // ALTERNATIV DESIGN: Money Value Object
    //
    // <code>
    // public class Money
    // {
    //     public decimal Amount { get; }
    //     public string Currency { get; } // "SEK", "USD"
    //     
    //     public Money(decimal amount, string currency = "SEK")
    //     {
    //         if (amount < 0) throw new ArgumentException(...);
    //         Amount = amount;
    //         Currency = currency;
    //     }
    //     
    //     public Money Add(Money other) { ... }
    //     public bool IsGreaterThan(Money other) { ... }
    // }
    // </code>
    //
    // TRADE-OFFS:
    // + Multi-currency support
    // + Encapsulated operations (no raw decimal math)
    // + Clearer intent (Money vs just decimal)
    // - Over-engineering för single-currency system
    // - Extra complexity
    //
    // Diskussionsfråga: När skulle Money Value Object vara rätt?
}
