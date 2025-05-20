using HManagSys.Attributes;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Models;
using HManagSys.Models.ViewModels.Finance;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HManagSys.Controllers
{
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class CashHandoverController : BaseController
    {
        private readonly ICashManagementService _cashManagementService;
        private readonly IFinancierService _financierService;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationLogger _logger;

        public CashHandoverController(
            ICashManagementService cashManagementService,
            IFinancierService financierService,
            IUserRepository userRepository,
            IApplicationLogger logger)
        {
            _cashManagementService = cashManagementService;
            _financierService = financierService;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Page d'accueil de la gestion de caisse
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Récupérer l'état actuel de la caisse
                var cashPosition = await _cashManagementService.GetCashPositionAsync(CurrentCenterId.Value);

                // Récupérer les 10 derniers mouvements
                var recentMovements = await _cashManagementService.GetCashMovementsAsync(
                    CurrentCenterId.Value,
                    DateTime.Now.AddDays(-30),
                    DateTime.Now);

                // Récupérer la réconciliation depuis la dernière remise
                var reconciliation = await _cashManagementService.CalculateCashReceiptsSinceLastHandoverAsync(CurrentCenterId.Value);

                // Modèle pour la vue
                var viewModel = new CashDashboardViewModel
                {
                    CashPosition = cashPosition,
                    RecentMovements = recentMovements.Take(10).ToList(),
                    Reconciliation = reconciliation
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "IndexError",
                    "Erreur lors du chargement du tableau de bord de caisse",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du tableau de bord de caisse";
                return View(new CashDashboardViewModel());
            }
        }

        /// <summary>
        /// Liste des remises d'espèces
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> List(CashHandoverFilters? filters = null)
        {
            try
            {
                filters ??= new CashHandoverFilters();
                filters.HospitalCenterId = CurrentCenterId;

                var (handovers, totalCount) = await _cashManagementService.GetCashHandoversAsync(filters);

                var viewModel = new PagedViewModel<CashHandoverViewModel, CashHandoverFilters>
                {
                    Items = handovers,
                    Filters = filters,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = filters.PageIndex,
                        PageSize = filters.PageSize,
                        TotalCount = totalCount
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "ListError",
                    "Erreur lors du chargement de la liste des remises",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des remises";
                return View(new PagedViewModel<CashHandoverViewModel, CashHandoverFilters>());
            }
        }

        /// <summary>
        /// Historique des mouvements de caisse
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Movements(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var movements = await _cashManagementService.GetCashMovementsAsync(
                    CurrentCenterId.Value,
                    fromDate,
                    toDate);

                var viewModel = new CashMovementsViewModel
                {
                    Movements = movements,
                    FromDate = fromDate ?? DateTime.Now.AddDays(-30).Date,
                    ToDate = toDate ?? DateTime.Now.Date,
                    InitialBalance = movements.Any() ? movements.First().Balance - (movements.First().Direction == "IN" ? movements.First().Amount : -movements.First().Amount) : 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "MovementsError",
                    "Erreur lors du chargement des mouvements de caisse",
                    CurrentUserId, CurrentCenterId,
                    details: new { FromDate = fromDate, ToDate = toDate, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des mouvements de caisse";
                return View(new CashMovementsViewModel());
            }
        }

        /// <summary>
        /// Détails d'une remise
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var handover = await _cashManagementService.GetHandoverByIdAsync(id);
                if (handover == null)
                {
                    TempData["ErrorMessage"] = "Remise introuvable";
                    return RedirectToAction(nameof(List));
                }

                return View(handover);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "DetailsError",
                    $"Erreur lors du chargement des détails de la remise {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { HandoverId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement des détails de la remise";
                return RedirectToAction(nameof(List));
            }
        }

        /// <summary>
        /// Formulaire de nouvelle remise
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Récupérer l'état actuel de la caisse pour préremplir le formulaire
                var cashPosition = await _cashManagementService.GetCashPositionAsync(CurrentCenterId.Value);

                // Récupérer les financiers actifs pour la liste déroulante
                var financiers = await _financierService.GetActiveFinanciersSelectAsync(CurrentCenterId.Value);

                // Préparer le modèle
                var model = new CreateCashHandoverViewModel
                {
                    HospitalCenterId = CurrentCenterId.Value,
                    HospitalCenterName = CurrentCenterName,
                    HandoverDate = DateTime.Now,
                    TotalCashAmount = cashPosition.CurrentBalance,
                    HandoverAmount = cashPosition.CurrentBalance, // Par défaut, remettre tout
                    RemainingCashAmount = 0, // Par défaut, ne rien garder
                    HandedOverBy = CurrentUserId.Value,
                    FinancierOptions = financiers
                };

                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "CreateGetError",
                    "Erreur lors du chargement du formulaire de remise",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du formulaire de remise";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Traitement du formulaire de nouvelle remise
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SuperAdmin]
        public async Task<IActionResult> Create(CreateCashHandoverViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Récupérer les financiers pour la liste déroulante
                    model.FinancierOptions = await _financierService.GetActiveFinanciersSelectAsync(CurrentCenterId.Value);
                    return View(model);
                }

                // Forcer le centre courant
                model.HospitalCenterId = CurrentCenterId.Value;

                // Vérifier la cohérence des montants
                if (model.TotalCashAmount != model.HandoverAmount + model.RemainingCashAmount)
                {
                    model.RemainingCashAmount = model.TotalCashAmount - model.HandoverAmount;
                }

                // Créer la remise
                var result = await _cashManagementService.CreateCashHandoverAsync(model, CurrentUserId.Value);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Remise d'espèces enregistrée avec succès";
                    return RedirectToAction(nameof(Receipt), new { id = result.Data.Id });
                }

                // En cas d'erreur
                ModelState.AddModelError("", result.ErrorMessage);
                model.FinancierOptions = await _financierService.GetActiveFinanciersSelectAsync(CurrentCenterId.Value);
                return View(model);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "CreatePostError",
                    "Erreur lors de la création de la remise",
                    CurrentUserId, CurrentCenterId,
                    details: new { Model = model, Error = ex.Message });

                ModelState.AddModelError("", "Une erreur est survenue lors de la création de la remise");
                model.FinancierOptions = await _financierService.GetActiveFinanciersSelectAsync(CurrentCenterId.Value);
                return View(model);
            }
        }

        /// <summary>
        /// Affichage du reçu de remise
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> Receipt(int id)
        {
            try
            {
                var handover = await _cashManagementService.GetHandoverByIdAsync(id);
                if (handover == null)
                {
                    TempData["ErrorMessage"] = "Remise introuvable";
                    return RedirectToAction(nameof(List));
                }

                return View(handover);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "ReceiptError",
                    $"Erreur lors du chargement du reçu de la remise {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { HandoverId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors du chargement du reçu";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        /// <summary>
        /// Téléchargement du reçu en PDF
        /// </summary>
        [MedicalStaff]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            try
            {
                var handover = await _cashManagementService.GetHandoverByIdAsync(id);
                if (handover == null)
                {
                    TempData["ErrorMessage"] = "Remise introuvable";
                    return RedirectToAction(nameof(List));
                }

                var pdfBytes = await _cashManagementService.GenerateHandoverReceiptAsync(id);

                return File(pdfBytes, "application/pdf", $"Bordereau_Remise_{id}.pdf");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "DownloadReceiptError",
                    $"Erreur lors du téléchargement du reçu de la remise {id}",
                    CurrentUserId, CurrentCenterId,
                    details: new { HandoverId = id, Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du PDF";
                return RedirectToAction(nameof(Receipt), new { id });
            }
        }

        /// <summary>
        /// Calcul du solde actuel (AJAX)
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetCurrentBalance()
        {
            try
            {
                var balance = await _cashManagementService.GetCurrentCashBalanceAsync(CurrentCenterId.Value);
                return Json(new { success = true, balance, formattedBalance = $"{balance:N0} FCFA" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "GetCurrentBalanceError",
                    "Erreur lors de la récupération du solde actuel",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération du solde" });
            }
        }

        /// <summary>
        /// Réconciliation de caisse depuis la dernière remise (AJAX)
        /// </summary>
        [HttpGet]
        [MedicalStaff]
        public async Task<IActionResult> GetReconciliation()
        {
            try
            {
                var reconciliation = await _cashManagementService.CalculateCashReceiptsSinceLastHandoverAsync(CurrentCenterId.Value);
                return Json(new { success = true, data = reconciliation });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashHandover", "GetReconciliationError",
                    "Erreur lors de la récupération de la réconciliation",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                return Json(new { success = false, message = "Erreur lors de la récupération de la réconciliation" });
            }
        }
    }

    /// <summary>
    /// Modèle pour le tableau de bord de caisse
    /// </summary>
    public class CashDashboardViewModel
    {
        public CashPositionViewModel CashPosition { get; set; } = new CashPositionViewModel();
        public List<CashMovementViewModel> RecentMovements { get; set; } = new List<CashMovementViewModel>();
        public CashReconciliationViewModel Reconciliation { get; set; } = new CashReconciliationViewModel();
    }

    /// <summary>
    /// Modèle pour la page des mouvements de caisse
    /// </summary>
    public class CashMovementsViewModel
    {
        public List<CashMovementViewModel> Movements { get; set; } = new List<CashMovementViewModel>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialBalance { get; set; }

        // Propriétés calculées
        public decimal TotalIn => Movements.Where(m => m.Direction == "IN").Sum(m => m.Amount);
        public decimal TotalOut => Movements.Where(m => m.Direction == "OUT").Sum(m => m.Amount);
        public decimal NetChange => TotalIn - TotalOut;
        public decimal FinalBalance => InitialBalance + NetChange;

        // Propriétés formatées
        public string FormattedInitialBalance => $"{InitialBalance:N0} FCFA";
        public string FormattedTotalIn => $"{TotalIn:N0} FCFA";
        public string FormattedTotalOut => $"{TotalOut:N0} FCFA";
        public string FormattedNetChange => $"{NetChange:N0} FCFA";
        public string FormattedFinalBalance => $"{FinalBalance:N0} FCFA";
    }
}