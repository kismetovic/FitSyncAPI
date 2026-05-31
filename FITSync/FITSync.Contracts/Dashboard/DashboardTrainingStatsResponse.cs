namespace FITSync.Contracts.Dashboard;

public class DashboardTrainingStatsResponse
{
    public int TrainingId { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public int ReservationsCount { get; set; }
    public DateTime? NextTerm { get; set; }
}
