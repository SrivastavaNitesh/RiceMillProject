using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL;

public class OrderReturnBAL(IConfiguration configuration)
{
    private readonly OrderReturnDAL dal = new(configuration);
    public OrderReturnPage GetPage(int officeId)=>dal.GetPage(officeId);
    public OrderReturnDetails? GetDetails(int id,int officeId)=>dal.GetDetails(id,officeId);
    public int Create(CreateOrderReturnInput model,int userId,int officeId)=>dal.Create(model,userId,officeId);
    public void Decide(DecideOrderReturnInput model,int userId,int officeId)=>dal.Decide(model,userId,officeId);
    public int CreateDispatch(CreateReturnDispatchInput model,int userId,int officeId)=>dal.CreateDispatch(model,userId,officeId);
    public int SaveMovement(SaveReturnMovementInput model,int userId,int officeId)=>dal.SaveMovement(model,userId,officeId);
    public void SaveWeight(SaveReturnWeightInput model,int userId,int officeId)=>dal.SaveWeight(model,userId,officeId);
    public void CompleteOutward(int dispatchId,int userId,int officeId)=>dal.CompleteOutward(dispatchId,userId,officeId);
}
