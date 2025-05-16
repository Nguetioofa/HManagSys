using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Data.Repositories.Interfaces
{
    /// <summary>
    /// Interface spécialisée pour la gestion des catégories de produits
    /// Comme un bibliothécaire spécialisé dans la classification des ouvrages
    /// </summary>
    public interface IProductCategoryRepository : IGenericRepository<ProductCategory>
    {
        // ===== RECHERCHES SPÉCIALISÉES =====

        /// <summary>
        /// Récupère toutes les catégories actives
        /// Utilisé pour les listes déroulantes et les filtres
        /// </summary>
        Task<List<ProductCategory>> GetActiveCategoriesAsync();

        /// <summary>
        /// Recherche des catégories avec critères
        /// Pour l'interface d'administration avec filtres
        /// </summary>
        Task<(List<ProductCategory> Categories, int TotalCount)> SearchCategoriesAsync(
            string? searchTerm = null,
            bool? isActive = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Récupère une catégorie avec ses produits
        /// Pour voir l'impact avant suppression/modification
        /// </summary>
        Task<ProductCategory?> GetCategoryWithProductsAsync(int categoryId);

        // ===== OPÉRATIONS SPÉCIALISÉES =====

        /// <summary>
        /// Vérifie si une catégorie peut être supprimée
        /// Une catégorie ne peut être supprimée si elle a des produits associés
        /// </summary>
        Task<bool> CanDeleteCategoryAsync(int categoryId);

        /// <summary>
        /// Active/désactive une catégorie
        /// Avec gestion des produits associés
        /// </summary>
        Task<bool> SetCategoryActiveStatusAsync(int categoryId, bool isActive, int modifiedBy);

        /// <summary>
        /// Récupère les statistiques d'une catégorie
        /// Nombre de produits, statistiques de stock, etc.
        /// </summary>
        Task<CategoryStatistics> GetCategoryStatisticsAsync(int categoryId);

        // ===== VALIDATION =====

        /// <summary>
        /// Vérifie si un nom de catégorie existe déjà
        /// Pour éviter les doublons lors de la création/modification
        /// </summary>
        Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null);
    }


}