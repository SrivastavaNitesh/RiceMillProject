using RiceMillProject.DAL;using RiceMillProject.Models;
namespace RiceMillProject.BAL;
public class MethDashboardBAL(IConfiguration configuration){private readonly MethDashboardDAL dal=new(configuration);public MethDashboard Get(int personId)=>dal.Get(personId);}
