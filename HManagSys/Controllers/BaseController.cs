using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Base controller simplifié - Plus de vérifications manuelles !
/// Les attributs gèrent tout automatiquement
/// </summary>
public abstract class BaseController : Controller
{
    // Propriétés utilitaires
    protected int? CurrentUserId => HttpContext.Session.GetInt32("UserId");
    protected int? CurrentCenterId => HttpContext.Session.GetInt32("CurrentCenterId");
    protected string? CurrentRole => HttpContext.Session.GetString("CurrentRole");
    protected string? CurrentCenterName => HttpContext.Session.GetString("CurrentCenterName");
    protected string? CurrentUserName => HttpContext.Session.GetString("UserName");

    // Propriétés booléennes pour faciliter l'usage
    protected bool IsSuperAdmin => CurrentRole == "SuperAdmin";
    protected bool IsMedicalStaff => CurrentRole == "MedicalStaff" || IsSuperAdmin;
    protected bool IsAuthenticated => CurrentUserId.HasValue;

    // Helpers pour les vues
    protected dynamic SetCurrentUser()
    {
        ViewBag.CurrentUserId = CurrentUserId;
        ViewBag.CurrentCenterId = CurrentCenterId;
        ViewBag.CurrentRole = CurrentRole;
        ViewBag.CurrentCenterName = CurrentCenterName;
        ViewBag.CurrentUserName = CurrentUserName;
        ViewBag.IsSuperAdmin = IsSuperAdmin;
        ViewBag.IsMedicalStaff = IsMedicalStaff;
        return ViewBag;
    }

    /// <summary>
    /// Méthode OnActionExecuting pour initialiser ViewBag automatiquement
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        SetCurrentUser();
        base.OnActionExecuting(context);
    }

    // Plus de méthodes CheckSuperAdminAccess() ! 
    // Les attributs s'en occupent automatiquement
}