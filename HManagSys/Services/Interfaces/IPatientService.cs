using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Patients;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour la gestion des patients
/// "Le gardien des dossiers médicaux"
/// </summary>
public interface IPatientService
{
    // ===== OPÉRATIONS PATIENTS =====

    /// <summary>
    /// Récupère un patient par son ID
    /// </summary>
    Task<Patient?> GetPatientByIdAsync(int id);

    /// <summary>
    /// Récupère un patient avec ses données détaillées
    /// </summary>
    Task<PatientDetailsViewModel?> GetPatientDetailsAsync(int id);

    /// <summary>
    /// Recherche des patients selon divers critères
    /// </summary>
    Task<(List<PatientViewModel> Patients, int TotalCount)> SearchPatientsAsync(PatientFilters filters);

    /// <summary>
    /// Crée un nouveau patient
    /// </summary>
    Task<OperationResult<PatientViewModel>> CreatePatientAsync(CreatePatientViewModel model, int createdBy);

    /// <summary>
    /// Met à jour un patient existant
    /// </summary>
    Task<OperationResult<PatientViewModel>> UpdatePatientAsync(int id, EditPatientViewModel model, int modifiedBy);

    /// <summary>
    /// Change le statut actif/inactif d'un patient
    /// </summary>
    Task<OperationResult> TogglePatientStatusAsync(int id, bool isActive, int modifiedBy);

    // ===== OPÉRATIONS DIAGNOSTICS =====

    /// <summary>
    /// Ajoute un nouveau diagnostic à un patient
    /// </summary>
    Task<OperationResult<DiagnosisViewModel>> AddDiagnosisAsync(CreateDiagnosisViewModel model, int createdBy);

    /// <summary>
    /// Récupère les diagnostics d'un patient
    /// </summary>
    Task<List<DiagnosisViewModel>> GetPatientDiagnosesAsync(int patientId);

    /// <summary>
    /// Récupère un diagnostic spécifique
    /// </summary>
    Task<DiagnosisViewModel?> GetDiagnosisAsync(int diagnosisId);

    // ===== HISTORIQUE PATIENT =====

    /// <summary>
    /// Récupère l'historique médical complet d'un patient
    /// </summary>
    Task<PatientHistoryViewModel> GetPatientHistoryAsync(int patientId);

    // ===== RECHERCHE AVANCÉE =====

    /// <summary>
    /// Recherche rapide de patients (autocomplete)
    /// </summary>
    Task<List<PatientSearchResultViewModel>> QuickSearchPatientsAsync(string searchTerm, int hospitalCenterId);

    /// <summary>
    /// Vérifie les doublons potentiels
    /// </summary>
    Task<List<PatientViewModel>> CheckPotentialDuplicatesAsync(string firstName, string lastName, string phoneNumber);

    // ===== STATISTIQUES =====

    /// <summary>
    /// Obtient des statistiques sur les patients
    /// </summary>
    Task<PatientStatisticsViewModel> GetPatientStatisticsAsync(int hospitalCenterId);
}