using System.Security.Claims;

namespace RiceMillProject.Models;

public static class UserAccess
{
    public const int GateManUserTypeId = 1;
    public const int WeightManUserTypeId = 2;
    public const int SupervisorUserTypeId = 3;
    public const int AdminUserTypeId = 4;
    public const int LabTechnicianUserTypeId = 5; 
    public const int MethUserTypeId = 6;
    public static bool IsMethUser(this ClaimsPrincipal user) =>
    !user.IsAdminUser() &&
    (
        user.UserTypeId() == MethUserTypeId ||
        RoleKey(user) == "meth"
    );
    public static int UserTypeId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("UserTypeId")?.Value, out var id) ? id : 0;

    public static int CompanyId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst("CompanyId")?.Value, out var id) ? id : 0;

    public static bool IsAdminUser(this ClaimsPrincipal user) =>
        user.UserTypeId() == AdminUserTypeId || user.IsInRole("Admin");

    public static bool IsGatemanUser(this ClaimsPrincipal user) =>
        !user.IsAdminUser() && (user.UserTypeId() == GateManUserTypeId || RoleKey(user) == "gateman");

    public static bool IsWeightmanUser(this ClaimsPrincipal user) =>
        !user.IsAdminUser() && (user.UserTypeId() == WeightManUserTypeId || RoleKey(user) is "weightman" or "weighbridgeman");

    public static bool IsSupervisorUser(this ClaimsPrincipal user) =>
        !user.IsAdminUser() && (user.UserTypeId() == SupervisorUserTypeId || RoleKey(user) == "supervisor");

    public static bool IsLabUser(this ClaimsPrincipal user) =>
        !user.IsAdminUser() && (user.UserTypeId() == LabTechnicianUserTypeId || RoleKey(user) == "labtechnician");

    private static string RoleKey(ClaimsPrincipal user) =>
        (user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
}
