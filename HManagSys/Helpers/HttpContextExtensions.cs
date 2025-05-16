namespace HManagSys.Helpers;

public static class HttpContextExtensions
{
    public static string? GetCurrentRole(this HttpContext context)
    {
        return context.Session.GetString("CurrentRole");
    }

    public static bool IsSuperAdmin(this HttpContext context)
    {
        return RoleHelper.IsSuperAdmin(context.GetCurrentRole());
    }

    public static bool CanManageUsers(this HttpContext context)
    {
        return RoleHelper.CanManageUsers(context.GetCurrentRole());
    }
}
