using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HManagSys.Attributes
{
    /// <summary>
    /// Attribut pour restreindre l'accès aux SuperAdmins uniquement
    /// </summary>
    public class SuperAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentRole = context.HttpContext.Session.GetString("CurrentRole");

            if (currentRole != "SuperAdmin")
            {
                // Pour les contrôleurs retournant JSON (AJAX)
                if (context.HttpContext.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                    context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Accès refusé. Droits SuperAdmin requis."
                    });
                }
                else
                {
                    // Pour les vues normales
                    var controller = context.Controller as Controller;
                    controller?.TempData.Add("ErrorMessage", "Accès refusé. Droits SuperAdmin requis.");
                    context.Result = new RedirectToActionResult("Index", "Dashboard", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Attribut pour s'assurer que l'utilisateur est authentifié
    /// </summary>
    public class RequireAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Attribut générique pour vérifier un rôle spécifique
    /// </summary>
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;
        private readonly string _redirectAction;
        private readonly string _redirectController;

        public RequireRoleAttribute(string role, string redirectAction = "Index", string redirectController = "Dashboard")
        {
            _allowedRoles = new[] { role };
            _redirectAction = redirectAction;
            _redirectController = redirectController;
        }

        public RequireRoleAttribute(string[] roles, string redirectAction = "Index", string redirectController = "Dashboard")
        {
            _allowedRoles = roles;
            _redirectAction = redirectAction;
            _redirectController = redirectController;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentRole = context.HttpContext.Session.GetString("CurrentRole");

            if (string.IsNullOrEmpty(currentRole) || !_allowedRoles.Contains(currentRole))
            {
                var allowedRolesList = string.Join(" ou ", _allowedRoles);
                var errorMessage = $"Accès refusé. Rôle requis : {allowedRolesList}";

                // Gérer les requêtes AJAX
                if (context.HttpContext.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                    context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new { success = false, message = errorMessage });
                }
                else
                {
                    var controller = context.Controller as Controller;
                    controller?.TempData.Add("ErrorMessage", errorMessage);
                    context.Result = new RedirectToActionResult(_redirectAction, _redirectController, null);
                }
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Attribut pour s'assurer qu'un utilisateur ne peut pas agir sur lui-même
    /// </summary>
    public class PreventSelfActionAttribute : ActionFilterAttribute
    {
        private readonly string _userIdParameterName;

        public PreventSelfActionAttribute(string userIdParameterName = "userId")
        {
            _userIdParameterName = userIdParameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentUserId = context.HttpContext.Session.GetInt32("UserId");

            // Récupérer l'ID de l'utilisateur cible depuis les paramètres
            var targetUserId = 0;

            // Vérifier dans les paramètres de route
            if (context.ActionArguments.ContainsKey(_userIdParameterName))
            {
                targetUserId = (int)context.ActionArguments[_userIdParameterName];
            }

            if (currentUserId == targetUserId)
            {
                var errorMessage = "Vous ne pouvez pas effectuer cette action sur votre propre compte.";

                // Gérer les requêtes AJAX
                if (context.HttpContext.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                    context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
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

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Attribut pour vérifier l'accès à un centre spécifique
    /// </summary>
    public class RequireCenterAccessAttribute : ActionFilterAttribute
    {
        private readonly string _centerIdParameterName;

        public RequireCenterAccessAttribute(string centerIdParameterName = "centerId")
        {
            _centerIdParameterName = centerIdParameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentCenterId = context.HttpContext.Session.GetInt32("CurrentCenterId");
            var currentRole = context.HttpContext.Session.GetString("CurrentRole");

            // SuperAdmin a accès à tous les centres
            if (currentRole == "SuperAdmin")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Récupérer l'ID du centre cible
            var targetCenterId = 0;
            if (context.ActionArguments.ContainsKey(_centerIdParameterName))
            {
                targetCenterId = (int)context.ActionArguments[_centerIdParameterName];
            }

            // Vérifier l'accès au centre
            if (currentCenterId != targetCenterId)
            {
                var errorMessage = "Accès refusé à ce centre hospitalier.";

                if (context.HttpContext.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                    context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new { success = false, message = errorMessage });
                }
                else
                {
                    var controller = context.Controller as Controller;
                    controller?.TempData.Add("ErrorMessage", errorMessage);
                    context.Result = new RedirectToActionResult("Index", "Dashboard", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}