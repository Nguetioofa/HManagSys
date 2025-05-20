using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Models.ViewModels.Users;
using HManagSys.Services.Implementations;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly IStockService _stockService;
        private readonly IGenericRepository<Patient> _patientRepository;
        private readonly IGenericRepository<CareEpisode> _careEpisodeService;
        private readonly IGenericRepository<Examination> _examinationRepository;
        private readonly IGenericRepository<Payment> _paymentRepository;
        private readonly IGenericRepository<CareService> _careServiceRepository;


        public DashboardController(
            IApplicationLogger appLogger,
            IAuthenticationService authService,
            IUserRepository userRepository,
            IMedicalDashboardService medicalDashboardService,
            IStockService stockService,
            IGenericRepository<Patient> patientRepository,
            IGenericRepository<CareEpisode> careEpisodeService,
            IGenericRepository<Examination> examinationRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<CareService> careServiceRepository,
            IHospitalCenterRepository hospitalCenterRepository)
        {
            _appLogger = appLogger;
            _authService = authService;
            _userRepository = userRepository;
            _medicalDashboardService = medicalDashboardService;
            _stockService = stockService;
            _patientRepository = patientRepository;
            _careEpisodeService = careEpisodeService;
            _paymentRepository = paymentRepository;
            _careServiceRepository = careServiceRepository;
            _examinationRepository = examinationRepository;
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

        [HttpGet]
        public async Task<IActionResult> GetStockAlertsWidget(List<StockAlertDetailViewModel> viewModels)
        {
            List<StockAlertDetailViewModel>? stockAlerts = await _stockService.GetStockAlertsAsync(CurrentCenterId.Value);

            return PartialView("_StockAlertsWidget", stockAlerts);
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
        /// Tableau de bord administrateur avec vision multi-centres
        /// </summary>
        [RequireAuthentication]
        [SuperAdmin]
        public async Task<IActionResult> Admin()
        {
            try
            {
                var userId = CurrentUserId;

                // Récupérer les centres accessibles
                List<HospitalCenter>? centers = await _hospitalCenterRepository.GetUserAccessibleCentersAsync(userId.Value);

                // Initialiser le modèle de vue
                var viewModel = new AdminDashboardViewModel
                {
                    Users = new List<UserSummary>(),
                    Filters = new UserManagementFilters(),
                    Statistics = new AdminStatistics(),
                    Pagination = new PaginationInfo(),
                    AvailableCenters = centers,
                    ActiveSessions = new List<SessionInfo>()
                };

                // Récupérer les statistiques de base
                viewModel.Statistics = await GetAdminStatisticsAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "AdminDashboardError",
                    "Erreur lors du chargement du tableau de bord administrateur",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord administrateur";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Récupère les statistiques administratives pour le tableau de bord
        /// </summary>
        private async Task<AdminStatistics> GetAdminStatisticsAsync()
        {
            // Récupérer le nombre total d'utilisateurs
            var totalUsers = await _userRepository.CountAsync();

            // Récupérer le nombre d'utilisateurs actifs
            var activeUsers = await _userRepository.CountAsync(q => q.Where(u => u.IsActive));

            // Récupérer le nombre d'utilisateurs connectés aujourd'hui
            var today = DateTime.Today;
            var usersLoggedToday = await _userRepository.CountAsync(q =>
                q.Where(u => u.LastLoginDate.HasValue && u.LastLoginDate.Value.Date == today));

            // Récupérer le nombre d'utilisateurs qui doivent changer leur mot de passe
            var usersRequiringPasswordChange = await _userRepository.CountAsync(q =>
                q.Where(u => u.MustChangePassword));

            // Récupérer le nombre de sessions actives
            //var activeSessions = await _authService.GetActiveSessionsCountAsync();

            // Récupérer le nombre de SuperAdmin et de Personnel Soignant
            var superAdminCount = await _userRepository.CountAsync(q =>
                q.Where(u => u.UserCenterAssignments.Any(uca => uca.RoleType == "SuperAdmin" && uca.IsActive)));

            var medicalStaffCount = await _userRepository.CountAsync(q =>
                q.Where(u => u.UserCenterAssignments.Any(uca => uca.RoleType == "MedicalStaff" && uca.IsActive)));

            return new AdminStatistics
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                UsersLoggedToday = usersLoggedToday,
                UsersRequiringPasswordChange = usersRequiringPasswordChange,
                //TotalActiveSessions = activeSessions,
                SuperAdmins = superAdminCount,
                MedicalStaff = medicalStaffCount
            };
        }

        /// <summary>
        /// API pour récupérer les statistiques admin via AJAX
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> GetAdminStatistics()
        {
            try
            {
                var stats = await GetAdminStatisticsAsync();

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetAdminStatisticsError",
                    "Erreur lors de la récupération des statistiques administratives",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des statistiques" });
            }
        }

        /// <summary>
        /// API pour récupérer les utilisateurs récents via AJAX
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> GetRecentUsers(int count = 10)
        {
            try
            {
                var recentUsers = await _userRepository.QueryListAsync(q =>
                    q.OrderByDescending(u => u.CreatedAt)
                    .Take(count)
                    .Select(u => new UserSummary
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        IsActive = u.IsActive,
                        LastLoginDate = u.LastLoginDate,
                        MustChangePassword = u.MustChangePassword,
                        ActiveSessionsCount = u.UserSessions.Count(us => us.IsActive)
                    }));

                return Json(new { success = true, data = recentUsers });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetRecentUsersError",
                    "Erreur lors de la récupération des utilisateurs récents",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des utilisateurs récents" });
            }
        }

        /// <summary>
        /// API pour récupérer les sessions actives via AJAX
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> GetActiveSessions(int count = 10)
        {
            try
            {
                var activeSessions = await _authService.GetUserActiveSessionsAsync(count);

                return Json(new { success = true, data = activeSessions });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetActiveSessionsError",
                    "Erreur lors de la récupération des sessions actives",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des sessions actives" });
            }
        }

        /// <summary>
        /// API pour récupérer les métriques des centres via AJAX
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> GetCenterMetrics()
        {
            try
            {
                var centerStats = await _hospitalCenterRepository.GetNetworkStatisticsAsync();

                return Json(new { success = true, data = centerStats });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetCenterMetricsError",
                    "Erreur lors de la récupération des métriques des centres",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des métriques des centres" });
            }
        }


        /// <summary>
        /// API pour récupérer les données principales du tableau de bord via AJAX
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return Json(new { success = false, message = "Aucun centre sélectionné" });
                }

                var today = DateTime.Today;

                // Compter les patients enregistrés aujourd'hui
                var todayPatientsCount = await _patientRepository.CountAsync(q =>
                    q.Where(p => p.CareEpisodes.Any(ce => ce.HospitalCenterId == CurrentCenterId.Value) &&
                                 p.CreatedAt.Date == today));

                // Récupérer le montant des ventes du jour
                var todaySalesAmount = await _paymentRepository.SumAsync(
                    q => q.Where(p => p.HospitalCenterId == CurrentCenterId.Value &&
                                     p.PaymentDate.Date == today &&
                                     p.ReferenceType == "Sale"),
                    p => p.Amount);

                // Compter les alertes de stock
                var stockAlertsCount = await _stockService.GetStockAlertsAsync(CurrentCenterId.Value);
                var stockAlertsCountValue = stockAlertsCount?.Count ?? 0;

                // Compter les examens en attente
                var pendingExamsCount = await _examinationRepository.CountAsync(q =>
                    q.Where(e => e.HospitalCenterId == CurrentCenterId.Value &&
                                (e.Status == "Requested" || e.Status == "Scheduled")));

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        todayPatientsCount,
                        todaySalesAmount,
                        stockAlertsCount = stockAlertsCountValue,
                        pendingExamsCount
                    }
                });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetDashboardDataError",
                    "Erreur lors de la récupération des données du tableau de bord",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des données du tableau de bord" });
            }
        }

        /// <summary>
        /// API pour récupérer les activités récentes via AJAX
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return Json(new { success = false, message = "Aucun centre sélectionné" });
                }

                // Récupérer les 5 derniers épisodes de soins
                var recentCareEpisodes = await _careEpisodeService.QueryListAsync(q =>
                    q.Where(ce => ce.HospitalCenterId == CurrentCenterId.Value)
                     .OrderByDescending(ce => ce.CreatedAt)
                     .Take(5)
                     .Include(ce => ce.Patient)
                     .Include(ce => ce.PrimaryCaregiverNavigation)
                     .Select(ce => new
                     {
                         ActivityType = "CareEpisode",
                         ReferenceId = ce.Id,
                         Description = $"Épisode de soins: {ce.Diagnosis.DiagnosisName}",
                         PatientName = $"{ce.Patient.FirstName} {ce.Patient.LastName}",
                         UserName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                         ActivityDate = ce.CreatedAt,
                         Status = ce.Status,
                         ActivityTypeIcon = "fa-stethoscope"
                     }));

                // Récupérer les 5 derniers examens
                var recentExaminations = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.HospitalCenterId == CurrentCenterId.Value)
                     .OrderByDescending(e => e.CreatedAt)
                     .Take(5)
                     .Include(e => e.Patient)
                     .Include(e => e.RequestedByNavigation)
                     .Select(e => new
                     {
                         ActivityType = "Examination",
                         ReferenceId = e.Id,
                         Description = $"Examen: {e.ExaminationType.Name}",
                         PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                         UserName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                         ActivityDate = e.CreatedAt,
                         Status = e.Status,
                         ActivityTypeIcon = "fa-microscope"
                     }));

                // Récupérer les 5 dernières ventes
                var recentSales = await _paymentRepository.QueryListAsync(q =>
                    q.Where(p => p.HospitalCenterId == CurrentCenterId.Value &&
                                p.ReferenceType == "Sale")
                     .OrderByDescending(p => p.CreatedAt)
                     .Take(5)
                     .Include(p => p.Patient)
                     .Include(p => p.ReceivedByNavigation)
                     .Select(p => new
                     {
                         ActivityType = "Sale",
                         ReferenceId = p.ReferenceId,
                         Description = $"Vente: {p.Amount} FCFA",
                         PatientName = p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}" : "Client anonyme",
                         UserName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                         ActivityDate = p.CreatedAt,
                         Status = "Completed",
                         ActivityTypeIcon = "fa-cash-register"
                     }));

                // Combiner et trier les activités
                var allActivities = recentCareEpisodes
                    .Concat(recentExaminations)
                    .Concat(recentSales)
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(count)
                    .ToList();

                return Json(new { success = true, data = allActivities });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetRecentActivitiesError",
                    "Erreur lors de la récupération des activités récentes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des activités récentes" });
            }
        }

        /// <summary>
        /// API pour récupérer les alertes de stock via AJAX
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetStockAlerts(int count = 5)
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return Json(new { success = false, message = "Aucun centre sélectionné" });
                }

                var alerts = await _stockService.GetStockAlertsAsync(CurrentCenterId.Value);

                // Filtrer les alertes les plus critiques et limiter le nombre
                var filteredAlerts = alerts
                    .OrderBy(a => a.CurrentQuantity)
                    .Take(count)
                    .Select(a => new
                    {
                        productId = a.ProductId,
                        productName = a.ProductName,
                        quantity = a.CurrentQuantity,
                        unitOfMeasure = a.UnitOfMeasure,
                        minThreshold = a.MinimumThreshold,
                        status = a.StatusText
                    })
                    .ToList();

                return Json(new { success = true, data = filteredAlerts });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetStockAlertsError",
                    "Erreur lors de la récupération des alertes de stock",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des alertes de stock" });
            }
        }

        /// <summary>
        /// API pour récupérer les rendez-vous du jour via AJAX
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetDailySchedule()
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    return Json(new { success = false, message = "Aucun centre sélectionné" });
                }

                var today = DateTime.Today;

                // Récupérer les examens planifiés aujourd'hui
                var scheduledExams = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.HospitalCenterId == CurrentCenterId.Value &&
                                e.Status == "Scheduled" &&
                                e.ScheduledDate.HasValue &&
                                e.ScheduledDate.Value.Date == today)
                     .OrderBy(e => e.ScheduledDate)
                     .Include(e => e.Patient)
                     .Select(e => new
                     {
                         type = "Examen",
                         patientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                         time = e.ScheduledDate.Value.ToString("HH:mm"),
                         description = e.ExaminationType.Name
                     }));



                // Récupérer les services de soins planifiés aujourd'hui
                var scheduledCares = await _careServiceRepository.QueryListAsync(q =>
                    q.Where(cs => cs.CareEpisode.HospitalCenterId == CurrentCenterId.Value &&
                                  cs.ServiceDate.Date == today)
                     .OrderBy(cs => cs.ServiceDate)
                     .Include(cs => cs.CareEpisode.Patient)
                     .Select(cs => new
                     {
                         type = cs.CareType.Name,
                         patientName = $"{cs.CareEpisode.Patient.FirstName} {cs.CareEpisode.Patient.LastName}",
                         time = cs.ServiceDate.ToString("HH:mm"),
                         description = $"Durée prévue: {cs.Duration} min."
                     }));

                // Combiner et trier les rendez-vous par heure
                var allSchedule = scheduledExams
                    .Concat(scheduledCares)
                    .OrderBy(s => s.time)
                    .ToList();

                return Json(new { success = true, data = allSchedule });
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Dashboard", "GetDailyScheduleError",
                    "Erreur lors de la récupération des rendez-vous du jour",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération des rendez-vous du jour" });
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