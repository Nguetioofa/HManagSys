using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des catégories de produits
    /// Comme un bibliothécaire spécialisé dans l'organisation des références médicales
    /// </summary>
    public interface IProductCategoryService
    {
        // ===== OPÉRATIONS CRUD =====

        /// <summary>
        /// Récupère toutes les catégories avec filtres et pagination
        /// </summary>
        Task<(List<ProductCategoryViewModel> Categories, int TotalCount)> GetCategoriesAsync(
            ProductCategoryFilters filters);

        /// <summary>
        /// Récupère une catégorie par son ID avec ses statistiques
        /// </summary>
        Task<ProductCategoryViewModel?> GetCategoryByIdAsync(int id);

        /// <summary>
        /// Crée une nouvelle catégorie
        /// </summary>
        Task<OperationResult<ProductCategoryViewModel>> CreateCategoryAsync(
            CreateProductCategoryViewModel model,
            int createdBy);

        /// <summary>
        /// Modifie une catégorie existante
        /// </summary>
        Task<OperationResult<ProductCategoryViewModel>> UpdateCategoryAsync(
            int id,
            EditProductCategoryViewModel model,
            int modifiedBy);

        /// <summary>
        /// Supprime une catégorie (soft delete si possible)
        /// </summary>
        Task<OperationResult> DeleteCategoryAsync(int id, int deletedBy);

        /// <summary>
        /// Active ou désactive une catégorie
        /// </summary>
        Task<OperationResult> ToggleCategoryStatusAsync(int id, bool isActive, int modifiedBy);

        // ===== REQUÊTES SPÉCIALISÉES =====

        /// <summary>
        /// Récupère toutes les catégories actives pour les listes déroulantes
        /// </summary>
        Task<List<ProductCategorySelectViewModel>> GetActiveCategoriesForSelectAsync();

        /// <summary>
        /// Vérifie si une catégorie peut être supprimée
        /// </summary>
        Task<bool> CanDeleteCategoryAsync(int id);

        /// <summary>
        /// Récupère les statistiques générales des catégories
        /// </summary>
        Task<CategoryStatistics> GetCategoryStatisticsAsync();

        /// <summary>
        /// Recherche des catégories par nom
        /// </summary>
        Task<List<ProductCategorySelectViewModel>> SearchCategoriesAsync(string searchTerm);

        /// <summary>
        /// Récupère les catégories les plus utilisées
        /// </summary>
        Task<List<CategoryUsageViewModel>> GetMostUsedCategoriesAsync(int limit = 10);

        // ===== VALIDATION =====

        /// <summary>
        /// Vérifie l'unicité du nom d'une catégorie
        /// </summary>
        Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null);

        /// <summary>
        /// Valide les données d'une catégorie avant création/modification
        /// </summary>
        Task<ValidationResult> ValidateCategoryAsync(
            string name,
            string? description,
            int? excludeId = null);
    }

    /// <summary>
    /// Résultat d'une opération avec gestion des erreurs
    /// </summary>
    public class OperationResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        public static OperationResult<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        public static OperationResult<T> Error(string message) => new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };

        public static OperationResult<T> ValidationError(List<string> errors) => new()
        {
            IsSuccess = false,
            ValidationErrors = errors
        };
    }

    /// <summary>
    /// Résultat d'une opération sans données de retour
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        public static OperationResult Success() => new() { IsSuccess = true };
        public static OperationResult Error(string message) => new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };

        public static OperationResult ValidationError(List<string> errors) => new()
        {
            IsSuccess = false,
            ValidationErrors = errors
        };
    }

    /// <summary>
    /// Résultat de validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ValidationResult Valid() => new() { IsValid = true };
        public static ValidationResult Invalid(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    /// <summary>
    /// Statistiques d'utilisation d'une catégorie
    /// </summary>
    public class CategoryUsageViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalStockMovements { get; set; }
        public decimal TotalStockValue { get; set; }
        public DateTime LastUsed { get; set; }

        public string UsageText => $"{ProductCount} produit(s), {TotalStockMovements} mouvement(s)";
        public string ValueText => $"{TotalStockValue:N0} FCFA";
    }
}