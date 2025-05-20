using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Finance;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour la gestion des financiers
/// </summary>
public interface IFinancierService
{
    /// <summary>
    /// Récupère un financier par son ID
    /// </summary>
    Task<FinancierViewModel?> GetByIdAsync(int id);

    /// <summary>
    /// Récupère tous les financiers d'un centre
    /// </summary>
    Task<List<FinancierViewModel>> GetFinanciersByCenterAsync(int hospitalCenterId);

    /// <summary>
    /// Récupère tous les financiers actifs
    /// </summary>
    Task<List<SelectOption>> GetActiveFinanciersSelectAsync(int hospitalCenterId);

    /// <summary>
    /// Récupère les financiers avec pagination et filtres
    /// </summary>
    Task<(List<FinancierViewModel> Items, int TotalCount)> GetFinanciersAsync(FinancierFilters filters);

    /// <summary>
    /// Crée un nouveau financier
    /// </summary>
    Task<OperationResult<FinancierViewModel>> CreateFinancierAsync(CreateFinancierViewModel model, int createdBy);

    /// <summary>
    /// Met à jour un financier existant
    /// </summary>
    Task<OperationResult<FinancierViewModel>> UpdateFinancierAsync(int id, EditFinancierViewModel model, int modifiedBy);

    /// <summary>
    /// Active ou désactive un financier
    /// </summary>
    Task<OperationResult> ToggleFinancierStatusAsync(int id, bool isActive, int modifiedBy);
}