using HManagSys.Models.ViewModels.Reports;
using System.Threading.Tasks;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la génération et gestion des rapports avancés
    /// Comme un analyste qui transforme les données brutes en informations décisionnelles
    /// </summary>
    public interface IReportService
    {
        // ===== RAPPORTS UTILISATEURS ET CENTRES =====

        /// <summary>
        /// Génère un rapport des utilisateurs par centre
        /// </summary>
        Task<UserCenterReportViewModel> GenerateUserCenterReportAsync(UserCenterReportFilters filters);

        /// <summary>
        /// Génère un rapport des sessions actives
        /// </summary>
        Task<ActiveSessionsReportViewModel> GenerateActiveSessionsReportAsync(ActiveSessionsReportFilters filters);

        // ===== RAPPORTS STOCK ET INVENTAIRE =====

        /// <summary>
        /// Génère un rapport sur l'état des stocks
        /// </summary>
        Task<StockStatusReportViewModel> GenerateStockStatusReportAsync(StockStatusReportFilters filters);

        /// <summary>
        /// Génère un rapport des mouvements de stock
        /// </summary>
        Task<StockMovementReportViewModel> GenerateStockMovementReportAsync(StockMovementReportFilters filters);

        /// <summary>
        /// Génère un rapport de valorisation du stock
        /// </summary>
        Task<Models.ViewModels.Reports.StockValuationReportViewModel> GenerateStockValuationReportAsync(StockValuationReportFilters filters);

        // ===== RAPPORTS FINANCIERS =====

        /// <summary>
        /// Génère un rapport d'activité financière
        /// </summary>
        Task<FinancialActivityReportViewModel> GenerateFinancialActivityReportAsync(FinancialActivityReportFilters filters);

        /// <summary>
        /// Génère un rapport des paiements
        /// </summary>
        Task<PaymentReportViewModel> GeneratePaymentReportAsync(PaymentReportFilters filters);

        /// <summary>
        /// Génère un rapport des ventes
        /// </summary>
        Task<SalesReportViewModel> GenerateSalesReportAsync(SalesReportFilters filters);

        // ===== RAPPORTS PERFORMANCES =====

        /// <summary>
        /// Génère un rapport de performance des soignants
        /// </summary>
        Task<CaregiverPerformanceReportViewModel> GenerateCaregiverPerformanceReportAsync(CaregiverPerformanceReportFilters filters);

        /// <summary>
        /// Génère un rapport d'activité médicale
        /// </summary>
        Task<MedicalActivityReportViewModel> GenerateMedicalActivityReportAsync(MedicalActivityReportFilters filters);

        // ===== EXPORTS =====

        /// <summary>
        /// Exporte un rapport au format Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync(ExportParameters parameters);

        /// <summary>
        /// Exporte un rapport au format PDF
        /// </summary>
        Task<byte[]> ExportToPdfAsync(ExportParameters parameters);

        // ===== GESTION DES RAPPORTS PLANIFIÉS =====

        /// <summary>
        /// Planifie un rapport récurrent
        /// </summary>
        Task<bool> ScheduleRecurringReportAsync(RecurringReportSchedule schedule);

        /// <summary>
        /// Récupère les rapports planifiés
        /// </summary>
        Task<List<RecurringReportViewModel>> GetScheduledReportsAsync(int? userId = null);

        /// <summary>
        /// Supprime un rapport planifié
        /// </summary>
        Task<bool> DeleteScheduledReportAsync(int scheduleId);

        // ===== RÉCUPÉRATION DE RAPPORTS PRÉCALCULÉS =====

        /// <summary>
        /// Vérifie si un rapport précalculé existe
        /// </summary>
        Task<bool> CachedReportExistsAsync(string reportType, string reportKey);

        /// <summary>
        /// Récupère un rapport précalculé depuis le cache
        /// </summary>
        Task<object> GetCachedReportAsync(string reportType, string reportKey);

        /// <summary>
        /// Force le calcul d'un rapport (mise à jour des tables de rapport)
        /// </summary>
        Task<bool> RefreshReportDataAsync(string reportType, DateTime? asOfDate = null);
    }
}