using HManagSys.Models.EfModels;
using HManagSys.Models.Enums;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service d'audit complet - L'historien de notre hôpital numérique
    /// Enregistre, analyse et reporte toutes les activités critiques
    /// Comme un système de surveillance intelligent avec mémoire parfaite
    /// </summary>
    public interface IAuditService
    {
        // ===== ENREGISTREMENT D'AUDITS GÉNÉRAUX =====

        /// <summary>
        /// Enregistre une action d'audit générale
        /// Point d'entrée principal pour tout événement auditable
        /// Comme noter un événement dans le registre principal
        /// </summary>
        Task LogActionAsync(
            int? userId,
            string actionType,
            string entityType,
            int? entityId = null,
            object? oldValues = null,
            object? newValues = null,
            string? description = null,
            string? ipAddress = null,
            int? hospitalCenterId = null,
            Dictionary<string, object>? additionalProperties = null);

        // ===== AUDITS SPÉCIALISÉS UTILISATEURS =====

        /// <summary>
        /// Enregistre la création d'un utilisateur
        /// Avec détails complets pour la conformité RH
        /// </summary>
        Task LogUserCreatedAsync(
            int createdUserId,
            int createdBy,
            User userData,
            string? ipAddress = null);

        /// <summary>
        /// Enregistre la modification d'un utilisateur
        /// Avec comparaison avant/après des données sensibles
        /// </summary>
        Task LogUserModifiedAsync(
            int modifiedUserId,
            int modifiedBy,
            User oldValues,
            User newValues,
            string? ipAddress = null);

        /// <summary>
        /// Enregistre les changements d'affectation de centres
        /// Crucial pour la traçabilité des responsabilités
        /// </summary>
        Task LogUserCenterAssignmentChangedAsync(
            int userId,
            int hospitalCenterId,
            string oldRole,
            string newRole,
            int modifiedBy,
            string? ipAddress = null);

        // ===== AUDITS SÉCURITÉ =====

        /// <summary>
        /// Enregistre les événements de connexion/déconnexion
        /// Avec détails géographiques et techniques
        /// </summary>
        Task LogAuthenticationEventAsync(
            int userId,
            AuthenticationEvent eventType,
            bool success,
            string? ipAddress = null,
            string? userAgent = null,
            string? failureReason = null);

        /// <summary>
        /// Enregistre les changements/réinitialisations de mot de passe
        /// Événement de sécurité majeur nécessitant audit complet
        /// </summary>
        Task LogPasswordChangeAsync(
            int userId,
            PasswordChangeType changeType,
            int? changedBy = null,
            string? ipAddress = null);

        /// <summary>
        /// Enregistre les changements de centre/contexte
        /// Pour tracer les mouvements dans l'hôpital
        /// </summary>
        Task LogCenterSwitchAsync(
            int userId,
            int fromCenterId,
            int toCenterId,
            string? ipAddress = null);

        // ===== AUDITS MÉTIER =====

        /// <summary>
        /// Enregistre les opérations sur les stocks
        /// Traçabilité complète pour la pharmacie hospitalière
        /// </summary>
        Task LogStockOperationAsync(
            int productId,
            int hospitalCenterId,
            string operationType,
            decimal quantityChange,
            decimal newQuantity,
            int? referenceId = null,
            string? referenceType = null,
            int? userId = null);

        /// <summary>
        /// Enregistre la création/modification de ventes
        /// Audit financier essentiel
        /// </summary>
        Task LogSaleOperationAsync(
            int saleId,
            string operationType,
            decimal amount,
            int? patientId = null,
            int? soldBy = null,
            int? hospitalCenterId = null);

        /// <summary>
        /// Enregistre les opérations sur les épisodes de soins
        /// Traçabilité médicale complète
        /// </summary>
        Task LogCareEpisodeOperationAsync(
            int episodeId,
            int patientId,
            string operationType,
            int? caregiverId = null,
            int? hospitalCenterId = null,
            object? additionalData = null);

        /// <summary>
        /// Enregistre les prescriptions et leurs modifications
        /// Audit médical critique
        /// </summary>
        Task LogPrescriptionOperationAsync(
            int prescriptionId,
            int patientId,
            string operationType,
            int? prescribedBy = null,
            int? hospitalCenterId = null);

        /// <summary>
        /// Enregistre les opérations sur les examens médicaux
        /// Suivi complet du parcours d'examen
        /// </summary>
        Task LogExaminationOperationAsync(
            int examinationId,
            int patientId,
            string operationType,
            int? requestedBy = null,
            int? performedBy = null,
            int? hospitalCenterId = null);

        // ===== AUDITS SYSTÈME =====

        /// <summary>
        /// Enregistre les erreurs système critiques
        /// Intégration avec le système de logging pour suivi
        /// </summary>
        Task LogSystemErrorAsync(
            Exception exception,
            string source,
            int? userId = null,
            int? hospitalCenterId = null,
            object? contextData = null);

        /// <summary>
        /// Enregistre les modifications de configuration système
        /// Changements critiques nécessitant approbation
        /// </summary>
        Task LogSystemConfigurationChangeAsync(
            string configurationKey,
            object? oldValue,
            object? newValue,
            int changedBy,
            string? ipAddress = null);

        // ===== CONSULTATION ET RECHERCHE =====

        /// <summary>
        /// Récupère l'historique d'audit d'un utilisateur
        /// Vision temporelle des actions d'un utilisateur
        /// </summary>
        Task<List<AuditLog>> GetUserAuditTrailAsync(
            int userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? actionType = null);

        /// <summary>
        /// Récupère l'historique d'audit d'une entité spécifique
        /// Biographie complète d'un objet du système
        /// </summary>
        Task<List<AuditLog>> GetEntityAuditTrailAsync(
            string entityType,
            int entityId,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Récupère l'audit d'activité d'un centre
        /// Vision globale de l'activité dans un centre
        /// </summary>
        Task<List<AuditLog>> GetCenterAuditTrailAsync(
            int hospitalCenterId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? actionType = null);

        /// <summary>
        /// Recherche dans les audits avec critères multiples
        /// Requête flexible pour investigations et rapports
        /// </summary>
        Task<(List<AuditLog> AuditLogs, int TotalCount)> SearchAuditLogsAsync(
            string? searchTerm = null,
            int? userId = null,
            string? actionType = null,
            string? entityType = null,
            int? entityId = null,
            int? hospitalCenterId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 50);

        // ===== RAPPORTS ET ANALYSES =====

        /// <summary>
        /// Génère un rapport d'activité par utilisateur
        /// Analyse comportementale et patterns d'utilisation
        /// </summary>
        Task<UserActivityReport> GenerateUserActivityReportAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate);

        /// <summary>
        /// Génère un rapport d'activité par centre
        /// Vue d'ensemble opérationnelle du centre
        /// </summary>
        Task<CenterActivityReport> GenerateCenterActivityReportAsync(
            int hospitalCenterId,
            DateTime fromDate,
            DateTime toDate);

        /// <summary>
        /// Génère un rapport de sécurité
        /// Analyse des événements de sécurité et anomalies
        /// </summary>
        Task<SecurityAuditReport> GenerateSecurityReportAsync(
            DateTime fromDate,
            DateTime toDate,
            int? hospitalCenterId = null);

        /// <summary>
        /// Détecte les activités suspectes automatiquement
        /// Analysis intelligent des patterns anormaux
        /// </summary>
        Task<List<SuspiciousActivity>> DetectSuspiciousActivitiesAsync(
            DateTime fromDate,
            DateTime toDate,
            int? hospitalCenterId = null);

        // ===== MAINTENANCE ET OPTIMISATION =====

        /// <summary>
        /// Archive les anciens logs d'audit
        /// Gestion du cycle de vie des données d'audit
        /// </summary>
        Task<int> ArchiveOldAuditLogsAsync(DateTime beforeDate);

        /// <summary>
        /// Valide l'intégrité des logs d'audit
        /// Vérification de non-altération des données critiques
        /// </summary>
        Task<AuditIntegrityReport> ValidateAuditIntegrityAsync(
            DateTime fromDate,
            DateTime toDate);
    }

    // ===== ENUMS ET CLASSES SUPPORT =====

    /// <summary>
    /// Types de changement de mot de passe
    /// Pour classification fine des événements de sécurité
    /// </summary>
    public enum PasswordChangeType
    {
        UserInitiated,      // Utilisateur change son propre mot de passe
        AdminReset,         // Administrateur réinitialise
        SystemForced,       // Système force (après expiration)
        SecurityIncident    // Après incident de sécurité
    }

    /// <summary>
    /// Rapport d'activité utilisateur
    /// Analyse comportementale complète
    /// </summary>
    public class UserActivityReport
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalActions { get; set; }
        public int LoginCount { get; set; }
        public int SalesCreated { get; set; }
        public int CareServicesProvided { get; set; }
        public int PrescriptionsIssued { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public List<string> MostFrequentActions { get; set; } = new();
        public List<int> CentersVisited { get; set; } = new();
        public Dictionary<string, int> ActionsByHour { get; set; } = new();
    }

    /// <summary>
    /// Rapport d'activité centre
    /// Vue opérationnelle globale
    /// </summary>
    public class CenterActivityReport
    {
        public int HospitalCenterId { get; set; }
        public string CenterName { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalActions { get; set; }
        public int UniqueUsersActive { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int StockMovements { get; set; }
        public int CareEpisodesCreated { get; set; }
        public int ExaminationsPerformed { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public List<(int UserId, string UserName, int ActionCount)> TopActiveUsers { get; set; } = new();
    }

    /// <summary>
    /// Rapport de sécurité
    /// Analyse des événements de sécurité
    /// </summary>
    public class SecurityAuditReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalLoginAttempts { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int PasswordResets { get; set; }
        public int AccountLockouts { get; set; }
        public int UnauthorizedAccessAttempts { get; set; }
        public int SuspiciousActivities { get; set; }
        public List<(string IpAddress, int AttemptCount)> TopFailedIPAddresses { get; set; } = new();
        public List<(int UserId, string UserName, int FailedAttempts)> UsersWithFailedLogins { get; set; } = new();
        public List<string> SecurityRecommendations { get; set; } = new();
    }

    /// <summary>
    /// Activité suspecte détectée
    /// Alertes de sécurité intelligentes
    /// </summary>
    public class SuspiciousActivity
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        public DateTime DetectedAt { get; set; }
        public string? IpAddress { get; set; }
        public Dictionary<string, object> Evidence { get; set; } = new();
        public bool IsResolved { get; set; }
        public string? ResolutionNotes { get; set; }
    }

    /// <summary>
    /// Rapport d'intégrité des audits
    /// Vérification de non-altération
    /// </summary>
    public class AuditIntegrityReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalAuditLogs { get; set; }
        public int VerifiedLogs { get; set; }
        public int SuspiciousLogs { get; set; }
        public bool IntegrityValid { get; set; }
        public List<string> IntegrityIssues { get; set; } = new();
        public List<int> SuspiciousAuditIds { get; set; } = new();
    }
}
