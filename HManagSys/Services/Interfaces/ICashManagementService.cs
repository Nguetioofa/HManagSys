using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Finance;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour la gestion des remises d'espèces et gestion de caisse
/// </summary>
public interface ICashManagementService
{
    /// <summary>
    /// Récupère une remise par son ID
    /// </summary>
    Task<CashHandoverViewModel?> GetHandoverByIdAsync(int id);

    /// <summary>
    /// Récupère les remises avec pagination et filtres
    /// </summary>
    Task<(List<CashHandoverViewModel> Items, int TotalCount)> GetCashHandoversAsync(CashHandoverFilters filters);

    /// <summary>
    /// Crée une nouvelle remise d'espèces
    /// </summary>
    Task<OperationResult<CashHandoverViewModel>> CreateCashHandoverAsync(CreateCashHandoverViewModel model, int createdBy);

    /// <summary>
    /// Récupère l'état de la caisse pour un centre
    /// </summary>
    Task<CashPositionViewModel> GetCashPositionAsync(int hospitalCenterId);

    /// <summary>
    /// Récupère l'historique des mouvements de caisse pour un centre
    /// </summary>
    Task<List<CashMovementViewModel>> GetCashMovementsAsync(int hospitalCenterId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Génère un bordereau de remise au format PDF
    /// </summary>
    Task<byte[]> GenerateHandoverReceiptAsync(int handoverId);

    /// <summary>
    /// Récupère le solde de caisse courant pour un centre
    /// </summary>
    Task<decimal> GetCurrentCashBalanceAsync(int hospitalCenterId);

    /// <summary>
    /// Calcule les recettes en espèces depuis la dernière remise
    /// </summary>
    Task<CashReconciliationViewModel> CalculateCashReceiptsSinceLastHandoverAsync(int hospitalCenterId);
}