using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.Enums;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HManagSys.Services
{
    /// <summary>
    /// Service d'audit complet - L'historien de notre hôpital numérique
    /// Enregistre, analyse et reporte toutes les activités critiques
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly HospitalManagementContext _context;
        private readonly IApplicationLogger _appLogger;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            HospitalManagementContext context,
            IApplicationLogger appLogger,
            ILogger<AuditService> logger)
        {
            _context = context;
            _appLogger = appLogger;
            _logger = logger;
        }

        // ===== ENREGISTREMENT D'AUDITS GÉNÉRAUX =====

        public async Task LogActionAsync(
            int? userId,
            string actionType,
            string entityType,
            int? entityId = null,
            object? oldValues = null,
            object? newValues = null,
            string? description = null,
            string? ipAddress = null,
            int? hospitalCenterId = null,
            Dictionary<string, object>? additionalProperties = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    Description = description,
                    IpAddress = ipAddress,
                    HospitalCenterId = hospitalCenterId,
                    ActionDate = TimeZoneHelper.GetCameroonTime()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Log aussi dans le système d'application logs
                await _appLogger.LogInfoAsync("Audit", actionType,
                    description ?? $"{actionType} sur {entityType}",
                    userId, hospitalCenterId, entityType, entityId,
                    additionalProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement d'audit pour {ActionType} sur {EntityType}",
                    actionType, entityType);
                throw;
            }
        }

        // ===== AUDITS SPÉCIALISÉS UTILISATEURS =====

        public async Task LogUserCreatedAsync(
            int createdUserId,
            int createdBy,
            User userData,
            string? ipAddress = null)
        {
            try
            {
                var sanitizedData = new
                {
                    userData.Id,
                    userData.FirstName,
                    userData.LastName,
                    userData.Email,
                    userData.PhoneNumber,
                    userData.IsActive
                    // Pas de mot de passe dans l'audit
                };

                await LogActionAsync(
                    createdBy,
                    "UserCreated",
                    "User",
                    createdUserId,
                    null,
                    sanitizedData,
                    $"Utilisateur créé: {userData.FirstName} {userData.LastName} ({userData.Email})",
                    ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de création d'utilisateur {UserId}", createdUserId);
                throw;
            }
        }

        public async Task LogUserModifiedAsync(
            int modifiedUserId,
            int modifiedBy,
            User oldValues,
            User newValues,
            string? ipAddress = null)
        {
            try
            {
                var oldData = new
                {
                    oldValues.FirstName,
                    oldValues.LastName,
                    oldValues.Email,
                    oldValues.PhoneNumber,
                    oldValues.IsActive
                };

                var newData = new
                {
                    newValues.FirstName,
                    newValues.LastName,
                    newValues.Email,
                    newValues.PhoneNumber,
                    newValues.IsActive
                };

                var changes = GetChangedProperties(oldData, newData);
                var description = $"Utilisateur modifié: {newValues.FirstName} {newValues.LastName}. " +
                                 $"Changements: {string.Join(", ", changes)}";

                await LogActionAsync(
                    modifiedBy,
                    "UserModified",
                    "User",
                    modifiedUserId,
                    oldData,
                    newData,
                    description,
                    ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de modification d'utilisateur {UserId}", modifiedUserId);
                throw;
            }
        }

        public async Task LogUserCenterAssignmentChangedAsync(
            int userId,
            int hospitalCenterId,
            string oldRole,
            string newRole,
            int modifiedBy,
            string? ipAddress = null)
        {
            try
            {
                await LogActionAsync(
                    modifiedBy,
                    "UserCenterAssignmentChanged",
                    "UserCenterAssignment",
                    null,
                    new { UserId = userId, HospitalCenterId = hospitalCenterId, Role = oldRole },
                    new { UserId = userId, HospitalCenterId = hospitalCenterId, Role = newRole },
                    $"Affectation modifiée - Utilisateur {userId} au centre {hospitalCenterId}: {oldRole} → {newRole}",
                    ipAddress,
                    hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de changement d'affectation pour utilisateur {UserId}", userId);
                throw;
            }
        }

        // ===== AUDITS SÉCURITÉ =====

        public async Task LogAuthenticationEventAsync(
            int userId,
            AuthenticationEvent eventType,
            bool success,
            string? ipAddress = null,
            string? userAgent = null,
            string? failureReason = null)
        {
            try
            {
                var description = eventType switch
                {
                    AuthenticationEvent.Login => success ? "Connexion réussie" : $"Échec de connexion: {failureReason}",
                    AuthenticationEvent.Logout => "Déconnexion",
                    AuthenticationEvent.PasswordReset => "Réinitialisation de mot de passe",
                    AuthenticationEvent.PasswordChanged => "Changement de mot de passe",
                    AuthenticationEvent.AccountLocked => "Compte verrouillé",
                    AuthenticationEvent.AccountUnlocked => "Compte déverrouillé",
                    _ => eventType.ToString()
                };

                var additionalData = new Dictionary<string, object>
                {
                    ["Success"] = success,
                    ["IpAddress"] = ipAddress ?? "Unknown",
                    ["UserAgent"] = userAgent ?? "Unknown"
                };

                if (!success && !string.IsNullOrEmpty(failureReason))
                {
                    additionalData["FailureReason"] = failureReason;
                }

                await LogActionAsync(
                    userId,
                    eventType.ToString(),
                    "Authentication",
                    userId,
                    null,
                    additionalData,
                    description,
                    ipAddress,
                    additionalProperties: additionalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'événement d'authentification pour utilisateur {UserId}", userId);
                throw;
            }
        }

        public async Task LogPasswordChangeAsync(
            int userId,
            PasswordChangeType changeType,
            int? changedBy = null,
            string? ipAddress = null)
        {
            try
            {
                var description = changeType switch
                {
                    PasswordChangeType.UserInitiated => "Changement de mot de passe par l'utilisateur",
                    PasswordChangeType.AdminReset => $"Réinitialisation par administrateur {changedBy}",
                    PasswordChangeType.SystemForced => "Changement forcé par le système",
                    PasswordChangeType.SecurityIncident => "Changement suite à incident de sécurité",
                    _ => "Changement de mot de passe"
                };

                await LogActionAsync(
                    changedBy ?? userId,
                    "PasswordChange",
                    "User",
                    userId,
                    null,
                    new { ChangeType = changeType.ToString(), ChangedBy = changedBy },
                    description,
                    ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de changement de mot de passe pour utilisateur {UserId}", userId);
                throw;
            }
        }

        public async Task LogCenterSwitchAsync(
            int userId,
            int fromCenterId,
            int toCenterId,
            string? ipAddress = null)
        {
            try
            {
                await LogActionAsync(
                    userId,
                    "CenterSwitch",
                    "UserSession",
                    null,
                    new { FromCenterId = fromCenterId },
                    new { ToCenterId = toCenterId },
                    $"Changement de centre: {fromCenterId} → {toCenterId}",
                    ipAddress,
                    toCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de changement de centre pour utilisateur {UserId}", userId);
                throw;
            }
        }

        // ===== AUDITS MÉTIER =====

        public async Task LogStockOperationAsync(
            int productId,
            int hospitalCenterId,
            string operationType,
            decimal quantityChange,
            decimal newQuantity,
            int? referenceId = null,
            string? referenceType = null,
            int? userId = null)
        {
            try
            {
                var operationData = new
                {
                    ProductId = productId,
                    HospitalCenterId = hospitalCenterId,
                    OperationType = operationType,
                    QuantityChange = quantityChange,
                    NewQuantity = newQuantity,
                    ReferenceId = referenceId,
                    ReferenceType = referenceType
                };

                await LogActionAsync(
                    userId,
                    "StockOperation",
                    "StockMovement",
                    referenceId,
                    null,
                    operationData,
                    $"Opération de stock: {operationType} - Produit {productId}, Quantité: {quantityChange}, Nouveau stock: {newQuantity}",
                    hospitalCenterId: hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'opération de stock pour produit {ProductId}", productId);
                throw;
            }
        }

        public async Task LogSaleOperationAsync(
            int saleId,
            string operationType,
            decimal amount,
            int? patientId = null,
            int? soldBy = null,
            int? hospitalCenterId = null)
        {
            try
            {
                var saleData = new
                {
                    SaleId = saleId,
                    OperationType = operationType,
                    Amount = amount,
                    PatientId = patientId,
                    SoldBy = soldBy,
                    HospitalCenterId = hospitalCenterId
                };

                await LogActionAsync(
                    soldBy,
                    "SaleOperation",
                    "Sale",
                    saleId,
                    null,
                    saleData,
                    $"Opération de vente: {operationType} - Montant: {amount:C}",
                    hospitalCenterId: hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'opération de vente {SaleId}", saleId);
                throw;
            }
        }

        public async Task LogCareEpisodeOperationAsync(
            int episodeId,
            int patientId,
            string operationType,
            int? caregiverId = null,
            int? hospitalCenterId = null,
            object? additionalData = null)
        {
            try
            {
                var careData = new
                {
                    EpisodeId = episodeId,
                    PatientId = patientId,
                    OperationType = operationType,
                    CaregiverId = caregiverId,
                    HospitalCenterId = hospitalCenterId,
                    AdditionalData = additionalData
                };

                await LogActionAsync(
                    caregiverId,
                    "CareEpisodeOperation",
                    "CareEpisode",
                    episodeId,
                    null,
                    careData,
                    $"Opération de soin: {operationType} - Épisode {episodeId}, Patient {patientId}",
                    hospitalCenterId: hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'opération de soin {EpisodeId}", episodeId);
                throw;
            }
        }

        public async Task LogPrescriptionOperationAsync(
            int prescriptionId,
            int patientId,
            string operationType,
            int? prescribedBy = null,
            int? hospitalCenterId = null)
        {
            try
            {
                var prescriptionData = new
                {
                    PrescriptionId = prescriptionId,
                    PatientId = patientId,
                    OperationType = operationType,
                    PrescribedBy = prescribedBy,
                    HospitalCenterId = hospitalCenterId
                };

                await LogActionAsync(
                    prescribedBy,
                    "PrescriptionOperation",
                    "Prescription",
                    prescriptionId,
                    null,
                    prescriptionData,
                    $"Opération de prescription: {operationType} - Prescription {prescriptionId}, Patient {patientId}",
                    hospitalCenterId: hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'opération de prescription {PrescriptionId}", prescriptionId);
                throw;
            }
        }

        public async Task LogExaminationOperationAsync(
            int examinationId,
            int patientId,
            string operationType,
            int? requestedBy = null,
            int? performedBy = null,
            int? hospitalCenterId = null)
        {
            try
            {
                var examinationData = new
                {
                    ExaminationId = examinationId,
                    PatientId = patientId,
                    OperationType = operationType,
                    RequestedBy = requestedBy,
                    PerformedBy = performedBy,
                    HospitalCenterId = hospitalCenterId
                };

                await LogActionAsync(
                    requestedBy ?? performedBy,
                    "ExaminationOperation",
                    "Examination",
                    examinationId,
                    null,
                    examinationData,
                    $"Opération d'examen: {operationType} - Examen {examinationId}, Patient {patientId}",
                    hospitalCenterId: hospitalCenterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'opération d'examen {ExaminationId}", examinationId);
                throw;
            }
        }

        // ===== AUDITS SYSTÈME =====

        public async Task LogSystemErrorAsync(
            Exception exception,
            string source,
            int? userId = null,
            int? hospitalCenterId = null,
            object? contextData = null)
        {
            try
            {
                var errorData = new
                {
                    ErrorType = exception.GetType().Name,
                    Message = exception.Message,
                    Source = source,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message,
                    ContextData = contextData
                };

                await LogActionAsync(
                    userId,
                    "SystemError",
                    "System",
                    null,
                    null,
                    errorData,
                    $"Erreur système dans {source}: {exception.Message}",
                    hospitalCenterId: hospitalCenterId);

                // Aussi enregistrer dans les logs d'erreur système
                await _appLogger.LogSystemErrorAsync(exception, source, userId, hospitalCenterId, contextData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit d'erreur système dans {Source}", source);
                throw;
            }
        }

        public async Task LogSystemConfigurationChangeAsync(
            string configurationKey,
            object? oldValue,
            object? newValue,
            int changedBy,
            string? ipAddress = null)
        {
            try
            {
                await LogActionAsync(
                    changedBy,
                    "SystemConfigurationChange",
                    "SystemConfiguration",
                    null,
                    new { Key = configurationKey, Value = oldValue },
                    new { Key = configurationKey, Value = newValue },
                    $"Configuration système modifiée: {configurationKey}",
                    ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'audit de changement de configuration {ConfigKey}", configurationKey);
                throw;
            }
        }

        // ===== CONSULTATION ET RECHERCHE =====

        public async Task<List<AuditLog>> GetUserAuditTrailAsync(
            int userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? actionType = null)
        {
            try
            {
                var query = _context.AuditLogs
                    .Where(al => al.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(al => al.ActionDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(al => al.ActionDate <= toDate.Value);

                if (!string.IsNullOrEmpty(actionType))
                    query = query.Where(al => al.ActionType == actionType);

                return await query
                    .OrderByDescending(al => al.ActionDate)
                    .Take(1000) // Limiter pour éviter les requêtes trop lourdes
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'audit trail pour utilisateur {UserId}", userId);
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetEntityAuditTrailAsync(
            string entityType,
            int entityId,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                var query = _context.AuditLogs
                    .Where(al => al.EntityType == entityType && al.EntityId == entityId);

                if (fromDate.HasValue)
                    query = query.Where(al => al.ActionDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(al => al.ActionDate <= toDate.Value);

                return await query
                    .OrderByDescending(al => al.ActionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'audit trail pour {EntityType} {EntityId}",
                    entityType, entityId);
                return new List<AuditLog>();
            }
        }

        public async Task<List<AuditLog>> GetCenterAuditTrailAsync(
            int hospitalCenterId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? actionType = null)
        {
            try
            {
                var query = _context.AuditLogs
                    .Where(al => al.HospitalCenterId == hospitalCenterId);

                if (fromDate.HasValue)
                    query = query.Where(al => al.ActionDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(al => al.ActionDate <= toDate.Value);

                if (!string.IsNullOrEmpty(actionType))
                    query = query.Where(al => al.ActionType == actionType);

                return await query
                    .OrderByDescending(al => al.ActionDate)
                    .Take(1000)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'audit trail pour centre {CenterId}", hospitalCenterId);
                return new List<AuditLog>();
            }
        }

        public async Task<(List<AuditLog> AuditLogs, int TotalCount)> SearchAuditLogsAsync(
            string? searchTerm = null,
            int? userId = null,
            string? actionType = null,
            string? entityType = null,
            int? entityId = null,
            int? hospitalCenterId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageIndex = 1,
            int pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                // Filtres
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(al => al.Description!.Contains(searchTerm) ||
                                             al.ActionType.Contains(searchTerm) ||
                                             al.EntityType.Contains(searchTerm));
                }

                if (userId.HasValue)
                    query = query.Where(al => al.UserId == userId);

                if (!string.IsNullOrEmpty(actionType))
                    query = query.Where(al => al.ActionType == actionType);

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(al => al.EntityType == entityType);

                if (entityId.HasValue)
                    query = query.Where(al => al.EntityId == entityId);

                if (hospitalCenterId.HasValue)
                    query = query.Where(al => al.HospitalCenterId == hospitalCenterId);

                if (fromDate.HasValue)
                    query = query.Where(al => al.ActionDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(al => al.ActionDate <= toDate.Value);

                var totalCount = await query.CountAsync();

                var auditLogs = await query
                    .OrderByDescending(al => al.ActionDate)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (auditLogs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche d'audit logs");
                return (new List<AuditLog>(), 0);
            }
        }

        // ===== RAPPORTS ET ANALYSES =====

        public async Task<UserActivityReport> GenerateUserActivityReportAsync(
            int userId,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var auditLogs = await GetUserAuditTrailAsync(userId, fromDate, toDate);
                var user = await _context.Users.FindAsync(userId);

                var report = new UserActivityReport
                {
                    UserId = userId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : $"Utilisateur {userId}",
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalActions = auditLogs.Count,
                    LoginCount = auditLogs.Count(al => al.ActionType == "Login"),
                    SalesCreated = auditLogs.Count(al => al.ActionType == "SaleOperation" && al.Description!.Contains("Created")),
                    CareServicesProvided = auditLogs.Count(al => al.ActionType == "CareEpisodeOperation"),
                    PrescriptionsIssued = auditLogs.Count(al => al.ActionType == "PrescriptionOperation" && al.Description!.Contains("Created")),
                    MostFrequentActions = auditLogs
                        .GroupBy(al => al.ActionType)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToList(),
                    CentersVisited = auditLogs
                        .Where(al => al.HospitalCenterId.HasValue)
                        .Select(al => al.HospitalCenterId!.Value)
                        .Distinct()
                        .ToList()
                };

                // Calculer la répartition par heure
                report.ActionsByHour = auditLogs
                    .GroupBy(al => al.ActionDate.Hour)
                    .ToDictionary(g => g.Key.ToString("D2") + "h", g => g.Count());

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du rapport d'activité pour utilisateur {UserId}", userId);
                throw;
            }
        }

        public async Task<CenterActivityReport> GenerateCenterActivityReportAsync(
            int hospitalCenterId,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var auditLogs = await GetCenterAuditTrailAsync(hospitalCenterId, fromDate, toDate);
                var center = await _context.HospitalCenters.FindAsync(hospitalCenterId);

                // Calculer les métriques spécifiques
                var salesLogs = auditLogs.Where(al => al.ActionType == "SaleOperation").ToList();
                var careEpisodeLogs = auditLogs.Where(al => al.ActionType == "CareEpisodeOperation").ToList();
                var examinationLogs = auditLogs.Where(al => al.ActionType == "ExaminationOperation").ToList();

                var report = new CenterActivityReport
                {
                    HospitalCenterId = hospitalCenterId,
                    CenterName = center?.Name ?? $"Centre {hospitalCenterId}",
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalActions = auditLogs.Count,
                    UniqueUsersActive = auditLogs
                        .Where(al => al.UserId.HasValue)
                        .Select(al => al.UserId!.Value)
                        .Distinct()
                        .Count(),
                    TotalSales = salesLogs.Count,
                    StockMovements = auditLogs.Count(al => al.ActionType == "StockOperation"),
                    CareEpisodesCreated = careEpisodeLogs.Count(al => al.Description!.Contains("Created")),
                    ExaminationsPerformed = examinationLogs.Count,
                    ActionsByType = auditLogs
                        .GroupBy(al => al.ActionType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TopActiveUsers = auditLogs
                        .Where(al => al.UserId.HasValue)
                        .GroupBy(al => al.UserId!.Value)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => (g.Key, "Utilisateur " + g.Key, g.Count()))
                        .ToList()
                };

                // Calculer le revenu total à partir des logs de vente (approximation)
                report.TotalRevenue = salesLogs.Count * 1000; // Estimation - à améliorer avec de vraies données

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du rapport d'activité pour centre {CenterId}", hospitalCenterId);
                throw;
            }
        }

        public async Task<SecurityAuditReport> GenerateSecurityReportAsync(
            DateTime fromDate,
            DateTime toDate,
            int? hospitalCenterId = null)
        {
            try
            {
                var query = _context.AuditLogs
                    .Where(al => al.ActionDate >= fromDate && al.ActionDate <= toDate);

                if (hospitalCenterId.HasValue)
                    query = query.Where(al => al.HospitalCenterId == hospitalCenterId);

                var securityLogs = await query
                    .Where(al => al.ActionType.Contains("Login") ||
                                al.ActionType.Contains("Password") ||
                                al.ActionType.Contains("Account") ||
                                al.ActionType == "UnauthorizedAccess")
                    .ToListAsync();

                var report = new SecurityAuditReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalLoginAttempts = securityLogs.Count(al => al.ActionType == "Login"),
                    FailedLoginAttempts = securityLogs.Count(al => al.ActionType == "Login" && al.Description!.Contains("Échec")),
                    PasswordResets = securityLogs.Count(al => al.ActionType == "PasswordReset"),
                    AccountLockouts = securityLogs.Count(al => al.ActionType == "AccountLocked"),
                    UnauthorizedAccessAttempts = securityLogs.Count(al => al.ActionType == "UnauthorizedAccess"),
                    TopFailedIPAddresses = securityLogs
                        .Where(al => al.ActionType == "Login" && al.Description!.Contains("Échec") && !string.IsNullOrEmpty(al.IpAddress))
                        .GroupBy(al => al.IpAddress!)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => (g.Key, g.Count()))
                        .ToList(),
                    UsersWithFailedLogins = securityLogs
                        .Where(al => al.ActionType == "Login" && al.Description!.Contains("Échec") && al.UserId.HasValue)
                        .GroupBy(al => al.UserId!.Value)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .Select(g => (g.Key, "Utilisateur " + g.Key, g.Count()))
                        .ToList()
                };

                report.SuspiciousActivities = report.UnauthorizedAccessAttempts +
                                             (report.FailedLoginAttempts > 100 ? 1 : 0);

                // Recommandations de sécurité
                if (report.FailedLoginAttempts > 50)
                    report.SecurityRecommendations.Add("Considérer l'implémentation d'un système de limitation des tentatives");

                if (report.PasswordResets > 20)
                    report.SecurityRecommendations.Add("Sensibiliser les utilisateurs à la sécurité des mots de passe");

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du rapport de sécurité");
                throw;
            }
        }

        public async Task<List<SuspiciousActivity>> DetectSuspiciousActivitiesAsync(
            DateTime fromDate,
            DateTime toDate,
            int? hospitalCenterId = null)
        {
            try
            {
                var suspiciousActivities = new List<SuspiciousActivity>();

                // Détecter les connexions multiples depuis différentes IP
                var loginsByUser = await _context.AuditLogs
                    .Where(al => al.ActionType == "Login" &&
                                al.ActionDate >= fromDate &&
                                al.ActionDate <= toDate &&
                                al.UserId.HasValue &&
                                (!hospitalCenterId.HasValue || al.HospitalCenterId == hospitalCenterId))
                    .GroupBy(al => al.UserId!.Value)
                    .Select(g => new { UserId = g.Key, IPAddresses = g.Select(al => al.IpAddress).Distinct().Count() })
                    .Where(x => x.IPAddresses > 3)
                    .ToListAsync();

                foreach (var userLogin in loginsByUser)
                {
                    suspiciousActivities.Add(new SuspiciousActivity
                    {
                        UserId = userLogin.UserId,
                        UserName = $"Utilisateur {userLogin.UserId}",
                        ActivityType = "MultipleIPLogin",
                        Description = $"Connexions depuis {userLogin.IPAddresses} adresses IP différentes",
                        RiskLevel = "Medium",
                        DetectedAt = TimeZoneHelper.GetCameroonTime(),
                        Evidence = new Dictionary<string, object> { ["IPCount"] = userLogin.IPAddresses }
                    });
                }

                // Détecter les activités anormales (beaucoup d'actions en peu de temps)
                var highActivityUsers = await _context.AuditLogs
                    .Where(al => al.ActionDate >= fromDate &&
                                al.ActionDate <= toDate &&
                                al.UserId.HasValue &&
                                (!hospitalCenterId.HasValue || al.HospitalCenterId == hospitalCenterId))
                    .GroupBy(al => al.UserId!.Value)
                    .Select(g => new { UserId = g.Key, ActionCount = g.Count() })
                    .Where(x => x.ActionCount > 1000) // Plus de 1000 actions sur la période
                    .ToListAsync();

                foreach (var highActivity in highActivityUsers)
                {
                    suspiciousActivities.Add(new SuspiciousActivity
                    {
                        UserId = highActivity.UserId,
                        UserName = $"Utilisateur {highActivity.UserId}",
                        ActivityType = "HighVolumeActivity",
                        Description = $"Volume d'activité anormalement élevé: {highActivity.ActionCount} actions",
                        RiskLevel = "High",
                        DetectedAt = TimeZoneHelper.GetCameroonTime(),
                        Evidence = new Dictionary<string, object> { ["ActionCount"] = highActivity.ActionCount }
                    });
                }

                return suspiciousActivities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la détection d'activités suspectes");
                return new List<SuspiciousActivity>();
            }
        }

        // ===== MAINTENANCE ET OPTIMISATION =====

        public async Task<int> ArchiveOldAuditLogsAsync(DateTime beforeDate)
        {
            try
            {
                // Dans cette implémentation, on va simplement compter les logs à archiver
                // Dans un vrai système, on déplacerait vers une table d'archive
                var logsToArchive = await _context.AuditLogs
                    .Where(al => al.ActionDate < beforeDate)
                    .CountAsync();

                await _appLogger.LogInfoAsync("Audit", "ArchiveOldLogs",
                    $"{logsToArchive} logs d'audit identifiés pour archivage avant {beforeDate:yyyy-MM-dd}",
                    details: new { BeforeDate = beforeDate, LogCount = logsToArchive });

                return logsToArchive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'archivage des anciens logs d'audit");
                return 0;
            }
        }

        public async Task<AuditIntegrityReport> ValidateAuditIntegrityAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var auditLogs = await _context.AuditLogs
                    .Where(al => al.ActionDate >= fromDate && al.ActionDate <= toDate)
                    .ToListAsync();

                var report = new AuditIntegrityReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalAuditLogs = auditLogs.Count,
                    VerifiedLogs = auditLogs.Count, // Tous les logs sont considérés comme vérifiés
                    SuspiciousLogs = 0,
                    IntegrityValid = true,
                    IntegrityIssues = new List<string>(),
                    SuspiciousAuditIds = new List<int>()
                };

                // Vérifications d'intégrité de base
                var logsWithoutUser = auditLogs.Where(al => !al.UserId.HasValue && al.ActionType != "SystemError").ToList();
                if (logsWithoutUser.Any())
                {
                    report.SuspiciousLogs += logsWithoutUser.Count;
                    report.IntegrityIssues.Add($"{logsWithoutUser.Count} logs sans utilisateur associé");
                    report.SuspiciousAuditIds.AddRange(logsWithoutUser.Select(al => al.Id));
                }

                // Vérifier les séquences de dates
                var outOfOrderLogs = auditLogs
                    .OrderBy(al => al.Id)
                    .Where((al, index) => index > 0 && al.ActionDate < auditLogs.OrderBy(x => x.Id).ElementAt(index - 1).ActionDate)
                    .ToList();

                if (outOfOrderLogs.Any())
                {
                    report.SuspiciousLogs += outOfOrderLogs.Count;
                    report.IntegrityIssues.Add($"{outOfOrderLogs.Count} logs avec dates hors séquence");
                    report.SuspiciousAuditIds.AddRange(outOfOrderLogs.Select(al => al.Id));
                }

                report.IntegrityValid = !report.IntegrityIssues.Any();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de l'intégrité des audits");

                return new AuditIntegrityReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    IntegrityValid = false,
                    IntegrityIssues = new List<string> { "Erreur lors de la validation" }
                };
            }
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Compare deux objets et retourne la liste des propriétés modifiées
        /// </summary>
        private static List<string> GetChangedProperties(object oldObj, object newObj)
        {
            var changes = new List<string>();
            var properties = oldObj.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var oldValue = prop.GetValue(oldObj);
                var newValue = prop.GetValue(newObj);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add($"{prop.Name}: {oldValue} → {newValue}");
                }
            }

            return changes;
        }
    }
}