// Attributs raccourcis pour faciliter l'utilisation
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HManagSys.Attributes
{
    /// <summary>
    /// Attribut raccourci pour MedicalStaff ou SuperAdmin
    /// </summary>
    public class MedicalStaffAttribute : RequireRoleAttribute
    {
        public MedicalStaffAttribute() : base(new[] { "MedicalStaff", "SuperAdmin" }) { }
    }

    /// <summary>
    /// Attribut pour les actions qui nécessitent d'être dans un centre
    /// </summary>
    public class RequireCurrentCenterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var currentCenterId = context.HttpContext.Session.GetInt32("CurrentCenterId");

            if (!currentCenterId.HasValue)
            {
                var controller = context.Controller as Controller;
                controller?.TempData.Add("ErrorMessage", "Veuillez sélectionner un centre hospitalier.");
                context.Result = new RedirectToActionResult("SelectCenter", "Auth", null);
            }

            base.OnActionExecuting(context);
        }
    }
}