using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.ViewModels.Reports;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace HManagSys.Controllers
{
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class ReportController : BaseController
    {
        private readonly IReportService _reportService;
        private readonly IHospitalCenterRepository _centerRepository;
        private readonly IProductCategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IApplicationLogger _logger;

        public ReportController(
            IReportService reportService,
            IHospitalCenterRepository centerRepository,
            IProductCategoryRepository categoryRepository,
            IUserRepository userRepository,
            IPaymentMethodRepository paymentMethodRepository,
            IApplicationLogger logger)
        {
            _reportService = reportService;
            _centerRepository = centerRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _logger = logger;
        }

        #region Dashboard et Index

        /// <summary>
        /// Vue principale des rapports
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Dashboard des rapports
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Récupérer des données de base pour le dashboard
                var stockStatus = await _reportService.GenerateStockStatusReportAsync(new StockStatusReportFilters
                {
                    HospitalCenterId = CurrentCenterId,
                    // Limiter aux produits avec stock critique ou bas
                    StockStatus = "Critical"
                });

                var financialActivity = await _reportService.GenerateFinancialActivityReportAsync(new FinancialActivityReportFilters
                {
                    HospitalCenterId = CurrentCenterId,
                    GroupBy = "Day",
                    FromDate = DateTime.Now.AddDays(-7),
                    ToDate = DateTime.Now
                });

                var activeSessions = await _reportService.GenerateActiveSessionsReportAsync(new ActiveSessionsReportFilters
                {
                    HospitalCenterId = CurrentCenterId
                });

                ViewBag.CriticalStockCount = stockStatus.ProductsWithCriticalStock;
                ViewBag.LowStockCount = stockStatus.ProductsWithLowStock;
                ViewBag.TotalRevenue7Days = financialActivity.TotalRevenue;
                ViewBag.ActiveSessionsCount = activeSessions.TotalActiveSessions;

                return View();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "DashboardError",
                    "Erreur lors du chargement du dashboard des rapports",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du dashboard des rapports";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Utilisateurs et Centres

        /// <summary>
        /// Rapport utilisateurs-centres
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> UserCenterReport(UserCenterReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new UserCenterReportFilters();

                // Récupérer le rapport
                var report = await _reportService.GenerateUserCenterReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.Roles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les rôles" },
                    new SelectListItem { Value = "SuperAdmin", Text = "Super Administrateur" },
                    new SelectListItem { Value = "MedicalStaff", Text = "Personnel Soignant" }
                };

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "UserCenterReportError",
                    "Erreur lors de la génération du rapport utilisateurs-centres",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport utilisateurs-centres";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport des sessions actives
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> ActiveSessionsReport(ActiveSessionsReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new ActiveSessionsReportFilters();

                // Récupérer le rapport
                var report = await _reportService.GenerateActiveSessionsReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "ActiveSessionsReportError",
                    "Erreur lors de la génération du rapport des sessions actives",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport des sessions actives";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Stock et Inventaire

        /// <summary>
        /// Rapport d'état des stocks
        /// </summary>
        public async Task<IActionResult> StockStatusReport(StockStatusReportFilters filters = null)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new StockStatusReportFilters();

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateStockStatusReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.Categories = await GetCategoriesSelectListAsync();
                ViewBag.StockStatuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les statuts" },
                    new SelectListItem { Value = "Critical", Text = "Critique" },
                    new SelectListItem { Value = "Low", Text = "Bas" },
                    new SelectListItem { Value = "Normal", Text = "Normal" },
                    new SelectListItem { Value = "Overstock", Text = "Surstock" }
                };

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "StockStatusReportError",
                    "Erreur lors de la génération du rapport d'état des stocks",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport d'état des stocks";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport des mouvements de stock
        /// </summary>
        public async Task<IActionResult> StockMovementReport(StockMovementReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new StockMovementReportFilters
                {
                    FromDate = DateTime.Now.AddDays(-30),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateStockMovementReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.MovementTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les types" },
                    new SelectListItem { Value = "Initial", Text = "Initial" },
                    new SelectListItem { Value = "Entry", Text = "Entrée" },
                    new SelectListItem { Value = "Sale", Text = "Vente" },
                    new SelectListItem { Value = "Transfer", Text = "Transfert" },
                    new SelectListItem { Value = "Adjustment", Text = "Ajustement" },
                    new SelectListItem { Value = "Care", Text = "Soins" }
                };

                ViewBag.ReferenceTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Toutes les références" },
                    new SelectListItem { Value = "Sale", Text = "Vente" },
                    new SelectListItem { Value = "CareService", Text = "Service de soin" },
                    new SelectListItem { Value = "Transfer", Text = "Transfert" }
                };

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "StockMovementReportError",
                    "Erreur lors de la génération du rapport des mouvements de stock",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport des mouvements de stock";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport de valorisation des stocks
        /// </summary>
        public async Task<IActionResult> StockValuationReport(StockValuationReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new StockValuationReportFilters();

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateStockValuationReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.Categories = await GetCategoriesSelectListAsync();
                ViewBag.ValuationTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "SellingPrice", Text = "Prix de vente" },
                    new SelectListItem { Value = "LastPurchasePrice", Text = "Dernier prix d'achat" },
                    new SelectListItem { Value = "AveragePrice", Text = "Prix moyen" }
                };

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "StockValuationReportError",
                    "Erreur lors de la génération du rapport de valorisation des stocks",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport de valorisation des stocks";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Rapports Financiers

        /// <summary>
        /// Rapport d'activité financière
        /// </summary>
        public async Task<IActionResult> FinancialActivityReport(FinancialActivityReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new FinancialActivityReportFilters
                {
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateFinancialActivityReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.GroupByOptions = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Day", Text = "Jour" },
                    new SelectListItem { Value = "Week", Text = "Semaine" },
                    new SelectListItem { Value = "Month", Text = "Mois" }
                };

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "FinancialActivityReportError",
                    "Erreur lors de la génération du rapport d'activité financière",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport d'activité financière";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport des paiements
        /// </summary>
        public async Task<IActionResult> PaymentReport(PaymentReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new PaymentReportFilters
                {
                    FromDate = DateTime.Now.AddDays(-30),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GeneratePaymentReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.PaymentMethods = await GetPaymentMethodsSelectListAsync();
                ViewBag.ReferenceTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les types" },
                    new SelectListItem { Value = "Sale", Text = "Vente" },
                    new SelectListItem { Value = "CareEpisode", Text = "Épisode de soin" },
                    new SelectListItem { Value = "Examination", Text = "Examen" }
                };
                ViewBag.Receivers = await GetUsersSelectListAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "PaymentReportError",
                    "Erreur lors de la génération du rapport des paiements",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport des paiements";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport des ventes
        /// </summary>
        public async Task<IActionResult> SalesReport(SalesReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new SalesReportFilters
                {
                    FromDate = DateTime.Now.AddDays(-30),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateSalesReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.PaymentStatuses = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les statuts" },
                    new SelectListItem { Value = "Paid", Text = "Payé" },
                    new SelectListItem { Value = "Partial", Text = "Partiel" },
                    new SelectListItem { Value = "Pending", Text = "En attente" }
                };
                ViewBag.Sellers = await GetUsersSelectListAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "SalesReportError",
                    "Erreur lors de la génération du rapport des ventes",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport des ventes";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Rapports Performances

        /// <summary>
        /// Rapport de performance des soignants
        /// </summary>
        public async Task<IActionResult> CaregiverPerformanceReport(CaregiverPerformanceReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new CaregiverPerformanceReportFilters
                {
                    FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateCaregiverPerformanceReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();
                ViewBag.Users = await GetUsersSelectListAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "CaregiverPerformanceReportError",
                    "Erreur lors de la génération du rapport de performance des soignants",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport de performance des soignants";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Rapport d'activité médicale
        /// </summary>
        public async Task<IActionResult> MedicalActivityReport(MedicalActivityReportFilters filters)
        {
            try
            {
                // Filtres par défaut si non spécifiés
                filters ??= new MedicalActivityReportFilters
                {
                    FromDate = DateTime.Now.AddDays(-30),
                    ToDate = DateTime.Now
                };

                // Définir le centre courant par défaut
                if (!filters.HospitalCenterId.HasValue)
                    filters.HospitalCenterId = CurrentCenterId;

                // Récupérer le rapport
                var report = await _reportService.GenerateMedicalActivityReportAsync(filters);
                report.GeneratedBy = CurrentUserName;

                // Préparer les listes déroulantes pour les filtres
                ViewBag.Centers = await GetCentersSelectListAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "MedicalActivityReportError",
                    "Erreur lors de la génération du rapport d'activité médicale",
                    CurrentUserId, CurrentCenterId,
                    details: new { Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du rapport d'activité médicale";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Exports

        /// <summary>
        /// Exporte un rapport en Excel
        /// </summary>
        public async Task<IActionResult> ExportToExcel(string reportType, string filters)
        {
            try
            {
                // Désérialiser les filtres à partir de la chaîne JSON
                var filtersObj = DeserializeFilters(reportType, filters);

                // Définir le nom du fichier
                string fileName = $"{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Créer les paramètres d'export
                var parameters = new ExportParameters
                {
                    ReportType = reportType,
                    Format = "Excel",
                    Filters = filtersObj,
                    FileName = fileName,
                    IncludeHeaders = true,
                    IncludeFooters = true,
                    CompanyLogo = null, // Pas de logo pour l'instant
                    CustomTitle = GetReportTitle(reportType),
                    CustomFooter = $"Généré par {CurrentUserName} le {DateTime.Now:dd/MM/yyyy HH:mm}"
                };

                // Générer le fichier Excel
                var fileContent = await _reportService.ExportToExcelAsync(parameters);

                // Retourner le fichier
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "ExportToExcelError",
                    "Erreur lors de l'export du rapport en Excel",
                    CurrentUserId, CurrentCenterId,
                    details: new { ReportType = reportType, Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de l'export du rapport en Excel";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Exporte un rapport en PDF
        /// </summary>
        public async Task<IActionResult> ExportToPdf(string reportType, string filters)
        {
            try
            {
                // Désérialiser les filtres à partir de la chaîne JSON
                var filtersObj = DeserializeFilters(reportType, filters);

                // Définir le nom du fichier
                string fileName = $"{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // Créer les paramètres d'export
                var parameters = new ExportParameters
                {
                    ReportType = reportType,
                    Format = "PDF",
                    Filters = filtersObj,
                    FileName = fileName,
                    IncludeHeaders = true,
                    IncludeFooters = true,
                    CompanyLogo = null, // Pas de logo pour l'instant
                    CustomTitle = GetReportTitle(reportType),
                    CustomFooter = $"Généré par {CurrentUserName} le {DateTime.Now:dd/MM/yyyy HH:mm}"
                };

                // Générer le fichier PDF
                var fileContent = await _reportService.ExportToPdfAsync(parameters);

                // Retourner le fichier
                return File(fileContent, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "ExportToPdfError",
                    "Erreur lors de l'export du rapport en PDF",
                    CurrentUserId, CurrentCenterId,
                    details: new { ReportType = reportType, Filters = filters, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de l'export du rapport en PDF";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Admin Reports

        /// <summary>
        /// Rafraîchit les données d'un rapport
        /// </summary>
        [HttpPost]
        [SuperAdmin]
        public async Task<IActionResult> RefreshReportData(string reportType)
        {
            try
            {
                bool success = await _reportService.RefreshReportDataAsync(reportType);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Données du rapport {reportType} rafraîchies avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Erreur lors du rafraîchissement des données du rapport {reportType}";
                }

                return RedirectToAction(reportType);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "RefreshReportDataError",
                    $"Erreur lors du rafraîchissement des données du rapport {reportType}",
                    CurrentUserId, CurrentCenterId,
                    details: new { ReportType = reportType, Error = ex.Message });

                TempData["ErrorMessage"] = $"Erreur lors du rafraîchissement des données du rapport {reportType}";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Gestion des rapports planifiés
        /// </summary>
        [SuperAdmin]
        public async Task<IActionResult> ScheduledReports()
        {
            try
            {
                var reports = await _reportService.GetScheduledReportsAsync();
                return View(reports);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "ScheduledReportsError",
                    "Erreur lors du chargement des rapports planifiés",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des rapports planifiés";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Formulaire de planification d'un rapport
        /// </summary>
        [SuperAdmin]
        public IActionResult ScheduleReport()
        {
            try
            {
                // Préparer les listes déroulantes
                ViewBag.ReportTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "StockStatusReport", Text = "État des stocks" },
                    new SelectListItem { Value = "FinancialActivityReport", Text = "Activité financière" },
                    new SelectListItem { Value = "CaregiverPerformanceReport", Text = "Performance des soignants" }
                };

                ViewBag.Frequencies = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Daily", Text = "Quotidien" },
                    new SelectListItem { Value = "Weekly", Text = "Hebdomadaire" },
                    new SelectListItem { Value = "Monthly", Text = "Mensuel" }
                };

                ViewBag.Formats = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Excel", Text = "Excel" },
                    new SelectListItem { Value = "PDF", Text = "PDF" }
                };

                ViewBag.DaysOfWeek = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Lundi" },
                    new SelectListItem { Value = "2", Text = "Mardi" },
                    new SelectListItem { Value = "3", Text = "Mercredi" },
                    new SelectListItem { Value = "4", Text = "Jeudi" },
                    new SelectListItem { Value = "5", Text = "Vendredi" },
                    new SelectListItem { Value = "6", Text = "Samedi" },
                    new SelectListItem { Value = "0", Text = "Dimanche" }
                };

                var model = new RecurringReportSchedule
                {
                    IsActive = true,
                    Format = "Excel",
                    Frequency = "Weekly",
                    DayOfWeek = DayOfWeek.Monday,
                    ExecutionTime = new TimeSpan(8, 0, 0),
                    SaveToServer = true
                };

                return View(model);
            }
            catch (Exception ex)
            {
                //await _logger.LogErrorAsync("ReportController", "ScheduleReportError",
                //    "Erreur lors du chargement du formulaire de planification",
                //    CurrentUserId, CurrentCenterId,
                //    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de planification";
                return RedirectToAction("ScheduledReports");
            }
        }

        /// <summary>
        /// Traitement du formulaire de planification
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> ScheduleReport(RecurringReportSchedule model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Recharger les listes déroulantes
                    ViewBag.ReportTypes = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "StockStatusReport", Text = "État des stocks" },
                        new SelectListItem { Value = "FinancialActivityReport", Text = "Activité financière" },
                        new SelectListItem { Value = "CaregiverPerformanceReport", Text = "Performance des soignants" }
                    };

                    ViewBag.Frequencies = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "Daily", Text = "Quotidien" },
                        new SelectListItem { Value = "Weekly", Text = "Hebdomadaire" },
                        new SelectListItem { Value = "Monthly", Text = "Mensuel" }
                    };

                    ViewBag.Formats = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "Excel", Text = "Excel" },
                        new SelectListItem { Value = "PDF", Text = "PDF" }
                    };

                    ViewBag.DaysOfWeek = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "1", Text = "Lundi" },
                        new SelectListItem { Value = "2", Text = "Mardi" },
                        new SelectListItem { Value = "3", Text = "Mercredi" },
                        new SelectListItem { Value = "4", Text = "Jeudi" },
                        new SelectListItem { Value = "5", Text = "Vendredi" },
                        new SelectListItem { Value = "6", Text = "Samedi" },
                        new SelectListItem { Value = "0", Text = "Dimanche" }
                    };

                    return View(model);
                }

                // Définir le créateur
                model.CreatedBy = CurrentUserId.Value;

                // Planifier le rapport
                bool success = await _reportService.ScheduleRecurringReportAsync(model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Rapport planifié avec succès";
                    return RedirectToAction("ScheduledReports");
                }
                else
                {
                    TempData["ErrorMessage"] = "Erreur lors de la planification du rapport";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "ScheduleReportPostError",
                    "Erreur lors de la planification du rapport",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la planification du rapport";
                return View(model);
            }
        }

        /// <summary>
        /// Suppression d'un rapport planifié
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> DeleteScheduledReport(int id)
        {
            try
            {
                bool success = await _reportService.DeleteScheduledReportAsync(id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Rapport planifié supprimé avec succès";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erreur lors de la suppression du rapport planifié";
                }

                return RedirectToAction("ScheduledReports");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ReportController", "DeleteScheduledReportError",
                    $"Erreur lors de la suppression du rapport planifié {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { Id = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la suppression du rapport planifié";
                return RedirectToAction("ScheduledReports");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Récupère la liste des centres pour les listes déroulantes
        /// </summary>
        private async Task<List<SelectListItem>> GetCentersSelectListAsync()
        {
            if (CurrentRole == "SuperAdmin")
            {
                // SuperAdmin peut voir tous les centres
                var centers = await _centerRepository.GetAllAsync(query => query.Where(c => c.IsActive));

                var centerList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Tous les centres" }
                };

                centerList.AddRange(centers.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == CurrentCenterId
                }));

                return centerList;
            }
            else
            {
                // Personnel soignant ne voit que son centre courant
                return new List<SelectListItem>
                {
                    new SelectListItem
                    {
                        Value = CurrentCenterId.ToString(),
                        Text = CurrentCenterName,
                        Selected = true
                    }
                };
            }
        }

        /// <summary>
        /// Récupère la liste des catégories pour les listes déroulantes
        /// </summary>
        private async Task<List<SelectListItem>> GetCategoriesSelectListAsync()
        {
            var categories = await _categoryRepository.GetAllAsync(query => query.Where(c => c.IsActive));

            var categoryList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Toutes les catégories" }
            };

            categoryList.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }));

            return categoryList;
        }

        /// <summary>
        /// Récupère la liste des utilisateurs pour les listes déroulantes
        /// </summary>
        private async Task<List<SelectListItem>> GetUsersSelectListAsync()
        {
            var users = await _userRepository.GetUsersByRoleAndCenterAsync("MedicalStaff", CurrentCenterId.Value);

            var userList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tous les utilisateurs" }
            };

            userList.AddRange(users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FirstName} {u.LastName}"
            }));

            return userList;
        }

        /// <summary>
        /// Récupère la liste des méthodes de paiement pour les listes déroulantes
        /// </summary>
        private async Task<List<SelectListItem>> GetPaymentMethodsSelectListAsync()
        {
            var methods = await _paymentMethodRepository.GetAllAsync(query => query.Where(pm => pm.IsActive));

            var methodList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Toutes les méthodes" }
            };

            methodList.AddRange(methods.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Name
            }));

            return methodList;
        }

        /// <summary>
        /// Désérialise les filtres à partir d'une chaîne JSON
        /// </summary>
        private object DeserializeFilters(string reportType, string filters)
        {
            // Désérialiser les filtres selon le type de rapport
            switch (reportType)
            {
                case "UserCenterReport":
                    return System.Text.Json.JsonSerializer.Deserialize<UserCenterReportFilters>(filters);

                case "ActiveSessionsReport":
                    return System.Text.Json.JsonSerializer.Deserialize<ActiveSessionsReportFilters>(filters);

                case "StockStatusReport":
                    return System.Text.Json.JsonSerializer.Deserialize<StockStatusReportFilters>(filters);

                case "StockMovementReport":
                    return System.Text.Json.JsonSerializer.Deserialize<StockMovementReportFilters>(filters);

                case "StockValuationReport":
                    return System.Text.Json.JsonSerializer.Deserialize<StockValuationReportFilters>(filters);

                case "FinancialActivityReport":
                    return System.Text.Json.JsonSerializer.Deserialize<FinancialActivityReportFilters>(filters);

                case "PaymentReport":
                    return System.Text.Json.JsonSerializer.Deserialize<PaymentReportFilters>(filters);

                case "SalesReport":
                    return System.Text.Json.JsonSerializer.Deserialize<SalesReportFilters>(filters);

                case "CaregiverPerformanceReport":
                    return System.Text.Json.JsonSerializer.Deserialize<CaregiverPerformanceReportFilters>(filters);

                case "MedicalActivityReport":
                    return System.Text.Json.JsonSerializer.Deserialize<MedicalActivityReportFilters>(filters);

                default:
                    throw new ArgumentException($"Type de rapport inconnu : {reportType}");
            }
        }

        /// <summary>
        /// Récupère le titre d'un rapport selon son type
        /// </summary>
        private string GetReportTitle(string reportType)
        {
            return reportType switch
            {
                "UserCenterReport" => "Rapport des Utilisateurs et Centres",
                "ActiveSessionsReport" => "Rapport des Sessions Actives",
                "StockStatusReport" => "Rapport d'État des Stocks",
                "StockMovementReport" => "Rapport des Mouvements de Stock",
                "StockValuationReport" => "Rapport de Valorisation des Stocks",
                "FinancialActivityReport" => "Rapport d'Activité Financière",
                "PaymentReport" => "Rapport des Paiements",
                "SalesReport" => "Rapport des Ventes",
                "CaregiverPerformanceReport" => "Rapport de Performance des Soignants",
                "MedicalActivityReport" => "Rapport d'Activité Médicale",
                _ => "Rapport"
            };
        }

        #endregion
    }
}