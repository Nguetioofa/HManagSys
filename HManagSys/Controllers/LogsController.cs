using HManagSys.Models.ViewModels;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur pour la consultation des logs depuis l'interface web
    /// Le centre de surveillance de notre hôpital numérique
    /// </summary>
    public class LogsController : Controller
    {
        private readonly IApplicationLogger _appLogger;

        public LogsController(IApplicationLogger appLogger)
        {
            _appLogger = appLogger;
        }

        public async Task<IActionResult> Index(LogsViewModel? model = null)
        {
            model ??= new LogsViewModel();

            var logs = await _appLogger.GetLogsAsync(
                model.FromDate, model.ToDate,
                model.Category, model.Action,
                model.LogLevel, model.UserId,
                model.HospitalCenterId,
                model.PageIndex, model.PageSize);

            model.Logs = logs;
            return View(model);
        }

        public async Task<IActionResult> SystemErrors()
        {
            var unresolved = await _appLogger.GetUnresolvedErrorsAsync();
            return View(unresolved);
        }

        public async Task<IActionResult> UserActivity(int userId)
        {
            var logs = await _appLogger.GetUserActivityAsync(userId);
            return View(logs);
        }

        public async Task<IActionResult> CenterActivity(int centerId)
        {
            var logs = await _appLogger.GetCenterActivityAsync(centerId);
            return View(logs);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveError(Guid errorId, string? notes)
        {
            var currentUserId = GetCurrentUserId(); // Méthode à implémenter
            await _appLogger.MarkErrorResolvedAsync(errorId, currentUserId, notes);

            return Json(new { success = true });
        }

        // API pour récupérer les logs via AJAX
        [HttpGet]
        public async Task<JsonResult> GetLogsData(
            DateTime? from, DateTime? to, string? category,
            string? level, int page = 1)
        {
            Models.Enums.LogLevel? logLevel = string.IsNullOrEmpty(level) ? null : Enum.Parse<HManagSys.Models.Enums.LogLevel>(level);
            var logs = await _appLogger.GetLogsAsync(from, to, category,
                logLevel: logLevel, pageIndex: page);

            return Json(logs);
        }

        private int GetCurrentUserId()
        {
            // Implémentation pour récupérer l'ID de l'utilisateur connecté
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }
    }
}
