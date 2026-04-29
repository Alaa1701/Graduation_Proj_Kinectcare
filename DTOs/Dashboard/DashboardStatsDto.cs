namespace KinectCare.API.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalChildren { get; set; }
    public int TotalSpecialists { get; set; }
    public int TotalParents { get; set; }
    public int ActiveSessions { get; set; }
    public int CompletedSessions { get; set; }
    public double AvgProgressScore { get; set; }
    public List<RecentChildDto> RecentChildren { get; set; } = new();
}

public class RecentChildDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SpecialistName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}