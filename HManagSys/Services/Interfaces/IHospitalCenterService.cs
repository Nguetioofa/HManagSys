using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.HospitalCenter;

namespace HManagSys.Services.Interfaces
{
    public interface IHospitalCenterService
    {
        Task<(List<HospitalCenterViewModel> Centers, int TotalCount)> GetCentersAsync(HospitalCenterFilters filters);
        Task<HospitalCenterDetailsViewModel?> GetCenterByIdAsync(int id);
        Task<OperationResult<HospitalCenterViewModel>> CreateCenterAsync(CreateHospitalCenterViewModel model, int createdBy);
        Task<OperationResult<HospitalCenterViewModel>> UpdateCenterAsync(int id, EditHospitalCenterViewModel model, int modifiedBy);
        Task<OperationResult> ToggleCenterStatusAsync(int id, bool isActive, int modifiedBy);
        Task<List<SelectOption>> GetActiveCentersSelectAsync();
        Task<Models.ViewModels.HospitalCenter.CenterActivityReport> GenerateActivityReportAsync(int centerId, DateTime fromDate, DateTime toDate);
        Task<NetworkStatistics> GetNetworkStatisticsAsync();
        Task<List<HospitalCenter>> GetUserAccessibleCentersAsync(int userId);
    }
}