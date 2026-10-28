using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public class SupervisorDashboardBAL(IConfiguration configuration)
{
    private readonly SupervisorDashboardDAL _dal = new(configuration);

    public SupervisorDashboard GetDashboard(int supervisorId, int officeId)
    {
        if (supervisorId <= 0) throw new ArgumentException("Valid supervisor login is required.", nameof(supervisorId));
        if (officeId <= 0) throw new ArgumentException("Supervisor company mapping is required.", nameof(officeId));
        return _dal.GetDashboard(supervisorId, officeId);
    }
}
