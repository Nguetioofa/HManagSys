using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Users;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service d'authentification avancé - Le cerveau de sécurité de notre hôpital
    /// Gère l'authentification, l'autorisation, les sessions et la sécurité globale
    /// Comme un directeur de sécurité avec une équipe de spécialistes
    /// </summary>
    public interface IAuthenticationService
    {
        // ===== AUTHENTIFICATION DE BASE =====

        /// <summary>
        /// Authentifie un utilisateur avec email et mot de passe
        /// Retourne un résultat détaillé avec les raisons d'échec
        /// Comme vérifier l'identité à l'entrée de l'hôpital
        /// </summary>
        Task<AuthenticationResult> LoginAsync(string email, string password, string? ipAddress = null);

        /// <summary>
        /// Déconnecte un utilisateur de toutes ses sessions
        /// Invalide tous les tokens et enregistre l'événement
        /// Comme récupérer tous les badges d'un employé qui part
        /// </summary>
        Task<bool> LogoutAsync(int userId, string? ipAddress = null);

        /// <summary>
        /// Déconnexion spécifique d'une session
        /// Utile pour déconnexion à distance ou gestion multi-sessions
        /// </summary>
        Task<bool> LogoutSessionAsync(string sessionToken, string? ipAddress = null);

        // ===== GESTION DES SESSIONS AVANCÉE =====

        /// <summary>
        /// Crée une nouvelle session pour un utilisateur dans un centre
        /// Génère un token sécurisé et enregistre tous les détails
        /// Comme délivrer un badge temporaire avec privilèges spécifiques
        /// </summary>
        Task<SessionInfo> CreateSessionAsync(
            int userId,
            int hospitalCenterId,
            string? ipAddress = null,
            string? userAgent = null);

        /// <summary>
        /// Valide une session existante
        /// Vérifie l'expiration, l'activité, et la validité du token
        /// Avec prolongation automatique si configurée
        /// </summary>
        Task<SessionValidationResult> ValidateSessionAsync(string sessionToken);

        /// <summary>
        /// Récupère les informations complètes d'une session
        /// Inclut l'utilisateur, le centre actuel, et les permissions
        /// Vision 360° de la session active
        /// </summary>
        Task<SessionDetails?> GetSessionDetailsAsync(string sessionToken);

        /// <summary>
        /// Prolonge une session existante
        /// Renouvelle la durée de vie sans créer une nouvelle session
        /// </summary>
        Task<bool> ExtendSessionAsync(string sessionToken, int additionalMinutes = 720);

        /// <summary>
        /// Récupère toutes les sessions actives d'un utilisateur
        /// Utile pour la gestion multi-appareils et la sécurité
        /// </summary>
        Task<List<SessionInfo>> GetUserActiveSessionsAsync(int userId);

        // ===== GESTION DES CENTRES =====

        /// <summary>
        /// Change le centre actif dans une session existante
        /// Vérifie les permissions et met à jour le contexte
        /// Comme changer de service dans l'hôpital
        /// </summary>
        Task<bool> SwitchCenterAsync(string sessionToken, int newCenterId);

        /// <summary>
        /// Récupère tous les centres accessibles à un utilisateur
        /// Avec indication du dernier centre sélectionné
        /// Pour l'écran de sélection de centre
        /// </summary>
        Task<List<CenterAssignmentInfo>> GetUserAccessibleCentersAsync(int userId);

        /// <summary>
        /// Mémorise le dernier centre sélectionné par un utilisateur
        /// Pour améliorer l'expérience utilisateur à la prochaine connexion
        /// </summary>
        Task<bool> SaveLastSelectedCenterAsync(int userId, int centerId);

        /// <summary>
        /// Récupère le dernier centre sélectionné par un utilisateur
        /// Pour proposer un choix par défaut
        /// </summary>
        Task<int?> GetLastSelectedCenterAsync(int userId);

        // ===== GESTION DES MOTS DE PASSE =====

        /// <summary>
        /// Change le mot de passe d'un utilisateur
        /// Avec validation de l'ancien mot de passe et règles de sécurité
        /// </summary>
        Task<PasswordChangeResult> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);

        /// <summary>
        /// Réinitialise le mot de passe d'un utilisateur par un administrateur
        /// Génère un mot de passe temporaire et force le changement
        /// </summary>
        Task<PasswordResetResult> ResetPasswordAsync(int userId, int resetBy);

        /// <summary>
        /// Valide un mot de passe selon les règles de sécurité
        /// Vérifie la complexité, l'historique, etc.
        /// </summary>
        Task<PasswordValidationResult> ValidatePasswordAsync(string password, int? userId = null);

        /// <summary>
        /// Force un utilisateur à changer son mot de passe
        /// Utilisé après réinitialisation ou pour des raisons de sécurité
        /// </summary>
        Task<bool> ForcePasswordChangeAsync(int userId, int forcedBy);

        // ===== SÉCURITÉ ET PERMISSIONS =====

        /// <summary>
        /// Vérifie si un utilisateur a une permission spécifique
        /// Dans un centre donné avec un rôle particulier
        /// Système de permissions granulaire
        /// </summary>
        Task<bool> HasPermissionAsync(
            int userId,
            string permission,
            int? hospitalCenterId = null);

        /// <summary>
        /// Récupère toutes les permissions d'un utilisateur
        /// Pour un centre spécifique ou global
        /// </summary>
        Task<List<string>> GetUserPermissionsAsync(int userId, int? hospitalCenterId = null);

        /// <summary>
        /// Vérifie si un utilisateur est actif et autorisé
        /// Contrôle global de l'état du compte
        /// </summary>
        Task<UserStatusCheck> CheckUserStatusAsync(int userId);

        /// <summary>
        /// Enregistre une tentative d'accès non autorisé
        /// Pour le suivi de sécurité et les alertes
        /// </summary>
        Task LogUnauthorizedAccessAttemptAsync(
            int? userId,
            string action,
            string? ipAddress = null);

        // ===== UTILITAIRES CRYPTOGRAPHIQUES =====

        /// <summary>
        /// Hash un mot de passe avec salt
        /// Utilise les meilleures pratiques de sécurité actuelles
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Vérifie un mot de passe contre son hash
        /// Résistant aux attaques de timing
        /// </summary>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Génère un mot de passe temporaire sécurisé
        /// Facile à communiquer mais suffisamment complexe
        /// </summary>
        Task<string> GenerateTemporaryPasswordAsync();

        // ===== MONITORING ET AUDIT =====

        /// <summary>
        /// Nettoie les sessions expirées
        /// Tâche de maintenance automatique
        /// </summary>
        Task<int> CleanExpiredSessionsAsync();

        /// <summary>
        /// Récupère les statistiques d'authentification
        /// Pour les tableaux de bord de sécurité
        /// </summary>
        Task<AuthenticationStatistics> GetAuthenticationStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? hospitalCenterId = null);
    }

 
}
