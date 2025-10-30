using System.Text.RegularExpressions;

namespace ClaimApp.Domain.ValueObjects;

/// <summary>
/// Value Object för svenskt registreringsnummer
/// 
/// VARFÖR VALUE OBJECT?
/// 1. Encapsulation - All validering på ett ställe
/// 2. Immutability - Kan inte ändras efter skapande
/// 3. Value equality - Jämförs på innehåll, inte referens
/// 4. Self-validating - Garanterat korrekt om objektet existerar
/// 
/// ALTERNATIV:
/// - String property: Enklare men måste valideras överallt där den används
/// - Data Annotations: [RegularExpression] på property - men validering sker sent
/// - Record: Kortare syntax men mindre control över validation logic
/// 
/// TRADE-OFFS:
/// + Garanti för korrekthet överallt i systemet
/// + Ingen glömmer att validera
/// + Centraliserad normalise ring
/// - Mer kod än en enkel string
/// - Lite overhead (extra objekt)
/// 
/// DISKUSSIONSFRÅGOR:
/// - Är 50 rader kod "worth it" för att garantera korrekthet?
/// - Vad händer om vi också behöver internationella reg.nummer?
/// - När är Value Objects over-engineering?
/// </summary>
public sealed class RegistrationNumber
{
    private readonly string _value;

    /// <summary>
    /// Skapar ett svenskt registreringsnummer med validering och normalisering
    /// 
    /// Svenskt format: ABC123 eller ABC12D
    /// - 3 bokstäver
    /// - 2 siffror
    /// - 1 bokstav eller siffra
    /// 
    /// Normalisering:
    /// - Tar bort mellanslag och bindestreck
    /// - Konverterar till versaler
    /// 
    /// Exempel:
    /// "abc 123" -> "ABC123" ✅
    /// "ABC-123" -> "ABC123" ✅
    /// "abc12d" -> "ABC12D" ✅
    /// "INVALID" -> ArgumentException ❌
    /// </summary>
    public RegistrationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Registreringsnummer får inte vara tomt",
                nameof(value));

        // Normalisera: Ta bort whitespace och bindestreck, uppercase
        var normalized = value
            .Replace(" ", "")
            .Replace("-", "")
            .ToUpperInvariant();

        if (!IsValidSwedishFormat(normalized))
            throw new ArgumentException(
                $"Ogiltigt svenskt registreringsnummer: '{value}'. " +
                $"Förväntat format: ABC123 eller ABC12D",
                nameof(value));

        _value = normalized;
    }

    /// <summary>
    /// Validerar svenskt registreringsnummer format
    /// Pattern: 3 bokstäver, 2 siffror, 1 bokstav/siffra
    /// </summary>
    private static bool IsValidSwedishFormat(string value)
    {
        // Regex: ^[A-Z]{3}\d{2}[A-Z\d]$
        // ^ = start of string
        // [A-Z]{3} = exactly 3 uppercase letters
        // \d{2} = exactly 2 digits
        // [A-Z\d] = one uppercase letter OR digit
        // $ = end of string
        return Regex.IsMatch(value, @"^[A-Z]{3}\d{2}[A-Z\d]$");
    }

    /// <summary>
    /// Det normaliserade värdet (ABC123)
    /// </summary>
    public string Value => _value;

    // VALUE EQUALITY - Jämför på innehåll, inte referens

    /// <summary>
    /// Value Objects jämförs på innehåll
    /// 
    /// Exempel:
    /// var r1 = new RegistrationNumber("ABC123");
    /// var r2 = new RegistrationNumber("ABC123");
    /// 
    /// ReferenceEquals(r1, r2) -> false (olika objekt i minnet)
    /// r1.Equals(r2) -> true (samma värde)
    /// 
    /// Detta gör att Value Objects fungerar korrekt i collections:
    /// - HashSet&lt;RegistrationNumber&gt;
    /// - Dictionary&lt;RegistrationNumber, X&gt;
    /// - LINQ Distinct(), Contains() etc
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is RegistrationNumber other && _value == other._value;
    }

    /// <summary>
    /// GetHashCode måste vara konsistent med Equals
    /// Samma value = samma hash code
    /// </summary>
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    /// User-friendly string representation
    /// </summary>
    public override string ToString() => _value;

    // OPTIONAL: Operators för convenience

    /// <summary>
    /// Equality operator för natural syntax
    /// Usage: if (regNr1 == regNr2)
    /// </summary>
    public static bool operator ==(RegistrationNumber? left, RegistrationNumber? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// Usage: if (regNr1 != regNr2)
    /// </summary>
    public static bool operator !=(RegistrationNumber? left, RegistrationNumber? right)
    {
        return !(left == right);
    }

    // NOTE: Sealed class förhindrar inheritance
    // Value Objects ska INTE ha subklasser - de är final/sealed
    // Annars kan man bryta Liskov Substitution Principle
}
