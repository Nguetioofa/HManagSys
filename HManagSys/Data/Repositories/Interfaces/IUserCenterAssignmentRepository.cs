using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HospitalManagementSystem.Data.Repositories;

namespace HManagSys.Data.Repositories.Interfaces
{
    /// <summary>
    /// Interface spécialisée pour la gestion des affectations utilisateur-centre
    /// Comme un responsable RH expert en affectations et mutations
    /// </summary>
    public interface IUserCenterAssignmentRepository : IGenericRepository<UserCenterAssignment>
    {
        // ===== RECHERCHES ET CONSULTATIONS =====

        /// <summary>
        /// Récupère toutes les affectations actives d'un utilisateur
        /// Vue complète des responsabilités de l'utilisateur
        /// </summary>
        Task<List<UserCenterAssignment>> GetUserActiveAssignmentsAsync(int userId);

        /// <summary>
        /// Récupère tous les utilisateurs affectés à un centre avec leurs rôles
        /// Liste d'équipe pour un centre spécifique
        /// </summary>
        Task<List<UserCenterAssignment>> GetCenterActiveAssignmentsAsync(int hospitalCenterId);

        /// <summary>
        /// Vérifie si un utilisateur a une affectation spécifique
        /// Contrôle d'autorisation essentiel à la sécurité
        /// </summary>
        Task<bool> HasAssignmentAsync(int userId, int hospitalCenterId, string? roleType = null);

        /// <summary>
        /// Récupère une affectation spécifique
        /// </summary>
        Task<UserCenterAssignment?> GetAssignmentAsync(int userId, int hospitalCenterId);

        // ===== GESTION DES AFFECTATIONS =====

        /// <summary>
        /// Crée une nouvelle affectation avec validation
        /// Processus contrôlé pour éviter les doublons et conflits
        /// </summary>
        Task<UserCenterAssignment> CreateAssignmentAsync(
            int userId, int hospitalCenterId, string roleType, int createdBy);

        /// <summary>
        /// Met à jour le rôle d'une affectation existante
        /// Changement de responsabilités avec audit complet
        /// </summary>
        Task<bool> UpdateRoleAsync(int userId, int hospitalCenterId, string newRoleType, int modifiedBy);

        /// <summary>
        /// Termine une affectation avec date de fin
        /// Processus de départ ou mutation avec conservation de l'historique
        /// </summary>
        Task<bool> EndAssignmentAsync(int userId, int hospitalCenterId, int endedBy, DateTime? endDate = null);

        // ===== OPÉRATIONS EN BATCH =====

        /// <summary>
        /// Termine toutes les affectations d'un utilisateur
        /// Utilisé lors de la désactivation d'un compte
        /// </summary>
        Task<bool> EndAllUserAssignmentsAsync(int userId, int endedBy);

        /// <summary>
        /// Termine toutes les affectations d'un centre
        /// Utilisé lors de la fermeture temporaire d'un centre
        /// </summary>
        Task<bool> EndAllCenterAssignmentsAsync(int hospitalCenterId, int endedBy);

        // ===== RECHERCHES AVANCÉES =====

        /// <summary>
        /// Recherche des affectations avec filtres multiples
        /// Outil d'analyse pour la gestion RH
        /// </summary>
        Task<(List<AssignmentDetails> Assignments, int TotalCount)> SearchAssignmentsAsync(
            string? searchTerm = null,
            string? roleType = null,
            int? hospitalCenterId = null,
            bool? isActive = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Génère des statistiques sur les affectations
        /// Pour les tableaux de bord RH
        /// </summary>
        Task<AssignmentStatistics> GetAssignmentStatisticsAsync(int? hospitalCenterId = null);
    }
}