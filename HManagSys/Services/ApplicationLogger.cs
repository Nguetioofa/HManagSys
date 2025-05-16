using HManagSys.Data.DBContext;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.Enums;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HospitalManagementSystem.Services
{
    /// <summary>
    /// Service de logging pour les événements métier consultables
    /// Le journaliste officiel de notre hôpital numérique
    /// </summary>
    public class ApplicationLogger : IApplicationLogger
    {
        private readonly HospitalManagementContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApplicationLogger> _systemLogger;

        public ApplicationLogger(
            HospitalManagementContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApplicationLogger> systemLogger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _systemLogger = systemLogger;
        }

        // Implémentation des méthodes de base
        public async Task LogInfoAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null)
        {
            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Info, category, action, message,
                userId, hospitalCenterId, entityType, entityId, details);
        }

        public async Task LogWarningAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null)
        {
            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Warning, category, action, message,
                userId, hospitalCenterId, entityType, entityId, details);
        }

        public async Task LogErrorAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null)
        {
            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Error, category, action, message,
                userId, hospitalCenterId, entityType, entityId, details);
        }

        public async Task LogCriticalAsync(string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null)
        {
            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Critical, category, action, message,
                userId, hospitalCenterId, entityType, entityId, details);
        }

        // Logs spécialisés pour l'authentification
        public async Task LogAuthenticationAsync(AuthenticationEvent eventType, string email,
            bool success, string? failureReason = null, string? ipAddress = null)
        {
            var message = eventType switch
            {
                AuthenticationEvent.Login => success ?
                    $"Connexion réussie pour {email}" :
                    $"Échec de connexion pour {email}: {failureReason}",
                AuthenticationEvent.Logout => $"Déconnexion de {email}",
                AuthenticationEvent.PasswordReset => $"Réinitialisation du mot de passe pour {email}",
                AuthenticationEvent.PasswordChanged => $"Changement de mot de passe pour {email}",
                AuthenticationEvent.AccountLocked => $"Compte verrouillé: {email}",
                AuthenticationEvent.AccountUnlocked => $"Compte déverrouillé: {email}",
                _ => $"Événement d'authentification: {eventType} pour {email}"
            };

            var logLevel = success ? HManagSys.Models.Enums.LogLevel.Info : HManagSys.Models.Enums.LogLevel.Warning;

            await CreateLogAsync(logLevel, "Authentication", eventType.ToString(), message,
                details: new { Email = email, Success = success, FailureReason = failureReason, IpAddress = ipAddress });
        }

        // Logs pour les mouvements de stock
        public async Task LogStockMovementAsync(int productId, string movementType,
            decimal quantity, int? referenceId = null,
            int? userId = null, int? hospitalCenterId = null)
        {
            var message = $"Mouvement de stock: {movementType} - Quantité: {quantity}";

            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Info, "Stock", "Movement", message,
                userId, hospitalCenterId, "Product", productId,
                new
                {
                    ProductId = productId,
                    MovementType = movementType,
                    Quantity = quantity,
                    ReferenceId = referenceId
                });
        }

        // Logs pour les ventes
        public async Task LogSaleAsync(int saleId, decimal amount, int? patientId,
            int? userId = null, int? hospitalCenterId = null)
        {
            var message = $"Vente créée - Montant: {amount:C}";
            if (patientId.HasValue)
                message += $" - Patient ID: {patientId}";

            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Info, "Sales", "Created", message,
                userId, hospitalCenterId, "Sale", saleId,
                new { SaleId = saleId, Amount = amount, PatientId = patientId });
        }

        // Logs pour les épisodes de soins
        public async Task LogCareEpisodeAsync(CareEpisodeEvent eventType, int episodeId,
            int patientId, int? caregiverId = null, int? hospitalCenterId = null)
        {
            var message = $"Épisode de soin {eventType} - Patient ID: {patientId}";
            if (caregiverId.HasValue)
                message += $" - Soignant ID: {caregiverId}";

            await CreateLogAsync(HManagSys.Models.Enums.LogLevel.Info, "Care", eventType.ToString(), message,
                caregiverId, hospitalCenterId, "CareEpisode", episodeId,
                new { EpisodeId = episodeId, PatientId = patientId, CaregiverId = caregiverId });
        }

        // Logs d'erreur système avec tracking de résolution
        public async Task LogSystemErrorAsync(Exception exception, string source,
            int? userId = null, int? hospitalCenterId = null,
            object? requestData = null, string? additionalContext = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var errorLog = new SystemErrorLog
                {
                    ErrorId = Guid.NewGuid(),
                    UserId = userId,
                    HospitalCenterId = hospitalCenterId,
                    Severity = DetermineSeverity(exception),
                    Source = source,
                    ErrorType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.ToString(),
                    RequestData = requestData != null ? JsonSerializer.Serialize(requestData) : null,
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    RequestPath = httpContext?.Request.Path.ToString(),
                    Timestamp = TimeZoneHelper.GetCameroonTime(),
                    IsResolved = false
                };

                _context.SystemErrorLogs.Add(errorLog);
                await _context.SaveChangesAsync();

                // Log aussi dans le système de logs normal pour les développeurs
                _systemLogger.LogError(exception,
                    "Erreur système enregistrée avec ID {ErrorId} dans {Source}. Context: {Context}",
                    errorLog.ErrorId, source, additionalContext);
            }
            catch (Exception logException)
            {
                // En cas d'erreur de logging, utiliser le système de logs normal
                _systemLogger.LogError(logException,
                    "Erreur lors de l'enregistrement d'une erreur système. Erreur originale: {OriginalError}",
                    exception.Message);
            }
        }

        public async Task MarkErrorResolvedAsync(Guid errorId, int resolvedBy, string? notes = null)
        {
            var error = await _context.SystemErrorLogs
                .FirstOrDefaultAsync(e => e.ErrorId == errorId);

            if (error != null)
            {
                error.IsResolved = true;
                error.ResolvedBy = resolvedBy;
                error.ResolvedAt = TimeZoneHelper.GetCameroonTime();
                error.ResolutionNotes = notes;

                await _context.SaveChangesAsync();

                await LogInfoAsync("System", "ErrorResolved",
                    $"Erreur {errorId} résolue", resolvedBy,
                    details: new { ErrorId = errorId, Notes = notes });
            }
        }

        // Méthodes de requête pour consultation
        public async Task<List<ApplicationLog>> GetLogsAsync(
            DateTime? fromDate = null, DateTime? toDate = null,
            string? category = null, string? action = null,
            HManagSys.Models.Enums.LogLevel? logLevel = null, int? userId = null,
            int? hospitalCenterId = null, int pageIndex = 1, int pageSize = 50)
        {
            var query = _context.ApplicationLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.Timestamp <= toDate.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(l => l.Category == category);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (logLevel.HasValue)
                query = query.Where(l => l.LogLevel == logLevel.ToString());

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId);

            if (hospitalCenterId.HasValue)
                query = query.Where(l => l.HospitalCenterId == hospitalCenterId);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<SystemErrorLog>> GetUnresolvedErrorsAsync(
            string? severity = null, DateTime? since = null)
        {
            var query = _context.SystemErrorLogs
                .Where(e => !e.IsResolved);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);

            if (since.HasValue)
                query = query.Where(e => e.Timestamp >= since);

            return await query
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        public async Task<List<ApplicationLog>> GetUserActivityAsync(int userId,
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetLogsAsync(fromDate, toDate, userId: userId);
        }

        public async Task<List<ApplicationLog>> GetCenterActivityAsync(int hospitalCenterId,
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetLogsAsync(fromDate, toDate, hospitalCenterId: hospitalCenterId);
        }

        // Méthodes privées utilitaires
        private async Task CreateLogAsync(HManagSys.Models.Enums.LogLevel level, string category, string action, string message,
            int? userId = null, int? hospitalCenterId = null,
            string? entityType = null, int? entityId = null,
            object? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var log = new ApplicationLog
                {
                    UserId = userId,
                    HospitalCenterId = hospitalCenterId,
                    LogLevel = level.ToString(),
                    Category = category,
                    Action = action,
                    Message = message,
                    Details = details != null ? JsonSerializer.Serialize(details) : null,
                    EntityType = entityType,
                    EntityId = entityId,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                    RequestPath = httpContext?.Request.Path.ToString(),
                    Timestamp = TimeZoneHelper.GetCameroonTime()
                };

                _context.ApplicationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // En cas d'erreur de logging, utiliser le système de logs normal
                _systemLogger.LogError(ex,
                    "Erreur lors de l'enregistrement d'un log applicatif: {Category}.{Action} - {Message}",
                    category, action, message);
            }
        }

        private static string DetermineSeverity(Exception exception)
        {
            // Logique pour déterminer la sévérité basée sur le type d'exception
            return exception switch
            {
                UnauthorizedAccessException => "Medium",
                InvalidOperationException => "Medium",
                ArgumentException => "Low",
                NullReferenceException => "High",
                OutOfMemoryException => "Critical",
                StackOverflowException => "Critical",
                _ => "Medium"
            };
        }
    }
}