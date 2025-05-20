using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.HospitalCenter;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    public class HospitalCenterService : IHospitalCenterService
    {
        private readonly IHospitalCenterRepository _hospitalCenterRepository;
        private readonly IUserCenterAssignmentRepository _userCenterAssignmentRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
        private readonly IGenericRepository<Sale> _saleRepository;
        private readonly IGenericRepository<Payment> _paymentRepository;
        private readonly IGenericRepository<StockInventory> _stockInventoryRepository;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public HospitalCenterService(
            IHospitalCenterRepository hospitalCenterRepository,
            IUserCenterAssignmentRepository userCenterAssignmentRepository,
            IGenericRepository<User> userRepository,
            IGenericRepository<CareEpisode> careEpisodeRepository,
            IGenericRepository<Sale> saleRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<StockInventory> stockInventoryRepository,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _hospitalCenterRepository = hospitalCenterRepository;
            _userCenterAssignmentRepository = userCenterAssignmentRepository;
            _userRepository = userRepository;
            _careEpisodeRepository = careEpisodeRepository;
            _saleRepository = saleRepository;
            _paymentRepository = paymentRepository;
            _stockInventoryRepository = stockInventoryRepository;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<(List<HospitalCenterViewModel> Centers, int TotalCount)> GetCentersAsync(HospitalCenterFilters filters)
        {
            try
            {
                var query = _hospitalCenterRepository.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(c => c.Name.ToLower().Contains(searchTerm) ||
                                           c.Address.ToLower().Contains(searchTerm) ||
                                           (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
                }

                if (filters.IsActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == filters.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Region))
                {
                    var region = filters.Region.ToLower();
                    query = query.Where(c => c.Address.ToLower().Contains(region));
                }

                // Count total before pagination
                var totalCount = await query.CountAsync();

                // Apply ordering and pagination
                var pagedQuery = query.OrderBy(c => c.Name)
                                     .Skip((filters.PageIndex - 1) * filters.PageSize)
                                     .Take(filters.PageSize);

                // Execute query
                var centers = await pagedQuery.ToListAsync();

                // Map to view models
                var viewModels = new List<HospitalCenterViewModel>();

                foreach (var center in centers)
                {
                    var activeUsers = await _userCenterAssignmentRepository.GetCenterActiveAssignmentsAsync(center.Id);

                    viewModels.Add(new HospitalCenterViewModel
                    {
                        Id = center.Id,
                        Name = center.Name,
                        Address = center.Address,
                        PhoneNumber = center.PhoneNumber,
                        Email = center.Email,
                        IsActive = center.IsActive,
                        CreatedAt = center.CreatedAt,
                        ModifiedAt = center.ModifiedAt,
                        ActiveUsersCount = activeUsers.Count
                    });
                }

                return (viewModels, totalCount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GetCentersError",
                    "Error retrieving hospital centers", details: new { Error = ex.Message });
                throw;
            }
        }

        public async Task<HospitalCenterDetailsViewModel?> GetCenterByIdAsync(int id)
        {
            try
            {
                var center = await _hospitalCenterRepository.GetByIdAsync(id);
                if (center == null)
                    return null;

                var statistics = await _hospitalCenterRepository.GetCenterWithStatsAsync(id);
                if (statistics == null)
                    return null;

                var creator = await _userRepository.GetByIdAsync(center.CreatedBy);
                var modifier = center.ModifiedBy.HasValue ? await _userRepository.GetByIdAsync(center.ModifiedBy.Value) : null;

                var viewModel = new HospitalCenterDetailsViewModel
                {
                    Id = center.Id,
                    Name = center.Name,
                    Address = center.Address,
                    PhoneNumber = center.PhoneNumber,
                    Email = center.Email,
                    IsActive = center.IsActive,
                    CreatedAt = center.CreatedAt,
                    CreatedByName = creator != null ? $"{creator.FirstName} {creator.LastName}" : "Unknown",
                    ModifiedAt = center.ModifiedAt,
                    ModifiedByName = modifier != null ? $"{modifier.FirstName} {modifier.LastName}" : null,
                    ActiveUsers = statistics.ActiveUsers,
                    ProductsInStock = statistics.ProductsInStock,
                    TotalSales = statistics.TotalSales,
                    ActiveCareEpisodes = statistics.ActiveCareEpisodes
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GetCenterByIdError",
                    $"Error retrieving hospital center {id}", details: new { CenterId = id, Error = ex.Message });
                throw;
            }
        }

        public async Task<OperationResult<HospitalCenterViewModel>> CreateCenterAsync(CreateHospitalCenterViewModel model, int createdBy)
        {
            try
            {
                // Check if name is unique
                var existingCenter = await _hospitalCenterRepository.AnyAsync(q =>
                    q.Where(c => c.Name.ToLower() == model.Name.ToLower()));

                if (existingCenter)
                {
                    return OperationResult<HospitalCenterViewModel>.Error("Un centre avec ce nom existe déjà");
                }

                var center = new HospitalCenter
                {
                    Name = model.Name.Trim(),
                    Address = model.Address.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim(),
                    Email = model.Email?.ToLower().Trim(),
                    IsActive = model.IsActive,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var createdCenter = await _hospitalCenterRepository.AddAsync(center);

                // Log the action
                await _auditService.LogActionAsync(
                    createdBy,
                    "CENTER_CREATE",
                    "HospitalCenter",
                    createdCenter.Id,
                    null,
                    new { createdCenter.Name, createdCenter.Address, createdCenter.IsActive },
                    $"Creation of hospital center {createdCenter.Name}"
                );

                var viewModel = new HospitalCenterViewModel
                {
                    Id = createdCenter.Id,
                    Name = createdCenter.Name,
                    Address = createdCenter.Address,
                    PhoneNumber = createdCenter.PhoneNumber,
                    Email = createdCenter.Email,
                    IsActive = createdCenter.IsActive,
                    CreatedAt = createdCenter.CreatedAt,
                    ActiveUsersCount = 0
                };

                return OperationResult<HospitalCenterViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "CreateCenterError",
                    "Error creating hospital center", createdBy, null, details: new { Model = model, Error = ex.Message });

                return OperationResult<HospitalCenterViewModel>.Error("Une erreur est survenue lors de la création du centre");
            }
        }

        public async Task<OperationResult<HospitalCenterViewModel>> UpdateCenterAsync(int id, EditHospitalCenterViewModel model, int modifiedBy)
        {
            try
            {
                var center = await _hospitalCenterRepository.GetByIdAsync(id);
                if (center == null)
                {
                    return OperationResult<HospitalCenterViewModel>.Error("Centre introuvable");
                }

                // Check if name is unique (excluding current center)
                var existingCenter = await _hospitalCenterRepository.AnyAsync(q =>
                    q.Where(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != id));

                if (existingCenter)
                {
                    return OperationResult<HospitalCenterViewModel>.Error("Un autre centre avec ce nom existe déjà");
                }

                // Store old values for audit
                var oldValues = new
                {
                    center.Name,
                    center.Address,
                    center.PhoneNumber,
                    center.Email,
                    center.IsActive
                };

                // Update center
                center.Name = model.Name.Trim();
                center.Address = model.Address.Trim();
                center.PhoneNumber = model.PhoneNumber?.Trim();
                center.Email = model.Email?.ToLower().Trim();
                center.IsActive = model.IsActive;
                center.ModifiedBy = modifiedBy;
                center.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                var newValues = new
                {
                    center.Name,
                    center.Address,
                    center.PhoneNumber,
                    center.Email,
                    center.IsActive
                };

                var updated = await _hospitalCenterRepository.UpdateAsync(center);
                if (!updated)
                {
                    return OperationResult<HospitalCenterViewModel>.Error("Erreur lors de la mise à jour du centre");
                }

                // Log the action
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "CENTER_UPDATE",
                    "HospitalCenter",
                    id,
                    oldValues,
                    newValues,
                    $"Update of hospital center {center.Name}"
                );

                var activeUsers = await _userCenterAssignmentRepository.GetCenterActiveAssignmentsAsync(center.Id);

                var viewModel = new HospitalCenterViewModel
                {
                    Id = center.Id,
                    Name = center.Name,
                    Address = center.Address,
                    PhoneNumber = center.PhoneNumber,
                    Email = center.Email,
                    IsActive = center.IsActive,
                    CreatedAt = center.CreatedAt,
                    ModifiedAt = center.ModifiedAt,
                    ActiveUsersCount = activeUsers.Count
                };

                return OperationResult<HospitalCenterViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "UpdateCenterError",
                    $"Error updating hospital center {id}", modifiedBy, null, details: new { Model = model, Error = ex.Message });

                return OperationResult<HospitalCenterViewModel>.Error("Une erreur est survenue lors de la mise à jour du centre");
            }
        }

        public async Task<OperationResult> ToggleCenterStatusAsync(int id, bool isActive, int modifiedBy)
        {
            try
            {
                var (success, warning) = await _hospitalCenterRepository.SetCenterActiveStatusAsync(id, isActive, modifiedBy);

                if (!success)
                {
                    return OperationResult.Error(warning ?? "Erreur lors du changement de statut du centre");
                }

                return warning != null
                    ? OperationResult.Success()
                    : OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "ToggleCenterStatusError",
                    $"Error toggling status for hospital center {id}", modifiedBy, null, details: new { CenterId = id, IsActive = isActive, Error = ex.Message });

                return OperationResult.Error("Une erreur est survenue lors du changement de statut du centre");
            }
        }

        public async Task<List<SelectOption>> GetActiveCentersSelectAsync()
        {
            try
            {
                var centers = await _hospitalCenterRepository.GetAllAsync(q =>
                    q.Where(c => c.IsActive)
                     .OrderBy(c => c.Name));

                return centers.Select(c => new SelectOption(c.Id.ToString(), c.Name)).ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GetActiveCentersSelectError",
                    "Error retrieving active centers for select", details: new { Error = ex.Message });
                throw;
            }
        }

        public async Task<Models.ViewModels.HospitalCenter.CenterActivityReport> GenerateActivityReportAsync(int centerId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                return await _hospitalCenterRepository.GenerateActivityReportAsync(centerId, fromDate, toDate);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GenerateActivityReportError",
                    $"Error generating activity report for center {centerId}", details: new { CenterId = centerId, FromDate = fromDate, ToDate = toDate, Error = ex.Message });
                throw;
            }
        }

        public async Task<NetworkStatistics> GetNetworkStatisticsAsync()
        {
            try
            {
                return await _hospitalCenterRepository.GetNetworkStatisticsAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GetNetworkStatisticsError",
                    "Error retrieving network statistics", details: new { Error = ex.Message });
                throw;
            }
        }

        public async Task<List<HospitalCenter>> GetUserAccessibleCentersAsync(int userId)
        {
            try
            {
                return await _hospitalCenterRepository.GetUserAccessibleCentersAsync(userId);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("HospitalCenterService", "GetUserAccessibleCentersError",
                    $"Error retrieving accessible centers for user {userId}", details: new { UserId = userId, Error = ex.Message });
                throw;
            }
        }
    }
}