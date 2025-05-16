using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;

namespace HManagSys.Data.Repositories.Interfaces
{
    /// <summary>
    /// Interface spécialisée pour la gestion des utilisateurs
    /// Hérite de toutes les capacités du repository générique
    /// et ajoute des fonctionnalités spécifiques aux utilisateurs
    /// Comme un bibliothécaire spécialisé dans les dossiers du personnel
    /// </summary>
    public interface IUserRepository : IGenericRepository<User>
    {
        // ===== RECHERCHES SPÉCIALISÉES =====

        /// <summary>
        /// Recherche un utilisateur par son email
        /// Méthode essentielle pour l'authentification
        /// Comme retrouver un dossier par le nom de famille
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Recherche des utilisateurs avec critères multiples
        /// Plus puissant que la recherche générique, adapté aux besoins RH
        /// </summary>
        Task<(List<UserSummary> Users, int TotalCount)> SearchUsersAsync(
            string? searchTerm = null,
            bool? isActive = null,
            string? roleFilter = null,
            int? hospitalCenterId = null,
            int pageIndex = 1,
            int pageSize = 20);

        /// <summary>
        /// Récupère les utilisateurs récemment actifs
        /// Utile pour les rapports d'activité et la gestion RH
        /// </summary>
        Task<List<User>> GetRecentlyActiveUsersAsync(int days = 30, int? hospitalCenterId = null);

        /// <summary>
        /// Récupère les utilisateurs par rôle dans un centre spécifique
        /// Essentiel pour la gestion des équipes par centre
        /// </summary>
        Task<List<User>> GetUsersByRoleAndCenterAsync(string roleType, int hospitalCenterId);

        // ===== GESTION DES MOTS DE PASSE =====

        /// <summary>
        /// Réinitialise le mot de passe d'un utilisateur
        /// Avec enregistrement automatique de l'audit et du log
        /// Comme délivrer un nouveau badge d'accès
        /// </summary>
        Task<(bool Success, string TempPassword)> ResetPasswordAsync(int userId, int resetBy);

        /// <summary>
        /// Change le mot de passe d'un utilisateur
        /// Avec validation de l'ancien mot de passe
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Force un utilisateur à changer son mot de passe
        /// Utilisé après une réinitialisation ou pour des raisons de sécurité
        /// </summary>
        Task<bool> ForcePasswordChangeAsync(int userId, bool mustChange = true);

        // ===== GESTION D'ACTIVITÉ =====

        /// <summary>
        /// Enregistre une connexion réussie
        /// Met à jour LastLoginDate et log l'événement
        /// Comme pointer à l'entrée de l'hôpital
        /// </summary>
        Task<bool> RecordLoginAsync(int userId, string? ipAddress = null);

        /// <summary>
        /// Active ou désactive un compte utilisateur
        /// Soft delete/restore avec logging automatique
        /// </summary>
        Task<bool> SetUserActiveStatusAsync(int userId, bool isActive, int modifiedBy);

        // ===== CENTRES ET AFFECTATIONS =====

        /// <summary>
        /// Récupère un utilisateur avec toutes ses affectations
        /// Inclut les centres et rôles associés
        /// Vision complète du profil utilisateur
        /// </summary>
        Task<User?> GetUserWithAssignmentsAsync(int userId);

        /// <summary>
        /// Récupère les affectations actives d'un utilisateur
        /// Pour la sélection de centre à la connexion
        /// </summary>
        Task<List<CenterAssignmentInfo>> GetUserActiveCentersAsync(int userId);

        /// <summary>
        /// Vérifie si un utilisateur a accès à un centre avec un rôle spécifique
        /// Essentiel pour les contrôles d'autorisation
        /// </summary>
        Task<bool> HasAccessToCenterAsync(int userId, int hospitalCenterId, string? roleType = null);

        // ===== STATISTIQUES ET RAPPORTS =====

        /// <summary>
        /// Obtient des statistiques sur les utilisateurs
        /// Pour les tableaux de bord administratifs
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync(int? hospitalCenterId = null);

        /// <summary>
        /// Récupère l'historique des connexions d'un utilisateur
        /// Pour l'audit de sécurité
        /// </summary>
        Task<List<UserLoginHistory>> GetUserLoginHistoryAsync(int userId, int days = 30);
    }

    // ===== CLASSES DE SUPPORT =====


}
