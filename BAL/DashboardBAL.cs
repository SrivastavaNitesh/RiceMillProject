using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public class DashboardBAL(IConfiguration configuration)
{
    private readonly DashboardDAL _dal = new(configuration);

    public DashboardStats GetAdminDashboard(int officeId, DateTime fromDate, DateTime toDate)
    {
        if (officeId <= 0) throw new ArgumentException("A valid company is required for the dashboard.", nameof(officeId));
        if (fromDate.Date > toDate.Date) throw new ArgumentException("From date cannot be after to date.");
        if ((toDate.Date - fromDate.Date).TotalDays > 366) throw new ArgumentException("Dashboard date range cannot exceed 366 days.");
        return _dal.GetAdminDashboard(officeId, fromDate.Date, toDate.Date);
    }
}
