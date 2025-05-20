using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Finance;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    public class FinancierService : IFinancierService
    {
        private readonly IGenericRepository<Financier> _financierRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IGenericRepository<CashHandover> _cashHandoverRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public FinancierService(
            IGenericRepository<Financier> financierRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IGenericRepository<CashHandover> cashHandoverRepository,
            IGenericRepository<User> userRepository,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _financierRepository = financierRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _cashHandoverRepository = cashHandoverRepository;
            _userRepository = userRepository;
            _logger = logger;
            _auditService = auditService;
        }

        /// <summary>
        /// Récupère un financier par son ID
        /// </summary>
        public async Task<FinancierViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var financier = await _financierRepository.QuerySingleAsync(q =>
                    q.Where(f => f.Id == id)
                     .Include(f => f.HospitalCenter)
                     .Select(f => new FinancierViewModel
                     {
                         Id = f.Id,
                         Name = f.Name,
                         HospitalCenterId = f.HospitalCenterId,
                         HospitalCenterName = f.HospitalCenter.Name,
                         ContactInfo = f.ContactInfo,
                         IsActive = f.IsActive,
                         CreatedBy = f.CreatedBy,
                         CreatedAt = f.CreatedAt,
                         ModifiedBy = f.ModifiedBy,
                         ModifiedAt = f.ModifiedAt
                     }));

                if (financier == null)
                    return null;

                // Récupérer les créateurs/modificateurs
                if (financier.CreatedBy > 0)
                {
                    var creator = await _userRepository.GetByIdAsync(financier.CreatedBy);
                    if (creator != null)
                    {
                        financier.CreatedByName = $"{creator.FirstName} {creator.LastName}";
                    }
                }

                if (financier.ModifiedBy.HasValue && financier.ModifiedBy.Value > 0)
                {
                    var modifier = await _userRepository.GetByIdAsync(financier.ModifiedBy.Value);
                    if (modifier != null)
                    {
                        financier.ModifiedByName = $"{modifier.FirstName} {modifier.LastName}";
                    }
                }

                // Récupérer les statistiques
                var handovers = await _cashHandoverRepository.QueryListAsync(q =>
                    q.Where(h => h.FinancierId == id)
                     .OrderByDescending(h => h.HandoverDate));

                financier.TotalHandovers = handovers.Count;
                financier.TotalAmountCollected = handovers.Sum(h => h.HandoverAmount);
                financier.LastHandoverDate = handovers.Any() ? handovers.First().HandoverDate : null;

                return financier;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "GetByIdError",
                    $"Erreur lors de la récupération du financier {id}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère tous les financiers d'un centre
        /// </summary>
        public async Task<List<FinancierViewModel>> GetFinanciersByCenterAsync(int hospitalCenterId)
        {
            try
            {
                var financiers = await _financierRepository.QueryListAsync(q =>
                    q.Where(f => f.HospitalCenterId == hospitalCenterId)
                     .Include(f => f.HospitalCenter)
                     .OrderBy(f => f.Name)
                     .Select(f => new FinancierViewModel
                     {
                         Id = f.Id,
                         Name = f.Name,
                         HospitalCenterId = f.HospitalCenterId,
                         HospitalCenterName = f.HospitalCenter.Name,
                         ContactInfo = f.ContactInfo,
                         IsActive = f.IsActive,
                         CreatedAt = f.CreatedAt
                     }));

                return financiers;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "GetFinanciersByCenterError",
                    $"Erreur lors de la récupération des financiers du centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère tous les financiers actifs pour une liste déroulante
        /// </summary>
        public async Task<List<SelectOption>> GetActiveFinanciersSelectAsync(int hospitalCenterId)
        {
            try
            {
                var financiers = await _financierRepository.QueryListAsync(q =>
                    q.Where(f => f.HospitalCenterId == hospitalCenterId && f.IsActive)
                     .OrderBy(f => f.Name)
                     .Select(f => new SelectOption
                     {
                         Value = f.Id.ToString(),
                         Text = f.Name
                     }));

                return financiers;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "GetActiveFinanciersSelectError",
                    $"Erreur lors de la récupération des financiers actifs du centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les financiers avec pagination et filtres
        /// </summary>
        public async Task<(List<FinancierViewModel> Items, int TotalCount)> GetFinanciersAsync(FinancierFilters filters)
        {
            try
            {
                // Construire la requête de base
                var query = _financierRepository.QueryListAsync<FinancierViewModel>(q =>
                {
                    var baseQuery = q.Include(f => f.HospitalCenter).AsQueryable();

                    // Appliquer les filtres
                    if (filters.HospitalCenterId.HasValue)
                    {
                        baseQuery = baseQuery.Where(f => f.HospitalCenterId == filters.HospitalCenterId.Value);
                    }

                    if (filters.IsActive.HasValue)
                    {
                        baseQuery = baseQuery.Where(f => f.IsActive == filters.IsActive.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchTerm = filters.SearchTerm.ToLower();
                        baseQuery = baseQuery.Where(f =>
                            f.Name.ToLower().Contains(searchTerm) ||
                            (f.ContactInfo != null && f.ContactInfo.ToLower().Contains(searchTerm)));
                    }

                    // Compter le nombre total d'éléments
                    var totalCount = baseQuery.Count();

                    // Appliquer la pagination
                    var pagedQuery = baseQuery
                        .OrderBy(f => f.Name)
                        .Skip((filters.PageIndex - 1) * filters.PageSize)
                        .Take(filters.PageSize);

                    // Projection
                    return pagedQuery.Select(f => new FinancierViewModel
                    {
                        Id = f.Id,
                        Name = f.Name,
                        HospitalCenterId = f.HospitalCenterId,
                        HospitalCenterName = f.HospitalCenter.Name,
                        ContactInfo = f.ContactInfo,
                        IsActive = f.IsActive,
                        CreatedBy = f.CreatedBy,
                        CreatedAt = f.CreatedAt
                    });
                });

                // Exécuter la requête
                var items = await query;

                // Récupérer le nombre total
                var totalCount = await _financierRepository.CountAsync(q =>
                {
                    var baseQuery = q.AsQueryable();

                    // Appliquer les filtres pour le comptage
                    if (filters.HospitalCenterId.HasValue)
                    {
                        baseQuery = baseQuery.Where(f => f.HospitalCenterId == filters.HospitalCenterId.Value);
                    }

                    if (filters.IsActive.HasValue)
                    {
                        baseQuery = baseQuery.Where(f => f.IsActive == filters.IsActive.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchTerm = filters.SearchTerm.ToLower();
                        baseQuery = baseQuery.Where(f =>
                            f.Name.ToLower().Contains(searchTerm) ||
                            (f.ContactInfo != null && f.ContactInfo.ToLower().Contains(searchTerm)));
                    }

                    return baseQuery;
                });

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "GetFinanciersError",
                    "Erreur lors de la récupération des financiers",
                    details: new { Filters = filters, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Crée un nouveau financier
        /// </summary>
        public async Task<OperationResult<FinancierViewModel>> CreateFinancierAsync(CreateFinancierViewModel model, int createdBy)
        {
            try
            {
                // Vérifier que le centre existe
                var center = await _hospitalCenterRepository.GetByIdAsync(model.HospitalCenterId);
                if (center == null)
                {
                    return OperationResult<FinancierViewModel>.Error("Centre hospitalier invalide");
                }

                // Vérifier si un financier avec le même nom existe déjà dans ce centre
                var existingFinancier = await _financierRepository.AnyAsync(q =>
                    q.Where(f => f.Name.ToLower() == model.Name.ToLower() &&
                               f.HospitalCenterId == model.HospitalCenterId));

                if (existingFinancier)
                {
                    return OperationResult<FinancierViewModel>.Error("Un financier avec ce nom existe déjà dans ce centre");
                }

                // Créer le financier
                var financier = new Financier
                {
                    Name = model.Name,
                    HospitalCenterId = model.HospitalCenterId,
                    ContactInfo = model.ContactInfo,
                    IsActive = model.IsActive,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var createdFinancier = await _financierRepository.AddAsync(financier);

                // Journaliser l'action
                await _auditService.LogActionAsync(
                    createdBy,
                    "FINANCIER_CREATE",
                    "Financier",
                    createdFinancier.Id,
                    null,
                    new { createdFinancier.Name, createdFinancier.HospitalCenterId, createdFinancier.IsActive },
                    $"Création du financier {createdFinancier.Name}"
                );

                // Retourner le viewmodel
                var result = await GetByIdAsync(createdFinancier.Id);
                return OperationResult<FinancierViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "CreateFinancierError",
                    "Erreur lors de la création du financier",
                    createdBy,
                    model.HospitalCenterId,
                    details: new { Model = model, Error = ex.Message });

                return OperationResult<FinancierViewModel>.Error("Une erreur est survenue lors de la création du financier");
            }
        }

        /// <summary>
        /// Met à jour un financier existant
        /// </summary>
        public async Task<OperationResult<FinancierViewModel>> UpdateFinancierAsync(int id, EditFinancierViewModel model, int modifiedBy)
        {
            try
            {
                // Récupérer le financier
                var financier = await _financierRepository.GetByIdAsync(id);
                if (financier == null)
                {
                    return OperationResult<FinancierViewModel>.Error("Financier introuvable");
                }

                // Vérifier si un autre financier avec le même nom existe déjà dans ce centre
                var existingFinancier = await _financierRepository.AnyAsync(q =>
                    q.Where(f => f.Name.ToLower() == model.Name.ToLower() &&
                               f.HospitalCenterId == financier.HospitalCenterId &&
                               f.Id != id));

                if (existingFinancier)
                {
                    return OperationResult<FinancierViewModel>.Error("Un autre financier avec ce nom existe déjà dans ce centre");
                }

                // Sauvegarder l'état actuel pour l'audit
                var oldValues = new
                {
                    financier.Name,
                    financier.ContactInfo,
                    financier.IsActive
                };

                // Mettre à jour le financier
                financier.Name = model.Name;
                financier.ContactInfo = model.ContactInfo;
                financier.IsActive = model.IsActive;
                financier.ModifiedBy = modifiedBy;
                financier.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                var newValues = new
                {
                    financier.Name,
                    financier.ContactInfo,
                    financier.IsActive
                };

                var updated = await _financierRepository.UpdateAsync(financier);
                if (!updated)
                {
                    return OperationResult<FinancierViewModel>.Error("Erreur lors de la mise à jour du financier");
                }

                // Journaliser l'action
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "FINANCIER_UPDATE",
                    "Financier",
                    id,
                    oldValues,
                    newValues,
                    $"Mise à jour du financier {financier.Name}"
                );

                // Retourner le viewmodel
                var result = await GetByIdAsync(id);
                return OperationResult<FinancierViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "UpdateFinancierError",
                    $"Erreur lors de la mise à jour du financier {id}",
                    modifiedBy,
                    null,
                    details: new { Model = model, Error = ex.Message });

                return OperationResult<FinancierViewModel>.Error("Une erreur est survenue lors de la mise à jour du financier");
            }
        }

        /// <summary>
        /// Active ou désactive un financier
        /// </summary>
        public async Task<OperationResult> ToggleFinancierStatusAsync(int id, bool isActive, int modifiedBy)
        {
            try
            {
                // Récupérer le financier
                var financier = await _financierRepository.GetByIdAsync(id);
                if (financier == null)
                {
                    return OperationResult.Error("Financier introuvable");
                }

                // Si l'état est déjà celui demandé, ne rien faire
                if (financier.IsActive == isActive)
                {
                    return OperationResult.Success();
                }

                // Sauvegarder l'état actuel pour l'audit
                var oldValues = new
                {
                    financier.IsActive
                };

                // Mettre à jour le financier
                financier.IsActive = isActive;
                financier.ModifiedBy = modifiedBy;
                financier.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                var newValues = new
                {
                    financier.IsActive
                };

                var updated = await _financierRepository.UpdateAsync(financier);
                if (!updated)
                {
                    return OperationResult.Error("Erreur lors de la mise à jour du statut du financier");
                }

                // Journaliser l'action
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "FINANCIER_STATUS_UPDATE",
                    "Financier",
                    id,
                    oldValues,
                    newValues,
                    $"Changement de statut du financier {financier.Name} à {(isActive ? "actif" : "inactif")}"
                );

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("FinancierService", "ToggleFinancierStatusError",
                    $"Erreur lors du changement de statut du financier {id}",
                    modifiedBy,
                    null,
                    details: new { Id = id, IsActive = isActive, Error = ex.Message });

                return OperationResult.Error("Une erreur est survenue lors du changement de statut du financier");
            }
        }
    }
}