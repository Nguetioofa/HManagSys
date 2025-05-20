using HManagSys.Models;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Reports
{
    /// <summary>
    /// Base commune pour tous les filtres de rapport
    /// </summary>
    public abstract class BaseReportFilters
    {
        [Display(Name = "Centre hospitalier")]
        public int? HospitalCenterId { get; set; }

        [Display(Name = "Date de début")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Date de fin")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Format d'export")]
        public string? ExportFormat { get; set; } // "Excel", "PDF", null=affichage web

        // Valeurs par défaut pour les dates
        public BaseReportFilters()
        {
            // Par défaut, affiche le mois en cours
            FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ToDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Base commune pour tous les modèles de rapport
    /// </summary>
    public abstract class BaseReportViewModel
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string ReportDescription { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public List<string> ColumnHeaders { get; set; } = new();
        public int TotalCount { get; set; }
        public object Filters { get; set; }

        // Formatage pour l'affichage
        public string FormattedGeneratedAt => GeneratedAt.ToString("dd/MM/yyyy HH:mm");
    }

    #region Rapports Utilisateurs et Centres

    /// <summary>
    /// Filtres pour le rapport utilisateurs-centres
    /// </summary>
    public class UserCenterReportFilters : BaseReportFilters
    {
        [Display(Name = "Rôle")]
        public string? RoleType { get; set; }

        [Display(Name = "Statut d'affectation")]
        public bool? IsAssignmentActive { get; set; }

        [Display(Name = "Statut utilisateur")]
        public bool? IsUserActive { get; set; }

        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport utilisateurs-centres
    /// </summary>
    public class UserCenterReportViewModel : BaseReportViewModel
    {
        public List<UserCenterReportItem> Items { get; set; } = new();

        // Statistiques
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalAssignments { get; set; }
        public int ActiveAssignments { get; set; }

        // Distribution par rôle
        public Dictionary<string, int> UsersByRole { get; set; } = new();

        // Distribution par centre
        public Dictionary<string, int> UsersByCenter { get; set; } = new();
    }

    /// <summary>
    /// Élément individuel du rapport utilisateurs-centres
    /// </summary>
    public class UserCenterReportItem
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool UserIsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int? AssignmentId { get; set; }
        public string RoleType { get; set; } = string.Empty;
        public bool AssignmentIsActive { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public DateTime? AssignmentStartDate { get; set; }
        public DateTime? AssignmentEndDate { get; set; }

        // Propriétés calculées pour l'affichage
        public string FullName => $"{FirstName} {LastName}";
        public string UserStatusBadge => UserIsActive ? "badge bg-success" : "badge bg-danger";
        public string UserStatusText => UserIsActive ? "Actif" : "Inactif";
        public string AssignmentStatusBadge => AssignmentIsActive ? "badge bg-success" : "badge bg-danger";
        public string AssignmentStatusText => AssignmentIsActive ? "Active" : "Inactive";
        public string FormattedLastLogin => LastLoginDate?.ToString("dd/MM/yyyy HH:mm") ?? "Jamais";
        public string FormattedStartDate => AssignmentStartDate?.ToString("dd/MM/yyyy") ?? "-";
        public string FormattedEndDate => AssignmentEndDate?.ToString("dd/MM/yyyy") ?? "-";
    }

    /// <summary>
    /// Filtres pour le rapport des sessions actives
    /// </summary>
    public class ActiveSessionsReportFilters : BaseReportFilters
    {
        [Display(Name = "Durée minimale (heures)")]
        public int? MinHoursConnected { get; set; }

        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport des sessions actives
    /// </summary>
    public class ActiveSessionsReportViewModel : BaseReportViewModel
    {
        public List<ActiveSessionReportItem> Items { get; set; } = new();

        // Statistiques
        public int TotalActiveSessions { get; set; }
        public int AverageSessionDuration { get; set; } // en minutes
        public Dictionary<string, int> SessionsByCenter { get; set; } = new();
    }

    /// <summary>
    /// Élément individuel du rapport des sessions actives
    /// </summary>
    public class ActiveSessionReportItem
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentHospitalCenter { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int HoursConnected { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedLoginTime => LoginTime.ToString("dd/MM/yyyy HH:mm");
        public string ConnectionDuration => $"{HoursConnected}h {((DateTime.Now - LoginTime).Minutes)} min";
    }

    #endregion

    #region Rapports Stock et Inventaire

    /// <summary>
    /// Filtres pour le rapport d'état des stocks
    /// </summary>
    public class StockStatusReportFilters : BaseReportFilters
    {
        [Display(Name = "Catégorie de produit")]
        public int? ProductCategoryId { get; set; }

        [Display(Name = "Statut de stock")]
        public string? StockStatus { get; set; } // "Normal", "Low", "Critical", "Overstock"

        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport d'état des stocks
    /// </summary>
    public class StockStatusReportViewModel : BaseReportViewModel
    {
        public List<StockStatusReportItem> Items { get; set; } = new();

        // Statistiques
        public int TotalProducts { get; set; }
        public int ProductsWithCriticalStock { get; set; }
        public int ProductsWithLowStock { get; set; }
        public int ProductsWithNormalStock { get; set; }
        public int ProductsWithOverstock { get; set; }
        public decimal TotalStockValue { get; set; }

        // Distribution par catégorie
        public Dictionary<string, int> ProductsByCategory { get; set; } = new();
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
    }

    /// <summary>
    /// Élément individuel du rapport d'état des stocks
    /// </summary>
    public class StockStatusReportItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal? MinimumThreshold { get; set; }
        public decimal? MaximumThreshold { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public DateTime? LastMovementDate { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedCurrentQuantity => $"{CurrentQuantity:N2}";
        public string FormattedMinThreshold => MinimumThreshold.HasValue ? $"{MinimumThreshold:N2}" : "-";
        public string FormattedMaxThreshold => MaximumThreshold.HasValue ? $"{MaximumThreshold:N2}" : "-";
        public string FormattedLastMovement => LastMovementDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";
        public string FormattedTotalValue => $"{TotalValue:N0} FCFA";
        public string StockStatusBadge => GetStockStatusBadge(StockStatus);

        private string GetStockStatusBadge(string status) => status switch
        {
            "Critical" => "badge bg-danger",
            "Low" => "badge bg-warning text-dark",
            "Normal" => "badge bg-success",
            "Overstock" => "badge bg-info text-dark",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// Filtres pour le rapport des mouvements de stock
    /// </summary>
    public class StockMovementReportFilters : BaseReportFilters
    {
        [Display(Name = "Produit")]
        public int? ProductId { get; set; }

        [Display(Name = "Type de mouvement")]
        public string? MovementType { get; set; }

        [Display(Name = "Type de référence")]
        public string? ReferenceType { get; set; }

        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport des mouvements de stock
    /// </summary>
    public class StockMovementReportViewModel : BaseReportViewModel
    {
        public List<StockMovementReportItem> Items { get; set; } = new();

        // Statistiques
        public Dictionary<string, int> MovementsByType { get; set; } = new();
        public Dictionary<string, decimal> QuantityByType { get; set; } = new();

        public int TotalMovements { get; set; }
        public decimal TotalInQuantity { get; set; }
        public decimal TotalOutQuantity { get; set; }
        public decimal NetChange { get; set; }
    }

    /// <summary>
    /// Élément individuel du rapport des mouvements de stock
    /// </summary>
    public class StockMovementReportItem
    {
        public int MovementId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string HospitalCenterName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public DateTime MovementDate { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        // Propriétés calculées pour l'affichage
        public string FormattedMovementDate => MovementDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedQuantity => $"{Quantity:N2}";
        public string QuantityClass => IsPositiveMovement(MovementType) ? "text-success" : "text-danger";
        public string QuantityPrefix => IsPositiveMovement(MovementType) ? "+" : "-";
        public string MovementTypeBadge => GetMovementTypeBadge(MovementType);

        private bool IsPositiveMovement(string type) => type switch
        {
            "Initial" => true,
            "Entry" => true,
            "Transfer" when ReferenceType == "Incoming" => true,
            _ => false
        };

        private string GetMovementTypeBadge(string type) => type switch
        {
            "Initial" => "badge bg-primary",
            "Entry" => "badge bg-success",
            "Sale" => "badge bg-danger",
            "Transfer" => "badge bg-info text-dark",
            "Adjustment" => "badge bg-warning text-dark",
            "Care" => "badge bg-secondary",
            _ => "badge bg-light text-dark"
        };
    }

    /// <summary>
    /// Filtres pour le rapport de valorisation du stock
    /// </summary>
    public class StockValuationReportFilters : BaseReportFilters
    {
        [Display(Name = "Catégorie de produit")]
        public int? ProductCategoryId { get; set; }

        [Display(Name = "Valoriser au prix de")]
        public string ValuationType { get; set; } = "SellingPrice"; // "SellingPrice", "LastPurchasePrice", "AveragePrice"
    }

    /// <summary>
    /// Modèle pour le rapport de valorisation du stock
    /// </summary>
    public class StockValuationReportViewModel : BaseReportViewModel
    {
        public List<StockValuationReportItem> Items { get; set; } = new();

        // Sommaire
        public decimal TotalValue { get; set; }
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();

        // Propriétés calculées
        public string FormattedTotalValue => $"{TotalValue:N0} FCFA";
    }

    /// <summary>
    /// Élément individuel du rapport de valorisation du stock
    /// </summary>
    public class StockValuationReportItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string HospitalCenterName { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedQuantity => $"{CurrentQuantity:N2} {UnitOfMeasure}";
        public string FormattedUnitPrice => $"{UnitPrice:N0} FCFA";
        public string FormattedTotalValue => $"{TotalValue:N0} FCFA";
    }

    #endregion

    #region Rapports Financiers

    /// <summary>
    /// Filtres pour le rapport d'activité financière
    /// </summary>
    public class FinancialActivityReportFilters : BaseReportFilters
    {
        [Display(Name = "Grouper par")]
        public string GroupBy { get; set; } = "Day"; // "Day", "Week", "Month"
    }

    /// <summary>
    /// Modèle pour le rapport d'activité financière
    /// </summary>
    public class FinancialActivityReportViewModel : BaseReportViewModel
    {
        public List<FinancialActivityReportItem> Items { get; set; } = new();

        // Sommaire
        public decimal TotalSales { get; set; }
        public decimal TotalCareRevenue { get; set; }
        public decimal TotalExaminationRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCashPayments { get; set; }
        public decimal TotalMobilePayments { get; set; }
        public int TotalTransactionCount { get; set; }
        public int TotalPatientCount { get; set; }

        // Propriétés calculées
        public string FormattedTotalRevenue => $"{TotalRevenue:N0} FCFA";
        public string FormattedTotalSales => $"{TotalSales:N0} FCFA";
        public string FormattedTotalCareRevenue => $"{TotalCareRevenue:N0} FCFA";
        public string FormattedTotalExaminationRevenue => $"{TotalExaminationRevenue:N0} FCFA";
    }

    /// <summary>
    /// Élément individuel du rapport d'activité financière
    /// </summary>
    public class FinancialActivityReportItem
    {
        public DateTime ReportDate { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalCareRevenue { get; set; }
        public decimal TotalExaminationRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCashPayments { get; set; }
        public decimal TotalMobilePayments { get; set; }
        public int TransactionCount { get; set; }
        public int PatientCount { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedReportDate => ReportDate.ToString("dd/MM/yyyy");
        public string FormattedTotalRevenue => $"{TotalRevenue:N0} FCFA";
        public string FormattedTotalSales => $"{TotalSales:N0} FCFA";
        public string FormattedTotalCareRevenue => $"{TotalCareRevenue:N0} FCFA";
        public string FormattedTotalExaminationRevenue => $"{TotalExaminationRevenue:N0} FCFA";
    }

    /// <summary>
    /// Filtres pour le rapport des paiements
    /// </summary>
    public class PaymentReportFilters : BaseReportFilters
    {
        [Display(Name = "Méthode de paiement")]
        public int? PaymentMethodId { get; set; }

        [Display(Name = "Type de référence")]
        public string? ReferenceType { get; set; }

        [Display(Name = "Patient")]
        public int? PatientId { get; set; }

        [Display(Name = "Reçu par")]
        public int? ReceivedBy { get; set; }

        [Display(Name = "Montant minimum")]
        public decimal? MinAmount { get; set; }

        [Display(Name = "Montant maximum")]
        public decimal? MaxAmount { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport des paiements
    /// </summary>
    public class PaymentReportViewModel : BaseReportViewModel
    {
        public List<PaymentReportItem> Items { get; set; } = new();

        // Sommaire
        public decimal TotalPayments { get; set; }
        public Dictionary<string, decimal> PaymentsByMethod { get; set; } = new();
        public Dictionary<string, decimal> PaymentsByReferenceType { get; set; } = new();

        // Propriétés calculées
        public string FormattedTotalPayments => $"{TotalPayments:N0} FCFA";
    }

    /// <summary>
    /// Élément individuel du rapport des paiements
    /// </summary>
    public class PaymentReportItem
    {
        public int PaymentId { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string HospitalCenterName { get; set; } = string.Empty;
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceivedByName { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedPaymentDate => PaymentDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedAmount => $"{Amount:N0} FCFA";
        public string ReferenceTypeBadge => GetReferenceTypeBadge(ReferenceType);

        private string GetReferenceTypeBadge(string type) => type switch
        {
            "Sale" => "badge bg-success",
            "CareEpisode" => "badge bg-primary",
            "Examination" => "badge bg-info text-dark",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// Filtres pour le rapport des ventes
    /// </summary>
    public class SalesReportFilters : BaseReportFilters
    {
        [Display(Name = "Statut de paiement")]
        public string? PaymentStatus { get; set; }

        [Display(Name = "Patient")]
        public int? PatientId { get; set; }

        [Display(Name = "Vendu par")]
        public int? SoldBy { get; set; }

        [Display(Name = "Produit")]
        public int? ProductId { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport des ventes
    /// </summary>
    public class SalesReportViewModel : BaseReportViewModel
    {
        public List<SaleReportItem> Items { get; set; } = new();

        // Sommaire
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalFinalAmount { get; set; }
        public int TotalProductsSold { get; set; }
        public Dictionary<string, decimal> SalesByPaymentStatus { get; set; } = new();
        public Dictionary<string, List<TopSellingProductItem>> TopSellingProducts { get; set; } = new();

        // Propriétés calculées
        public string FormattedTotalSalesAmount => $"{TotalSalesAmount:N0} FCFA";
        public string FormattedTotalDiscountAmount => $"{TotalDiscountAmount:N0} FCFA";
        public string FormattedTotalFinalAmount => $"{TotalFinalAmount:N0} FCFA";
    }

    /// <summary>
    /// Élément individuel du rapport des ventes
    /// </summary>
    public class SaleReportItem
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string HospitalCenterName { get; set; } = string.Empty;
        public string SoldByName { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<SaleItemDetail> Items { get; set; } = new();

        // Propriétés calculées pour l'affichage
        public string FormattedSaleDate => SaleDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedTotalAmount => $"{TotalAmount:N0} FCFA";
        public string FormattedDiscountAmount => $"{DiscountAmount:N0} FCFA";
        public string FormattedFinalAmount => $"{FinalAmount:N0} FCFA";
        public string PaymentStatusBadge => GetPaymentStatusBadge(PaymentStatus);
        public int ItemCount => Items.Count;

        private string GetPaymentStatusBadge(string status) => status switch
        {
            "Paid" => "badge bg-success",
            "Partial" => "badge bg-warning text-dark",
            "Pending" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    /// <summary>
    /// Détail d'un élément de vente
    /// </summary>
    public class SaleItemDetail
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedQuantity => $"{Quantity:N2}";
        public string FormattedUnitPrice => $"{UnitPrice:N0} FCFA";
        public string FormattedTotalPrice => $"{TotalPrice:N0} FCFA";
    }

    /// <summary>
    /// Élément pour les produits les plus vendus
    /// </summary>
    public class TopSellingProductItem
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedQuantitySold => $"{QuantitySold:N2}";
        public string FormattedRevenue => $"{Revenue:N0} FCFA";
    }

    #endregion

    #region Rapports Performances

    /// <summary>
    /// Filtres pour le rapport de performance des soignants
    /// </summary>
    public class CaregiverPerformanceReportFilters : BaseReportFilters
    {
        [Display(Name = "Soignant")]
        public int? UserId { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport de performance des soignants
    /// </summary>
    public class CaregiverPerformanceReportViewModel : BaseReportViewModel
    {
        public List<CaregiverPerformanceReportItem> Items { get; set; } = new();

        // Sommaire
        public int TotalCaregivers { get; set; }
        public int TotalPatientsServed { get; set; }
        public int TotalCareServicesProvided { get; set; }
        public int TotalExaminationsRequested { get; set; }
        public int TotalPrescriptionsIssued { get; set; }
        public int TotalSalesMade { get; set; }
        public decimal TotalRevenueGenerated { get; set; }

        // Propriétés calculées
        public decimal AverageRevenuePerCaregiver => TotalCaregivers > 0 ? TotalRevenueGenerated / TotalCaregivers : 0;
        public decimal AveragePatientsPerCaregiver => TotalCaregivers > 0 ? (decimal)TotalPatientsServed / TotalCaregivers : 0;

        public string FormattedTotalRevenueGenerated => $"{TotalRevenueGenerated:N0} FCFA";
        public string FormattedAverageRevenuePerCaregiver => $"{AverageRevenuePerCaregiver:N0} FCFA";
    }

    /// <summary>
    /// Élément individuel du rapport de performance des soignants
    /// </summary>
    public class CaregiverPerformanceReportItem
    {
        public int UserId { get; set; }
        public string CaregiverName { get; set; } = string.Empty;
        public string HospitalCenterName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public int PatientsServed { get; set; }
        public int CareServicesProvided { get; set; }
        public int ExaminationsRequested { get; set; }
        public int PrescriptionsIssued { get; set; }
        public int SalesMade { get; set; }
        public decimal TotalRevenueGenerated { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedReportDate => ReportDate.ToString("dd/MM/yyyy");
        public string FormattedTotalRevenueGenerated => $"{TotalRevenueGenerated:N0} FCFA";
        public decimal AverageRevenuePerPatient => PatientsServed > 0 ? TotalRevenueGenerated / PatientsServed : 0;
        public string FormattedAverageRevenuePerPatient => $"{AverageRevenuePerPatient:N0} FCFA";
    }

    /// <summary>
    /// Filtres pour le rapport d'activité médicale
    /// </summary>
    public class MedicalActivityReportFilters : BaseReportFilters
    {
        [Display(Name = "Type de soin")]
        public int? CareTypeId { get; set; }

        [Display(Name = "Type d'examen")]
        public int? ExaminationTypeId { get; set; }
    }

    /// <summary>
    /// Modèle pour le rapport d'activité médicale
    /// </summary>
    public class MedicalActivityReportViewModel : BaseReportViewModel
    {
        public List<MedicalActivityReportItem> Items { get; set; } = new();

        // Sommaire
        public int TotalEpisodes { get; set; }
        public int TotalActiveEpisodes { get; set; }
        public int TotalCompletedEpisodes { get; set; }
        public int TotalExaminations { get; set; }
        public int TotalPrescriptions { get; set; }
        public int TotalPatients { get; set; }
        public Dictionary<string, int> EpisodesByDiagnosis { get; set; } = new();

        // Tendances
        public List<TrendPoint> PatientTrend { get; set; } = new();
        public List<TrendPoint> RevenueTrend { get; set; } = new();
    }

    /// <summary>
    /// Élément individuel du rapport d'activité médicale
    /// </summary>
    public class MedicalActivityReportItem
    {
        public DateTime ActivityDate { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public int NewPatients { get; set; }
        public int CareEpisodes { get; set; }
        public int CareServices { get; set; }
        public int Examinations { get; set; }
        public int Prescriptions { get; set; }
        public decimal TotalRevenue { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedActivityDate => ActivityDate.ToString("dd/MM/yyyy");
        public string FormattedTotalRevenue => $"{TotalRevenue:N0} FCFA";
    }

    /// <summary>
    /// Point de tendance pour les graphiques
    /// </summary>
    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedDate => Date.ToString("dd/MM/yyyy");
        public string FormattedValue => Value.ToString("N0");
    }

    #endregion

    #region Export et Planification

    /// <summary>
    /// Paramètres pour l'export de rapports
    /// </summary>
    public class ExportParameters
    {
        public string ReportType { get; set; } = string.Empty;
        public string Format { get; set; } = "Excel"; // "Excel" ou "PDF"
        public object Filters { get; set; }
        public string? FileName { get; set; }
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeFooters { get; set; } = true;
        public string? CompanyLogo { get; set; }
        public string? CustomTitle { get; set; }
        public string? CustomFooter { get; set; }
    }

    /// <summary>
    /// Planification d'un rapport récurrent
    /// </summary>
    public class RecurringReportSchedule
    {
        public int? Id { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Frequency { get; set; } = "Weekly"; // "Daily", "Weekly", "Monthly"
        public DayOfWeek? DayOfWeek { get; set; }
        public int? DayOfMonth { get; set; }
        public TimeSpan ExecutionTime { get; set; } = new TimeSpan(8, 0, 0); // 8h00 par défaut
        public string Format { get; set; } = "Excel"; // "Excel" ou "PDF"
        public string? EmailRecipients { get; set; }
        public bool SaveToServer { get; set; } = true;
        public string? ServerPath { get; set; }
        public object Filters { get; set; }
        public int CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Modèle pour l'affichage des rapports récurrents
    /// </summary>
    public class RecurringReportViewModel
    {
        public int Id { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string ScheduleDescription { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string? EmailRecipients { get; set; }
        public bool SaveToServer { get; set; }
        public string? ServerPath { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastExecutionDate { get; set; }
        public DateTime? NextExecutionDate { get; set; }

        // Propriétés calculées pour l'affichage
        public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy");
        public string FormattedLastExecution => LastExecutionDate?.ToString("dd/MM/yyyy HH:mm") ?? "Jamais";
        public string FormattedNextExecution => NextExecutionDate?.ToString("dd/MM/yyyy HH:mm") ?? "Indéterminé";
        public string StatusBadge => IsActive ? "badge bg-success" : "badge bg-danger";
        public string StatusText => IsActive ? "Actif" : "Inactif";
    }

    #endregion
}