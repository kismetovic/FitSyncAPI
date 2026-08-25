namespace FITSync.Domain.Enums;

/// <summary>
/// Signals collected for the recommender. Each value is weighted differently when
/// scoring candidate trainings - see docs/RECOMMENDER.md.
/// </summary>
public enum UserActionType
{
    ViewedTraining = 0,
    SearchedTraining = 1,
    ReservedTraining = 2,
    CancelledTraining = 3,
    ReviewedTraining = 4,
    CompletedTraining = 5
}
