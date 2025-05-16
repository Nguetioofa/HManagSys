using HManagSys.Models.EfModels;
using HManagSys.Models.Enums;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service de logging spécialisé pour les logs métier consultables
    /// Comme un journaliste spécialisé dans les événements hospitaliers
    /// </summary>
    public interface IApplicationLogger
    {
        // Logs d'information générale
        Task LogInfoAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null);

        // Logs d'avertissement
        Task LogWarningAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null);

        // Logs d'erreur métier (différent des erreurs système)
        Task LogErrorAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null);

        // Logs critiques nécessitant attention immédiate
        Task LogCriticalAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null);

        // Logs spécialisés pour différents domaines
        Task LogAuthenticationAsync(AuthenticationEvent eventType, string email,
            bool success, string? failureReason = null, string? ipAddress = null);

        Task LogStockMovementAsync(int productId, string movementType,
            decimal quantity, int? referenceId = null,
            int? userId = null, int? hospitalCenterId = null);

        Task LogSaleAsync(int saleId, decimal amount, int? patientId,
            int? userId = null, int? hospitalCenterId = null);

        Task LogCareEpisodeAsync(CareEpisodeEvent eventType, int episodeId,
            int patientId, int? caregiverId = null, int? hospitalCenterId = null);

        // Logs d'erreur système avec tracking de résolution
        Task LogSystemErrorAsync(Exception exception, string source,
            int? userId = null, int? hospitalCenterId = null,
            object? requestData = null, string? additionalContext = null);

        // Gestion de la résolution d'erreurs
        Task MarkErrorResolvedAsync(Guid errorId, int resolvedBy, string? notes = null);

        // Requêtes pour consultation des logs
        Task<List<ApplicationLog>> GetLogsAsync(
            DateTime? fromDate = null, DateTime? toDate = null,
            string? category = null, string? action = null,
            HManagSys.Models.Enums.LogLevel? logLevel = null, int? userId = null,
            int? hospitalCenterId = null, int pageIndex = 1, int pageSize = 50);

        Task<List<SystemErrorLog>> GetUnresolvedErrorsAsync(
            string? severity = null, DateTime? since = null);

        Task<List<ApplicationLog>> GetUserActivityAsync(int userId,
            DateTime? fromDate = null, DateTime? toDate = null);

        Task<List<ApplicationLog>> GetCenterActivityAsync(int hospitalCenterId,
            DateTime? fromDate = null, DateTime? toDate = null);
    }
}
