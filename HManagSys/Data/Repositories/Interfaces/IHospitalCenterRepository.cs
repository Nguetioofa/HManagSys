using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.HospitalCenter;
using HManagSys.Services.Interfaces;

namespace HManagSys.Data.Repositories.Interfaces
{
    /// <summary>
    /// Interface spécialisée pour la gestion des centres hospitaliers
    /// Comme un directeur des opérations qui supervise tous les établissements
    /// du réseau hospitalier
    /// </summary>
    public interface IHospitalCenterRepository : IGenericRepository<HospitalCenter>
    {
        // ===== RECHERCHES SPÉCIALISÉES =====

        /// <summary>
        /// Récupère un centre avec ses statistiques de base
        /// Comme obtenir un rapport d'activité d'un établissement
        /// </summary>
        Task<HospitalCenterWithStats?> GetCenterWithStatsAsync(int centerId);

        /// <summary>
        /// Recherche des centres avec filtres
        /// Permet de filtrer par statut, région, etc.
        /// </summary>
        Task<List<HospitalCenter>> SearchCentersAsync(
            string? searchTerm = null,
            bool? isActive = null,
            string? region = null);

        /// <summary>
        /// Récupère les centres accessibles à un utilisateur
        /// Essentiel pour les interfaces de sélection de centre
        /// </summary>
        Task<List<HospitalCenter>> GetUserAccessibleCentersAsync(int userId);

        // ===== GESTION DES CENTRES =====

        /// <summary>
        /// Active ou désactive un centre avec gestion des dépendances
        /// Processus sécurisé qui vérifie les impacts avant modification
        /// </summary>
        Task<(bool Success, string? Warning)> SetCenterActiveStatusAsync(
            int centerId, bool isActive, int modifiedBy);

        // ===== RAPPORTS ET STATISTIQUES =====

        /// <summary>
        /// Génère un rapport d'activité pour un centre
        /// Vue d'ensemble des opérations sur une période
        /// </summary>
        Task<HManagSys.Models.ViewModels.HospitalCenter.CenterActivityReport> GenerateActivityReportAsync(
            int centerId, DateTime fromDate, DateTime toDate);

        /// <summary>
        /// Récupère les statistiques globales de tous les centres
        /// Pour les tableaux de bord de direction
        /// </summary>
        Task<NetworkStatistics> GetNetworkStatisticsAsync();
    }
}