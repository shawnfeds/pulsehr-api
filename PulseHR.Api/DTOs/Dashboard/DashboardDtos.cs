namespace PulseHR.Api.DTOs.Dashboard;

public class AdminDashboardDto
{
    public int TotalEmployees { get; set; }
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int PendingLeaves { get; set; }
    public int TotalDepartments { get; set; }
}

public class EmployeeDashboardDto
{
    public decimal HoursThisMonth { get; set; }
    public decimal LeavesTaken { get; set; }
    public int ActiveProjects { get; set; }
    public int AttendanceRate { get; set; }
}
