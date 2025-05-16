// Exemple d'attribut combiné pour des cas spécifiques

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HManagSys.Attributes;

/// <summary>
/// Attribut combiné pour les actions administratives sur les utilisateurs
/// Vérifie : SuperAdmin + pas sur soi-même + authentification
/// </summary>
public class AdminUserActionAttribute : ActionFilterAttribute
{
    private readonly string _userIdParameterName;

    public AdminUserActionAttribute(string userIdParameterName = "userId")
    {
        _userIdParameterName = userIdParameterName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // 1. Vérifier authentification
        var currentUserId = context.HttpContext.Session.GetInt32("UserId");
        if (!currentUserId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // 2. Vérifier SuperAdmin
        var currentRole = context.HttpContext.Session.GetString("CurrentRole");
        if (currentRole != "SuperAdmin")
        {
            var errorMessage = "Accès refusé. Droits SuperAdmin requis.";
            HandleError(context, errorMessage);
            return;
        }

        // 3. Vérifier pas action sur soi-même
        if (context.ActionArguments.ContainsKey(_userIdParameterName))
        {
            var targetUserId = (int)context.ActionArguments[_userIdParameterName];
            if (currentUserId == targetUserId)
            {
                var errorMessage = "Vous ne pouvez pas effectuer cette action sur votre propre compte.";
                HandleError(context, errorMessage);
                return;
            }
        }

        base.OnActionExecuting(context);
    }

    private void HandleError(ActionExecutingContext context, string errorMessage)
    {
        if (IsAjaxRequest(context.HttpContext))
        {
            context.Result = new JsonResult(new { success = false, message = errorMessage });
        }
        else
        {
            var controller = context.Controller as Controller;
            controller?.TempData.Add("ErrorMessage", errorMessage);
            context.Result = new RedirectToActionResult("Index", "Admin", null);
        }
    }

    private bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
               context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
