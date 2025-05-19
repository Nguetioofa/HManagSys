using HManagSys.Models.ViewModels.Dashboard;
using HManagSys.Models.ViewModels.Patients;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour les tableaux de bord médicaux
    /// Fournit des données agrégées et des statistiques pour le suivi médical
    /// </summary>
    public interface IMedicalDashboardService
    {
        /// <summary>
        /// Récupère les données du tableau de bord médical pour un centre spécifique
        /// </summary>
        Task<MedicalDashboardViewModel> GetMedicalDashboardDataAsync(int hospitalCenterId);

        /// <summary>
        /// Récupère les données du tableau de bord pour un patient spécifique
        /// </summary>
        Task<PatientDashboardViewModel> GetPatientDashboardDataAsync(int patientId, int hospitalCenterId);

        /// <summary>
        /// Récupère le résumé d'un épisode de soins
        /// </summary>
        Task<CareEpisodeSummaryViewModel> GetCareEpisodeSummaryAsync(int episodeId);

        /// <summary>
        /// Récupère les données de progression du traitement pour un épisode de soins
        /// </summary>
        Task<TreatmentProgressViewModel> GetTreatmentProgressAsync(int episodeId);

        /// <summary>
        /// Récupère les statistiques médicales récentes pour un centre
        /// </summary>
        Task<RecentMedicalStatisticsViewModel> GetRecentMedicalStatisticsAsync(int hospitalCenterId, int days = 30);
    }
}