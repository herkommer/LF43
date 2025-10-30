namespace ClaimApp.Domain.ValueObjects;

/// <summary>
/// Value Object för datumintervall (start/slutdatum för resa)
/// 
/// VARFÖR VALUE OBJECT?
/// - Encapsulation: Start/End hör ihop logiskt
/// - Validation: End kan inte vara före Start
/// - Convenience methods: DurationInDays() etc
/// - Immutability: Kan inte ändra efter skapande
/// 
/// ALTERNATIV:
/// - Två separata DateTime properties (StartDate, EndDate)
///   Problem: Ingen garanti att End > Start
///   Problem: Måste validera överallt där de används
/// 
/// - Tuple (DateTime Start, DateTime? End)
///   Problem: Ingen validation
///   Problem: Ingen self-documenting methods
/// 
/// DISKUSSIONSFRÅGOR:
/// - Är detta over-engineering för bara 2 dates?
/// - När är värdet för encapsulation större än kostnaden?
/// - Hur skulle ni hantera "pågående resa" (ingen EndDate)?
/// </summary>
public sealed class DateRange
{
    public DateTime Start { get; }
    public DateTime? End { get; }

    /// <summary>
    /// Skapar ett datumintervall med validering
    /// 
    /// Regler:
    /// - Start är required
    /// - End är optional (för pågående resor)
    /// - Om End finns måste den vara >= Start
    /// 
    /// Exempel:
    /// new DateRange(2024-01-01, 2024-01-10) ✅
    /// new DateRange(2024-01-01, null) ✅ Pågående resa
    /// new DateRange(2024-01-10, 2024-01-01) ❌ End före Start
    /// </summary>
    public DateRange(DateTime start, DateTime? end = null)
    {
        if (end.HasValue && end.Value < start)
            throw new ArgumentException(
                "Slutdatum kan inte vara före startdatum",
                nameof(end));

        Start = start;
        End = end;
    }

    /// <summary>
    /// Beräknar reselängd i dagar
    /// Om resan pågår (End = null) använd aktuellt datum
    /// 
    /// Business use case:
    /// - Reseskador måste rapporteras inom 14 dagar efter hemkomst (BR3)
    /// - Denna metod används för att validera reporting deadline
    /// </summary>
    public int DurationInDays()
    {
        var endDate = End ?? DateTime.Now;
        return (endDate - Start).Days + 1; // +1 för att inkludera båda dagar
    }

    /// <summary>
    /// Kontrollerar om resan är avslutad
    /// Används för att avgöra om rapportering är möjlig
    /// </summary>
    public bool IsCompleted => End.HasValue;

    /// <summary>
    /// Kontrollerar om resan pågår just nu
    /// </summary>
    public bool IsOngoing => !End.HasValue || End.Value >= DateTime.Today;

    // VALUE EQUALITY

    public override bool Equals(object? obj)
    {
        return obj is DateRange other &&
               Start == other.Start &&
               End == other.End;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }

    public override string ToString()
    {
        return End.HasValue
            ? $"{Start:yyyy-MM-dd} till {End:yyyy-MM-dd}"
            : $"{Start:yyyy-MM-dd} till pågående";
    }

    // Equality operators

    public static bool operator ==(DateRange? left, DateRange? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(DateRange? left, DateRange? right)
    {
        return !(left == right);
    }

    // DISKUSSIONSPUNKT: Alternative design
    // Skulle vi kunna använda C# 9 Records istället?
    // 
    // public record DateRange(DateTime Start, DateTime? End)
    // {
    //     public DateRange(DateTime start, DateTime? end = null)
    //     {
    //         if (end.HasValue && end.Value < start)
    //             throw new ArgumentException(...);
    //         Start = start;
    //         End = end;
    //     }
    //     
    //     public int DurationInDays() { ... }
    // }
    //
    // Trade-offs:
    // + Mindre kod (value equality gratis)
    // + Modern C# syntax
    // - Lite mindre explicit
    // - Mindre control över implementation
}
