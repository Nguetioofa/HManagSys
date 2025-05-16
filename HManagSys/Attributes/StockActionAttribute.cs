using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HManagSys.Attributes;


/// <summary>
/// Attribut pour les actions de stock
/// </summary>
public class StockActionAttribute : ActionFilterAttribute
{
    private readonly bool _requiresSuperAdmin;

    public StockActionAttribute(bool requiresSuperAdmin = false)
    {
        _requiresSuperAdmin = requiresSuperAdmin;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Authentification
        var userId = context.HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // Centre sélectionné
        var centerId = context.HttpContext.Session.GetInt32("CurrentCenterId");
        if (!centerId.HasValue)
        {
            context.Result = new RedirectToActionResult("SelectCenter", "Auth", null);
            return;
        }

        // Vérification rôle
        var role = context.HttpContext.Session.GetString("CurrentRole");
        if (_requiresSuperAdmin && role != "SuperAdmin")
        {
            var errorMessage = "Cette action nécessite des droits SuperAdmin.";
            if (IsAjaxRequest(context.HttpContext))
            {
                context.Result = new JsonResult(new { success = false, message = errorMessage });
            }
            else
            {
                var controller = context.Controller as Controller;
                controller?.TempData.Add("ErrorMessage", errorMessage);
                context.Result = new RedirectToActionResult("Index", "Stock", null);
            }
            return;
        }

        base.OnActionExecuting(context);
    }

    private bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
               context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
