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
    [RequireCurrentCenter]
    public class DashboardController : BaseController
    {
        private readonly IApplicationLogger _appLogger;
        private readonly IAuthenticationService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IHospitalCenterRepository _hospitalCenterRepository;
        private readonly IMedicalDashboardService _medicalDashboardService;

        public DashboardController(
            IApplicationLogger appLogger,
            IAuthenticationService authService,
            IUserRepository userRepository,
            IMedicalDashboardService medicalDashboardService,
            IHospitalCenterRepository hospitalCenterRepository)
        {
            _appLogger = appLogger;
            _authService = authService;
            _userRepository = userRepository;
            _medicalDashboardService = medicalDashboardService;
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
        /// Tableau de bord médical pour le centre actuel
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Medical()
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return RedirectToAction("SelectCenter", "Auth");
                }

                var dashboardData = await _medicalDashboardService.GetMedicalDashboardDataAsync(CurrentCenterId.Value);

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "MedicalDashboardError",
                    "Erreur lors du chargement du tableau de bord médical",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord médical";
                return RedirectToAction("Index");
            }
        }


        /// <summary>
        /// Tableau de bord pour un patient spécifique
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Patient(int id)
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return RedirectToAction("SelectCenter", "Auth");
                }

                var dashboardData = await _medicalDashboardService.GetPatientDashboardDataAsync(id, CurrentCenterId.Value);

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "PatientDashboardError",
                    $"Erreur lors du chargement du tableau de bord du patient {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { PatientId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord du patient";
                return RedirectToAction("Details", "Patient", new { id });
            }
        }

        /// <summary>
        /// Résumé d'un épisode de soins
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> CareEpisode(int id)
        {
            try
            {
                var episodeSummary = await _medicalDashboardService.GetCareEpisodeSummaryAsync(id);

                return View(episodeSummary);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "CareEpisodeSummaryError",
                    $"Erreur lors du chargement du résumé de l'épisode de soins {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { EpisodeId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du résumé de l'épisode de soins";
                return RedirectToAction("Index", "CareEpisode");
            }
        }

        /// <summary>
        /// Progression du traitement pour un épisode de soins
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> TreatmentProgress(int id)
        {
            try
            {
                var progressData = await _medicalDashboardService.GetTreatmentProgressAsync(id);

                return View(progressData);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "TreatmentProgressError",
                    $"Erreur lors du chargement de la progression du traitement {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { EpisodeId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement de la progression du traitement";
                return RedirectToAction("CareEpisode", new { id });
            }
        }

        /// <summary>
        /// Statistiques médicales récentes
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> RecentStatistics(int? days = 30)
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return RedirectToAction("SelectCenter", "Auth");
                }

                var statsData = await _medicalDashboardService.GetRecentMedicalStatisticsAsync(
                    CurrentCenterId.Value, days ?? 30);

                return View(statsData);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "RecentStatisticsError",
                    "Erreur lors du chargement des statistiques médicales récentes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Days = days, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des statistiques médicales récentes";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Partie AJAX - récupère les données de progression du traitement
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetTreatmentProgressData(int id)
        {
            try
            {
                var progressData = await _medicalDashboardService.GetTreatmentProgressAsync(id);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        progressPercentage = progressData.ProgressPercentage,
                        examinationsProgressPercentage = progressData.ExaminationsProgressPercentage,
                        prescriptionsProgressPercentage = progressData.PrescriptionsProgressPercentage,
                        paymentProgressPercentage = progressData.PaymentProgressPercentage,
                        serviceProgressByDate = progressData.ServiceProgressByDate,
                        nextSteps = progressData.NextSteps
                    }
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetTreatmentProgressDataError",
                    $"Erreur lors de la récupération des données de progression pour l'épisode {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { EpisodeId = id, Error = ex.Message });

                return Json(new
                {
                    success = false,
                    message = "Erreur lors de la récupération des données de progression"
                });
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