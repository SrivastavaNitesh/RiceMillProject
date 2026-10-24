using System.Collections;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RiceMillProject.DAL;

namespace RiceMillProject.Models;

// Protect legacy submission routes as well as the dedicated correction editor.
public class RecordWorkflowFilter(IConfiguration configuration) : IAsyncActionFilter, IResultFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next)
    {
        var controller=Convert.ToString(context.RouteData.Values["controller"]);
        var action=Convert.ToString(context.RouteData.Values["action"]);
        var user=context.HttpContext.User;
        var restricted=HidePartyMobile(user);
        if(restricted && controller=="Person" && action is "Create" or "Edit" or "Delete")
        {context.Result=new ForbidResult();return;}
        if(HttpMethods.IsPost(context.HttpContext.Request.Method) && controller is "Unload" or "Meth" or "GateEntry")
        {
            string? module=null,id=null;
            object? Arg(string key)=>context.ActionArguments.TryGetValue(key,out var value)?value:null;
            if(Arg("model") is SupervisorUnloadActionViewModel field && field.CategoryRows?.Any(r=>
                (r.CategoryId>0||r.ItemId>0||r.LocationId>0||r.BagTypeId>0||r.BagCount!=0) &&
                (r.CategoryId<=0||r.ItemId<=0||r.LocationId<=0||r.BagTypeId<=0||r.BagCount<=0))==true)
            {context.Result=new BadRequestObjectResult("Every entered material row must have category, item, location, bag type and positive bag count.");return;}
            if(Arg("unload") is UnloadTransaction labour && labour.WorkerRows?.Any(r=>r.BagCount<0||r.PerBagCharge<0||
                (r.BagCount>0 && (r.WorkerId<=0||r.BagTypeId.GetValueOrDefault()<=0||r.WorkType is not("Load" or "Unload" or "Loading" or "Unloading"))))==true)
            {context.Result=new BadRequestObjectResult("Enter a valid worker, work type, bag type, positive bags and non-negative charge.");return;}
            if(controller=="Unload")
            {
                module=action is "SupervisorAction" or "Complete" or "Verify"?"Supervisor":action=="SupervisorMethWork"?"Labour":null;
                id=Arg("id")?.ToString()??Arg("UnloadId")?.ToString()??(Arg("model") as SupervisorUnloadActionViewModel)?.UnloadId.ToString()
                    ??(Arg("model") as UnloadTransaction)?.UnloadId.ToString()??(Arg("unload") as UnloadTransaction)?.UnloadId.ToString();
            }
            else if(controller=="Meth" && action=="ExecuteUnload") {module="Labour";id=(Arg("unload") as UnloadTransaction)?.UnloadId.ToString();}
            else if(controller=="GateEntry" && action is "WeightmanAction" or "Outbound")
            {module="Weight";id=(Arg("action") as WeightmanRstAction)?.RSTNumber??Arg("rstNumber")?.ToString()??Arg("id")?.ToString();}
            if(module!=null && id!=null)
            {
                var dal=new RecordEditDAL(configuration);
                if(module=="Supervisor" && action=="Verify" && !dal.Exists(module,id))module="Assignment";
                if(dal.Exists(module,id))
                {
                    int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier),out var userId);
                    var access=dal.Access(userId,module,id);
                    if(access==null || !access.CanEdit)
                    {context.Result=new ObjectResult("Editing is closed: finalized, expired, outward completed or another operator's entry."){StatusCode=403};return;}
                }
                else if(module=="Weight")
                {context.Result=new ObjectResult("This weighment has no valid original entry timestamp."){StatusCode=403};return;}
            }
        }
        var executed=await next();
        if(executed.Result is ViewResult viewResult && controller is "Unload" or "Meth")
        {
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier),out var actor);
            viewResult.ViewData["RecordActions"]=new RecordEditDAL(configuration).List(actor);
        }
    }
    public static bool HidePartyMobile(ClaimsPrincipal user) => !user.IsAdminUser() &&
        (user.IsWeightmanUser()||user.IsSupervisorUser()||user.IsMethUser()||user.FindFirst("PostId")?.Value is "2" or "3" or "6");
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if(!HidePartyMobile(context.HttpContext.User))return;
        if(context.Result is ViewResult view)
        {
            var visited=new HashSet<object>(ReferenceEqualityComparer.Instance);
            Scrub(view.Model,visited);
            foreach(var value in view.ViewData.Values)Scrub(value,visited);
        }
        else if(context.Result is JsonResult json && json.Value!=null)
        {
            var node=JsonSerializer.SerializeToNode(json.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
            ScrubJson(node);json.Value=node;
        }
        else if(context.Result is ObjectResult obj && obj.Value!=null && obj.Value is not string)
        {
            var node=JsonSerializer.SerializeToNode(obj.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
            ScrubJson(node);obj.Value=node;
        }
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
    public static void Scrub(object? value,HashSet<object> visited)
    {
        if(value==null||value is string||value.GetType().IsValueType||!visited.Add(value))return;
        if(value is DataTable table)
        {
            foreach(DataColumn col in table.Columns)
                if(col.ColumnName.Contains("Party",StringComparison.OrdinalIgnoreCase) && col.ColumnName.Contains("Mobile",StringComparison.OrdinalIgnoreCase))
                    foreach(DataRow row in table.Rows)row[col]="";
            if(table.Columns.Contains("PersonType")&&table.Columns.Contains("MobileNumber"))
                foreach(DataRow row in table.Rows)
                    if(Convert.ToString(row["PersonType"])?.ToLowerInvariant() is "8" or "party" or "supplier" or "kisan" or "farmer")row["MobileNumber"]="";
            return;
        }
        if(value is Person p && (p.PostId==8 || p.PersonType?.ToLowerInvariant() is "8" or "party" or "supplier" or "kisan" or "farmer"))p.MobileNumber="";
        if(value is IDictionary dictionary){foreach(var v in dictionary.Values)Scrub(v,visited);return;}
        if(value is IEnumerable enumerable){foreach(var v in enumerable)Scrub(v,visited);return;}
        if(value.GetType().Namespace?.StartsWith("RiceMillProject")==false)return;
        foreach(var prop in value.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(p=>p.CanRead&&p.GetIndexParameters().Length==0))
        {
            if(prop.CanWrite && prop.PropertyType==typeof(string) && prop.Name.Contains("Party",StringComparison.OrdinalIgnoreCase) && prop.Name.Contains("Mobile",StringComparison.OrdinalIgnoreCase))prop.SetValue(value,"");
            else Scrub(prop.GetValue(value),visited);
        }
    }
    private static void ScrubJson(JsonNode? node)
    {
        if(node is JsonObject obj)
        {
            var type=obj.FirstOrDefault(k=>k.Key.Equals("personType",StringComparison.OrdinalIgnoreCase)).Value?.ToString().ToLowerInvariant();
            bool party=type is "8" or "party" or "supplier" or "kisan" or "farmer";
            foreach(var key in obj.Select(k=>k.Key).ToArray())
            {
                if((key.Contains("party",StringComparison.OrdinalIgnoreCase)&&key.Contains("mobile",StringComparison.OrdinalIgnoreCase))||(party&&key.Equals("mobileNumber",StringComparison.OrdinalIgnoreCase)))obj[key]="";
                else ScrubJson(obj[key]);
            }
        }
        else if(node is JsonArray array)foreach(var v in array)ScrubJson(v);
    }
}
