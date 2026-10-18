using System.Security.Claims;

namespace RiceMillProject.Models;

public static class AdminWorkContext
{
    public const string RoleKey = "AdminWorkRole";
    public const string PersonKey = "AdminWorkPersonId";
    public const string UserKey = "AdminWorkUserId";
    public const string NameKey = "AdminWorkPersonName";

    public static int EffectivePersonId(this HttpContext context)
    {
        if (context.User.IsAdminUser())
            return context.Session.GetInt32(PersonKey) ?? 0;
        return int.TryParse(context.User.FindFirstValue("PersonId"), out var id) ? id : 0;
    }

    public static int EffectiveUserId(this HttpContext context)
    {
        if (context.User.IsAdminUser())
            return context.Session.GetInt32(UserKey) ?? 0;
        return int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }
}
