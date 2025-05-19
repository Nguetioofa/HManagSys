using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Patients;

namespace HManagSys.Services.Interfaces
{
    /// <summary>
    /// Service pour la gestion des épisodes de soins
    /// </summary>
    public interface ICareEpisodeService
    {
        Task<List<CareEpisode>> QueryListAsync(Func<IQueryable<CareEpisode>, IQueryable<CareEpisode>> queryBuilder);
        Task<CareEpisodeViewModel?> GetByIdAsync(int id);
        Task<CareServiceProductModalsViewModel?> GetServiceProducts(int serviceId);
        Task<(List<CareEpisodeViewModel> Items, int TotalCount)> GetCareEpisodesAsync(CareEpisodeFilters filters);
        Task<OperationResult<CareEpisodeViewModel>> CreateCareEpisodeAsync(CreateCareEpisodeViewModel model, int createdBy);
        Task<OperationResult<CareEpisodeViewModel>> UpdateCareEpisodeAsync(int id, EditCareEpisodeViewModel model, int modifiedBy);
        Task<OperationResult> CompleteCareEpisodeAsync(int id, CompleteCareEpisodeViewModel model, int modifiedBy);
        Task<OperationResult> InterruptCareEpisodeAsync(int id, InterruptCareEpisodeViewModel model, int modifiedBy);
        Task<List<CareEpisodeViewModel>> GetPatientCareEpisodesAsync(int patientId);
        Task<List<CareServiceViewModel>> GetCareServicesAsync(int episodeId);
        Task<CareServiceViewModel> GetCareServiceByIdAsync(int serviceId);
        Task<OperationResult<CareServiceViewModel>> AddCareServiceAsync(CreateCareServiceViewModel model, int createdBy);
    }
}
