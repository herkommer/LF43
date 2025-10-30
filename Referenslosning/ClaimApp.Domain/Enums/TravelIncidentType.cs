namespace ClaimApp.Domain.Enums;

/// <summary>
/// Specifika incidenttyper för reseskador
/// 
/// Design rationale:
/// - Affärsterminologi direkt från domänen
/// - Kan mappa till olika försäkringsvillkor
/// - Möjliggör specialiserad handläggning per typ
/// 
/// Framtida utökning:
/// - FlightDelay (fördröjning vs cancellation)
/// - AccommodationIssues
/// - DocumentLoss (pass, visum)
/// </summary>
public enum TravelIncidentType
{
    LostLuggage = 0,
    FlightCancellation = 1,
    MedicalEmergency = 2
}
