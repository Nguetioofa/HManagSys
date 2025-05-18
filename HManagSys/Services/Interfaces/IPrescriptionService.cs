using HManagSys.Models.ViewModels.Patients;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour la gestion des prescriptions
/// </summary>
public interface IPrescriptionService
{
    Task<PrescriptionViewModel?> GetByIdAsync(int id);
    Task<(List<PrescriptionViewModel> Items, int TotalCount)> GetPrescriptionsAsync(PrescriptionFilters filters);
    Task<OperationResult<PrescriptionViewModel>> CreatePrescriptionAsync(CreatePrescriptionViewModel model, int createdBy);
    Task<OperationResult<PrescriptionViewModel>> UpdatePrescriptionAsync(int id, EditPrescriptionViewModel model, int modifiedBy);
    Task<OperationResult> DisposePrescriptionAsync(int id, int modifiedBy);
    Task<List<PrescriptionViewModel>> GetPatientPrescriptionsAsync(int patientId);
    Task<OperationResult<PrescriptionItemViewModel>> AddPrescriptionItemAsync(int prescriptionId, CreatePrescriptionItemViewModel model, int createdBy);
    Task<OperationResult> RemovePrescriptionItemAsync(int prescriptionItemId, int modifiedBy);
}