namespace TrainingManagement.Api.Dtos.Dashboard;

public sealed class DashboardStatsDto
{
    public int UpcomingCourses { get; set; }

    public int ActiveRequests { get; set; }

    public int ValidCertificates { get; set; }

    public int PendingApprovals { get; set; }

    public int HandledThisWeek { get; set; }

    public int OverdueRequests { get; set; }

    public int PendingFilings { get; set; }

    public int PendingSignIn { get; set; }

    public int PendingCertificates { get; set; }

    public int TotalCourses { get; set; }
}
