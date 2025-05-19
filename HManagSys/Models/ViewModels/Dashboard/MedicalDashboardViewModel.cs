using HManagSys.Models.ViewModels.Patients;
using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Dashboard
{
    /// <summary>
    /// Données générales du tableau de bord médical pour un centre
    /// </summary>
    public class MedicalDashboardViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;

        // Statistiques patients
        public int TotalPatientsCount { get; set; }
        public int ActivePatientsCount { get; set; }
        public int NewPatientsThisMonth { get; set; }

        // Statistiques des soins
        public int ActiveCareEpisodesCount { get; set; }
        public int CompletedCareEpisodesThisMonth { get; set; }
        public int InterruptedCareEpisodesThisMonth { get; set; }

        // Statistiques des examens
        public int PendingExaminationsCount { get; set; }
        public int ScheduledExaminationsCount { get; set; }
        public int CompletedExaminationsThisMonth { get; set; }

        // Statistiques des prescriptions
        public int PendingPrescriptionsCount { get; set; }
        public int DispensedPrescriptionsThisMonth { get; set; }

        // Statistiques financières liées aux soins
        public decimal TotalCareRevenue { get; set; }
        public decimal OutstandingCareBalance { get; set; }

        // Données pour les graphiques
        public List<ChartDataPoint> DailyPatientAdmissions { get; set; } = new();
        public List<ChartDataPoint> MonthlyCareEpisodes { get; set; } = new();
        public List<ChartDataPoint> ExaminationsByType { get; set; } = new();

        // Données liées à l'activité récente
        public List<RecentActivityItem> RecentActivities { get; set; } = new();
        public List<AlertItem> Alerts { get; set; } = new();
    }

    /// <summary>
    /// Données du tableau de bord pour un patient spécifique
    /// </summary>
    public class PatientDashboardViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public bool IsActive { get; set; }

        // Statistiques médicales
        public int TotalDiagnosesCount { get; set; }
        public int TotalCareEpisodesCount { get; set; }
        public int ActiveCareEpisodesCount { get; set; }
        public int TotalExaminationsCount { get; set; }
        public int PendingExaminationsCount { get; set; }
        public int TotalPrescriptionsCount { get; set; }
        public int PendingPrescriptionsCount { get; set; }

        // Données financières
        public decimal TotalAmountBilled { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal OutstandingBalance { get; set; }

        // Dernières activités
        public List<PatientActivityItem> RecentActivities { get; set; } = new();

        // Diagnostics actifs
        public List<DiagnosisViewModel> ActiveDiagnoses { get; set; } = new();

        // Épisodes de soins actifs
        public List<CareEpisodeViewModel> ActiveCareEpisodes { get; set; } = new();

        // Infos additionnelles pour graphiques
        public List<ChartDataPoint> CareProgressData { get; set; } = new();
        public List<ChartDataPoint> PaymentHistoryData { get; set; } = new();

        // Propriétés calculées
        public int? Age => DateOfBirth.HasValue
            ? CalculateAge(DateOfBirth.Value.ToDateTime(TimeOnly.MinValue))
            : null;

        public DateTime? LastVisitDate { get; set; }
        public bool HasOutstandingBalance => OutstandingBalance > 0;

        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>
    /// Données de progression du traitement pour un épisode de soins
    /// </summary>
    public class TreatmentProgressViewModel
    {
        public int CareEpisodeId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DiagnosisName { get; set; } = string.Empty;
        public DateTime EpisodeStartDate { get; set; }
        public DateTime? EpisodeEndDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Progression du traitement
        public int TotalServices { get; set; }
        public int CompletedServices { get; set; }
        public double ProgressPercentage => TotalServices > 0 ? (double)CompletedServices / TotalServices * 100 : 0;

        // Examens liés
        public int TotalExaminations { get; set; }
        public int CompletedExaminations { get; set; }
        public double ExaminationsProgressPercentage => TotalExaminations > 0 ? (double)CompletedExaminations / TotalExaminations * 100 : 0;

        // Prescriptions liées
        public int TotalPrescriptions { get; set; }
        public int DispensedPrescriptions { get; set; }
        public double PrescriptionsProgressPercentage => TotalPrescriptions > 0 ? (double)DispensedPrescriptions / TotalPrescriptions * 100 : 0;

        // Résumé des services de soins
        public List<CareServiceViewModel> RecentCareServices { get; set; } = new();

        // Données financières
        public decimal TotalCost { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }
        public double PaymentProgressPercentage => TotalCost > 0 ? (double)AmountPaid / (double)TotalCost * 100 : 0;

        // Données pour graphiques
        public List<ChartDataPoint> ServiceProgressByDate { get; set; } = new();

        // Prochaines étapes
        public List<TreatmentNextStep> NextSteps { get; set; } = new();
    }

    /// <summary>
    /// Résumé d'un épisode de soins
    /// </summary>
    public class CareEpisodeSummaryViewModel
    {
        public int CareEpisodeId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int DiagnosisId { get; set; }
        public string DiagnosisName { get; set; } = string.Empty;
        public string PrimaryCaregiverName { get; set; } = string.Empty;
        public DateTime EpisodeStartDate { get; set; }
        public DateTime? EpisodeEndDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Statistiques liées à l'épisode
        public int CareServicesCount { get; set; }
        public int ExaminationsCount { get; set; }
        public int PrescriptionsCount { get; set; }

        // Données financières
        public decimal TotalCost { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance { get; set; }

        // Services récents
        public List<CareServiceViewModel> RecentCareServices { get; set; } = new();

        // Examens récents
        public List<ExaminationViewModel> RecentExaminations { get; set; } = new();

        // Prescriptions récentes
        public List<PrescriptionViewModel> RecentPrescriptions { get; set; } = new();

        // Propriété calculée pour la durée
        public int DurationDays => EpisodeEndDate.HasValue
            ? (int)(EpisodeEndDate.Value - EpisodeStartDate).TotalDays
            : (int)(DateTime.Now - EpisodeStartDate).TotalDays;
    }

    /// <summary>
    /// Statistiques médicales récentes pour un centre
    /// </summary>
    public class RecentMedicalStatisticsViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public int Days { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; } = DateTime.Now;

        // Statistiques récentes
        public int NewPatientsCount { get; set; }
        public int NewCareEpisodesCount { get; set; }
        public int CompletedCareEpisodesCount { get; set; }
        public int NewExaminationsCount { get; set; }
        public int CompletedExaminationsCount { get; set; }
        public int NewPrescriptionsCount { get; set; }
        public int DispensedPrescriptionsCount { get; set; }

        // Revenus
        public decimal TotalRevenue { get; set; }
        public decimal CareRevenueAmount { get; set; }
        public decimal ExaminationRevenueAmount { get; set; }

        // Données pour graphiques
        public List<ChartDataPoint> DailyRevenueData { get; set; } = new();
        public List<ChartDataPoint> ServiceTypeDistribution { get; set; } = new();
        public List<ChartDataPoint> DiagnosisDistribution { get; set; } = new();
    }

    /// <summary>
    /// Point de données pour les graphiques
    /// </summary>
    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public string? Category { get; set; }
        public string? Color { get; set; }
        public DateTime? Date { get; set; }
    }

    /// <summary>
    /// Élément d'activité récente
    /// </summary>
    public class RecentActivityItem
    {
        public string ActivityType { get; set; } = string.Empty; // Patient, CareEpisode, Examination, Prescription, etc.
        public int ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Propriétés d'affichage
        public string ActivityTypeIcon => ActivityType switch
        {
            "Patient" => "fa-user",
            "CareEpisode" => "fa-procedures",
            "Examination" => "fa-microscope",
            "Prescription" => "fa-prescription",
            "Payment" => "fa-money-bill-wave",
            _ => "fa-calendar-alt"
        };

        public string StatusBadgeClass => Status switch
        {
            "Completed" => "bg-success",
            "Active" => "bg-primary",
            "Pending" => "bg-warning",
            "Scheduled" => "bg-info",
            "Canceled" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Élément d'activité pour un patient
    /// </summary>
    public class PatientActivityItem
    {
        public string ActivityType { get; set; } = string.Empty; // Diagnosis, CareEpisode, Examination, Prescription, Payment
        public int ReferenceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Amount { get; set; }

        // Propriétés d'affichage (identiques à RecentActivityItem)
        public string ActivityTypeIcon => ActivityType switch
        {
            "Diagnosis" => "fa-stethoscope",
            "CareEpisode" => "fa-procedures",
            "Examination" => "fa-microscope",
            "Prescription" => "fa-prescription",
            "Payment" => "fa-money-bill-wave",
            _ => "fa-calendar-alt"
        };

        public string StatusBadgeClass => Status switch
        {
            "Completed" => "bg-success",
            "Active" => "bg-primary",
            "Pending" => "bg-warning",
            "Scheduled" => "bg-info",
            "Canceled" => "bg-danger",
            _ => "bg-secondary"
        };
    }

    /// <summary>
    /// Élément d'alerte
    /// </summary>
    public class AlertItem
    {
        public string AlertType { get; set; } = string.Empty; // Warning, Info, Danger, Success
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public bool IsRead { get; set; }

        // Propriétés d'affichage
        public string AlertTypeIcon => AlertType switch
        {
            "Warning" => "fa-exclamation-triangle",
            "Info" => "fa-info-circle",
            "Danger" => "fa-exclamation-circle",
            "Success" => "fa-check-circle",
            _ => "fa-bell"
        };

        public string AlertTypeClass => AlertType switch
        {
            "Warning" => "alert-warning",
            "Info" => "alert-info",
            "Danger" => "alert-danger",
            "Success" => "alert-success",
            _ => "alert-secondary"
        };
    }

    /// <summary>
    /// Prochaine étape de traitement
    /// </summary>
    public class TreatmentNextStep
    {
        public string StepType { get; set; } = string.Empty; // Examination, Prescription, CareService, Payment
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Now;
        public string Priority { get; set; } = "Normal"; // Urgent, High, Normal, Low

        // Propriétés d'affichage
        public string StepTypeIcon => StepType switch
        {
            "Examination" => "fa-microscope",
            "Prescription" => "fa-prescription",
            "CareService" => "fa-procedures",
            "Payment" => "fa-money-bill-wave",
            _ => "fa-tasks"
        };

        public string PriorityClass => Priority switch
        {
            "Urgent" => "text-danger",
            "High" => "text-warning",
            "Normal" => "text-primary",
            "Low" => "text-secondary",
            _ => "text-primary"
        };
    }
}