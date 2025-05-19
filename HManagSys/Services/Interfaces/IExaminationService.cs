using HManagSys.Models.ViewModels.Patients;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des examens médicaux
    /// </summary>
    public interface IExaminationService
    {
        Task<ExaminationViewModel?> GetByIdAsync(int id);
        Task<List<ExaminationViewModel>> GetByEpisodeAsync(int episodeId);
        Task<(List<ExaminationViewModel> Items, int TotalCount)> GetExaminationsAsync(ExaminationFilters filters);
        Task<OperationResult<ExaminationViewModel>> CreateExaminationAsync(CreateExaminationViewModel model, int createdBy);
        Task<OperationResult<ExaminationViewModel>> ScheduleExaminationAsync(int id, ScheduleExaminationViewModel model, int modifiedBy);
        Task<OperationResult<ExaminationViewModel>> CompleteExaminationAsync(int id, CompleteExaminationViewModel model, int modifiedBy);
        Task<OperationResult<ExaminationViewModel>> CancelExaminationAsync(int id, string reason, int modifiedBy);
        Task<OperationResult<ExaminationResultViewModel>> AddExaminationResultAsync(int examinationId, CreateExaminationResultViewModel model, int createdBy);
        Task<List<ExaminationViewModel>> GetPatientExaminationsAsync(int patientId);
        Task<ExaminationResultViewModel?> GetExaminationResultAsync(int examinationId);
    }
}
