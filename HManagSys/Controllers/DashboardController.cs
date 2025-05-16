using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models.ViewModels;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    /// <summary>
    /// Contrôleur de tableau de bord pour le Personnel Soignant
    /// Interface principale pour les opérations quotidiennes
    /// </summary>
    [RequireAuthentication]
    public class DashboardController : Controller
    {
        private readonly IApplicationLogger _appLogger;
        private readonly IAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IHospitalCenterRepository _hospitalCenterRepository;

        public DashboardController(
            IApplicationLogger appLogger,
            IAuthenticationService authService,
            IUserRepository userRepository,
            IHospitalCenterRepository hospitalCenterRepository)
        {
            _appLogger = appLogger;
            _authService = authService;
            _userRepository = userRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
        }

        /// <summary>
        /// Tableau de bord principal pour le personnel soignant
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var currentCenterId = HttpContext.Session.GetInt32("CurrentCenterId");
                var currentRole = HttpContext.Session.GetString("CurrentRole");

                if (!userId.HasValue || !currentCenterId.HasValue)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Récupérer les informations de l'utilisateur et du centre
                var user = await _userRepository.GetByIdAsync(userId.Value);
                var center = await _hospitalCenterRepository.GetByIdAsync(currentCenterId.Value);

                if (user == null || center == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Log de l'accès au tableau de bord
                await _appLogger.LogInfoAsync("Dashboard", "AccessedDashboard",
                    $"Accès au tableau de bord par {user.FirstName} {user.LastName}",
                    userId.Value, currentCenterId.Value);

                // Créer le modèle de vue pour le tableau de bord
                var dashboardModel = new DashboardViewModel
                {
                    User = user,
                    Center = center,
                    CurrentRole = currentRole ?? "Personnel Soignant",
                    WelcomeMessage = GetWelcomeMessage(user.FirstName),
                    // Ici, nous ajouterions les statistiques et données du tableau de bord
                    // QuickStats = await GetQuickStats(currentCenterId.Value),
                    // RecentActivities = await GetRecentActivities(userId.Value, currentCenterId.Value)
                };

                return View(dashboardModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "DashboardError",
                    "Erreur lors du chargement du tableau de bord",
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord";
                return RedirectToAction("Login", "Auth");
            }
        }

        /// <summary>
        /// API pour changer de centre via AJAX
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SwitchCenter(int centerId)
        {
            try
            {
                var sessionToken = HttpContext.Session.GetString("SessionToken");

                var success = await _authService.SwitchCenterAsync(sessionToken, centerId);

                if (success)
                {
                    // Mettre à jour la session
                    var sessionDetails = await _authService.GetSessionDetailsAsync(sessionToken);
                    if (sessionDetails != null)
                    {
                        HttpContext.Session.SetInt32("CurrentCenterId", centerId);
                        HttpContext.Session.SetString("CurrentCenterName", sessionDetails.CurrentCenter.Name);
                        HttpContext.Session.SetString("CurrentRole", sessionDetails.SessionInfo.CurrentRole);

                        return Json(new
                        {
                            success = true,
                            centerName = sessionDetails.CurrentCenter.Name,
                            role = sessionDetails.SessionInfo.CurrentRole
                        });
                    }
                }

                return Json(new { success = false, message = "Impossible de changer de centre" });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "SwitchCenterError",
                    $"Erreur lors du changement de centre vers {centerId}",
                    details: new { CenterId = centerId, Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors du changement de centre" });
            }
        }

        /// <summary>
        /// Génère un message de bienvenue personnalisé selon l'heure
        /// </summary>
        private static string GetWelcomeMessage(string firstName)
        {
            var hour = DateTime.Now.Hour;
            var greeting = hour switch
            {
                < 12 => "Bonjour",
                < 18 => "Bon après-midi",
                _ => "Bonsoir"
            };

            return $"{greeting}, {firstName}";
        }
    }

}