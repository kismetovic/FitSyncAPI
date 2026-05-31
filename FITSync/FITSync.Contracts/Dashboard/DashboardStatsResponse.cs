namespace FITSync.Contracts.Dashboard;

public class DashboardStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalTrainings { get; set; }
    public int TotalReservations { get; set; }
    public decimal TotalRevenue { get; set; }
}
