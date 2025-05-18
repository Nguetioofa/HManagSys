using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations;

public class CareEpisodeService : ICareEpisodeService
{
    private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
    private readonly IGenericRepository<CareService> _careServiceRepository;
    private readonly IGenericRepository<CareServiceProduct> _careServiceProductRepository;
    private readonly IGenericRepository<StockMovement> _stockMovementRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Diagnosis> _diagnosisRepository;
    private readonly IGenericRepository<HospitalCenter> _centerRepository;
    private readonly IGenericRepository<CareType> _careTypeRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<StockInventory> _stockInventoryRepository;
    private readonly IApplicationLogger _logger;
    private readonly IAuditService _auditService;

    public CareEpisodeService(
        IGenericRepository<CareEpisode> careEpisodeRepository,
        IGenericRepository<CareService> careServiceRepository,
        IGenericRepository<CareServiceProduct> careServiceProductRepository,
        IGenericRepository<StockMovement> stockMovementRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<Diagnosis> diagnosisRepository,
        IGenericRepository<HospitalCenter> centerRepository,
        IGenericRepository<CareType> careTypeRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<StockInventory> stockInventoryRepository,
        IApplicationLogger logger,
        IAuditService auditService)
    {
        _careEpisodeRepository = careEpisodeRepository;
        _careServiceRepository = careServiceRepository;
        _careServiceProductRepository = careServiceProductRepository;
        _stockMovementRepository = stockMovementRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _diagnosisRepository = diagnosisRepository;
        _centerRepository = centerRepository;
        _careTypeRepository = careTypeRepository;
        _productRepository = productRepository;
        _stockInventoryRepository = stockInventoryRepository;
        _logger = logger;
        _auditService = auditService;
    }

    // Récupérer un épisode de soins par ID avec toutes les relations
    public async Task<CareEpisodeViewModel?> GetByIdAsync(int id)
    {
        try
        {
            // Récupérer l'épisode avec ses relations
            var episode = await _careEpisodeRepository.QuerySingleAsync<CareEpisodeViewModel>(q =>
                q.Where(e => e.Id == id)
                 .Include(e => e.Patient)
                 .Include(e => e.Diagnosis)
                 .Include(e => e.HospitalCenter)
                 .Include(e => e.PrimaryCaregiverNavigation)
                 .Select(e => new CareEpisodeViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     DiagnosisId = e.DiagnosisId,
                     DiagnosisName = e.Diagnosis.DiagnosisName,
                     HospitalCenterId = e.HospitalCenterId,
                     HospitalCenterName = e.HospitalCenter.Name,
                     PrimaryCaregiverId = e.PrimaryCaregiver,
                     PrimaryCaregiverName = $"{e.PrimaryCaregiverNavigation.FirstName} {e.PrimaryCaregiverNavigation.LastName}",
                     EpisodeStartDate = e.EpisodeStartDate,
                     EpisodeEndDate = e.EpisodeEndDate,
                     Status = e.Status,
                     InterruptionReason = e.InterruptionReason,
                     TotalCost = e.TotalCost,
                     AmountPaid = e.AmountPaid,
                     RemainingBalance = e.RemainingBalance
                 }));

            if (episode == null)
                return null;

            // Récupérer les services liés à cet épisode
            episode.CareServices = await _careServiceRepository.QueryListAsync(q =>
                q.Where(s => s.CareEpisodeId == id)
                 .Include(s => s.CareType)
                 .Include(s => s.AdministeredByNavigation)
                 .Include(s => s.CareServiceProducts)
                    .ThenInclude(p => p.Product)
                 .OrderByDescending(s => s.ServiceDate)
                 .Select(s => new CareServiceViewModel
                 {
                     Id = s.Id,
                     CareEpisodeId = s.CareEpisodeId,
                     CareTypeId = s.CareTypeId,
                     CareTypeName = s.CareType.Name,
                     AdministeredById = s.AdministeredBy,
                     AdministeredByName = $"{s.AdministeredByNavigation.FirstName} {s.AdministeredByNavigation.LastName}",
                     ServiceDate = s.ServiceDate,
                     Duration = s.Duration,
                     Notes = s.Notes,
                     Cost = s.Cost,
                     UsedProducts = s.CareServiceProducts.Select(p => new CareServiceProductViewModel
                     {
                         Id = p.Id,
                         CareServiceId = p.CareServiceId,
                         ProductId = p.ProductId,
                         ProductName = p.Product.Name,
                         QuantityUsed = p.QuantityUsed,
                         UnitCost = p.UnitCost,
                         TotalCost = p.TotalCost
                     }).ToList()
                 }));

            return episode;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "GetByIdError",
                $"Erreur lors de la récupération de l'épisode de soins {id}",
                details: new { EpisodeId = id, Error = ex.Message });
            throw;
        }
    }

    // Récupérer les épisodes de soins avec pagination et filtres
    public async Task<(List<CareEpisodeViewModel> Items, int TotalCount)> GetCareEpisodesAsync(CareEpisodeFilters filters)
    {
        try
        {
            int TotalCount = 0;

            var query = await _careEpisodeRepository.QueryListAsync<CareEpisodeViewModel>(q =>
            {
                var baseQuery = q.Include(e => e.Patient)
                                .Include(e => e.Diagnosis)
                                .Include(e => e.HospitalCenter)
                                .Include(e => e.PrimaryCaregiverNavigation)
                                .AsQueryable();

                // Filtre par centre hospitalier
                if (filters.HospitalCenterId.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.HospitalCenterId == filters.HospitalCenterId.Value);
                }

                // Filtre par patient
                if (filters.PatientId.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.PatientId == filters.PatientId.Value);
                }

                // Filtre par soignant principal
                if (filters.PrimaryCaregiver.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.PrimaryCaregiver == filters.PrimaryCaregiver.Value);
                }

                // Filtre par statut
                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    baseQuery = baseQuery.Where(e => e.Status == filters.Status);
                }

                // Filtre par période
                if (filters.FromDate.HasValue)
                {
                    var fromDate = filters.FromDate.Value.Date;
                    baseQuery = baseQuery.Where(e => e.EpisodeStartDate >= fromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                    baseQuery = baseQuery.Where(e => e.EpisodeStartDate <= toDate);
                }

                // Filtre par recherche
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    baseQuery = baseQuery.Where(e =>
                        e.Patient.FirstName.ToLower().Contains(searchTerm) ||
                        e.Patient.LastName.ToLower().Contains(searchTerm) ||
                        e.Diagnosis.DiagnosisName.ToLower().Contains(searchTerm) ||
                        e.PrimaryCaregiverNavigation.FirstName.ToLower().Contains(searchTerm) ||
                        e.PrimaryCaregiverNavigation.LastName.ToLower().Contains(searchTerm));
                }

                // Récupérer le nombre total
                TotalCount = baseQuery.Count();

                // Pagination
                var pagedQuery = baseQuery
                    .OrderByDescending(e => e.EpisodeStartDate)
                    .Skip((filters.PageIndex - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(e => new CareEpisodeViewModel
                    {
                        Id = e.Id,
                        PatientId = e.PatientId,
                        PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                        DiagnosisId = e.DiagnosisId,
                        DiagnosisName = e.Diagnosis.DiagnosisName,
                        HospitalCenterId = e.HospitalCenterId,
                        HospitalCenterName = e.HospitalCenter.Name,
                        PrimaryCaregiverId = e.PrimaryCaregiver,
                        PrimaryCaregiverName = $"{e.PrimaryCaregiverNavigation.FirstName} {e.PrimaryCaregiverNavigation.LastName}",
                        EpisodeStartDate = e.EpisodeStartDate,
                        EpisodeEndDate = e.EpisodeEndDate,
                        Status = e.Status,
                        InterruptionReason = e.InterruptionReason,
                        TotalCost = e.TotalCost,
                        AmountPaid = e.AmountPaid,
                        RemainingBalance = e.RemainingBalance
                    });

                return pagedQuery;
            });

            return (query, TotalCount);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "GetCareEpisodesError",
                "Erreur lors de la récupération des épisodes de soins",
                details: new { Filters = filters, Error = ex.Message });
            throw;
        }
    }

    // Créer un nouvel épisode de soins
    public async Task<OperationResult<CareEpisodeViewModel>> CreateCareEpisodeAsync(CreateCareEpisodeViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que le patient existe
            var patient = await _patientRepository.GetByIdAsync(model.PatientId);
            if (patient == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Patient introuvable");
            }

            // Vérifier que le diagnostic existe
            var diagnosis = await _diagnosisRepository.GetByIdAsync(model.DiagnosisId);
            if (diagnosis == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Diagnostic introuvable");
            }

            // Vérifier que le centre existe
            var center = await _centerRepository.GetByIdAsync(model.HospitalCenterId);
            if (center == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Centre hospitalier introuvable");
            }

            // Vérifier que le soignant existe
            var caregiver = await _userRepository.GetByIdAsync(model.PrimaryCaregiver);
            if (caregiver == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Soignant introuvable");
            }

            // Vérifier qu'il n'y a pas d'épisode actif pour ce patient avec le même diagnostic
            var hasActiveEpisode = await _careEpisodeRepository.AnyAsync(q =>
                q.Where(e =>
                    e.PatientId == model.PatientId &&
                    e.DiagnosisId == model.DiagnosisId &&
                    e.Status == "Active"));

            if (hasActiveEpisode)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Un épisode de soins actif existe déjà pour ce patient avec ce diagnostic");
            }

            // Créer l'épisode de soins
            var careEpisode = new CareEpisode
            {
                PatientId = model.PatientId,
                DiagnosisId = model.DiagnosisId,
                HospitalCenterId = model.HospitalCenterId,
                PrimaryCaregiver = model.PrimaryCaregiver,
                EpisodeStartDate = model.EpisodeStartDate,
                Status = "Active",
                TotalCost = 0,
                AmountPaid = 0,
                RemainingBalance = 0,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdEpisode = await _careEpisodeRepository.AddAsync(careEpisode);

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "CareEpisode",
                createdEpisode.Id,
                null,
                new
                {
                    PatientId = model.PatientId,
                    DiagnosisId = model.DiagnosisId,
                    StartDate = model.EpisodeStartDate
                },
                $"Création d'un nouvel épisode de soins pour le patient {patient.FirstName} {patient.LastName}"
            );

            // Log
            await _logger.LogInfoAsync("CareEpisodeService", "CareEpisodeCreated",
                $"Nouvel épisode de soins créé pour le patient {patient.FirstName} {patient.LastName}",
                createdBy,
                model.HospitalCenterId,
                details: new { EpisodeId = createdEpisode.Id });

            // Retourner l'épisode créé
            var viewModel = await GetByIdAsync(createdEpisode.Id);
            return OperationResult<CareEpisodeViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "CreateCareEpisodeError",
                "Erreur lors de la création de l'épisode de soins",
                createdBy,
                model.HospitalCenterId,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<CareEpisodeViewModel>.Error("Une erreur est survenue lors de la création de l'épisode de soins");
        }
    }

    // Mettre à jour un épisode de soins
    public async Task<OperationResult<CareEpisodeViewModel>> UpdateCareEpisodeAsync(int id, EditCareEpisodeViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que l'épisode existe
            var episode = await _careEpisodeRepository.GetByIdAsync(id);
            if (episode == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Épisode de soins introuvable");
            }

            // Vérifier que l'épisode n'est pas déjà terminé ou interrompu
            if (episode.Status != "Active")
            {
                return OperationResult<CareEpisodeViewModel>.Error("Impossible de modifier un épisode de soins terminé ou interrompu");
            }

            // Vérifier que le diagnostic existe
            var diagnosis = await _diagnosisRepository.GetByIdAsync(model.DiagnosisId);
            if (diagnosis == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Diagnostic introuvable");
            }

            // Vérifier que le soignant existe
            var caregiver = await _userRepository.GetByIdAsync(model.PrimaryCaregiver);
            if (caregiver == null)
            {
                return OperationResult<CareEpisodeViewModel>.Error("Soignant introuvable");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                DiagnosisId = episode.DiagnosisId,
                PrimaryCaregiver = episode.PrimaryCaregiver,
                EpisodeStartDate = episode.EpisodeStartDate
            };

            // Mettre à jour l'épisode
            episode.DiagnosisId = model.DiagnosisId;
            episode.PrimaryCaregiver = model.PrimaryCaregiver;
            episode.EpisodeStartDate = model.EpisodeStartDate;
            episode.ModifiedBy = modifiedBy;
            episode.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _careEpisodeRepository.UpdateAsync(episode);

            // Audit
            var newValues = new
            {
                DiagnosisId = episode.DiagnosisId,
                PrimaryCaregiver = episode.PrimaryCaregiver,
                EpisodeStartDate = episode.EpisodeStartDate
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "UPDATE",
                "CareEpisode",
                id,
                oldValues,
                newValues,
                $"Modification de l'épisode de soins #{id}"
            );

            // Log
            await _logger.LogInfoAsync("CareEpisodeService", "CareEpisodeUpdated",
                $"Épisode de soins #{id} modifié",
                modifiedBy,
                episode.HospitalCenterId,
                details: new { EpisodeId = id });

            // Retourner l'épisode mis à jour
            var viewModel = await GetByIdAsync(id);
            return OperationResult<CareEpisodeViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "UpdateCareEpisodeError",
                $"Erreur lors de la modification de l'épisode de soins {id}",
                modifiedBy,
                details: new { EpisodeId = id, Model = model, Error = ex.Message });
            return OperationResult<CareEpisodeViewModel>.Error("Une erreur est survenue lors de la modification de l'épisode de soins");
        }
    }

    // Terminer un épisode de soins
    public async Task<OperationResult> CompleteCareEpisodeAsync(int id, CompleteCareEpisodeViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que l'épisode existe
            var episode = await _careEpisodeRepository.GetByIdAsync(id);
            if (episode == null)
            {
                return OperationResult.Error("Épisode de soins introuvable");
            }

            // Vérifier que l'épisode est actif
            if (episode.Status != "Active")
            {
                return OperationResult.Error("Impossible de terminer un épisode de soins qui n'est pas actif");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                Status = episode.Status,
                EpisodeEndDate = episode.EpisodeEndDate
            };

            // Mettre à jour l'épisode
            episode.Status = "Completed";
            episode.EpisodeEndDate = model.CompletionDate;
            episode.ModifiedBy = modifiedBy;
            episode.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _careEpisodeRepository.UpdateAsync(episode);

            // Audit
            var newValues = new
            {
                Status = episode.Status,
                EpisodeEndDate = episode.EpisodeEndDate
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "CareEpisode",
                id,
                oldValues,
                newValues,
                $"Clôture de l'épisode de soins #{id}"
            );

            // Log
            await _logger.LogInfoAsync("CareEpisodeService", "CareEpisodeCompleted",
                $"Épisode de soins #{id} terminé",
                modifiedBy,
                episode.HospitalCenterId,
                details: new { EpisodeId = id });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "CompleteCareEpisodeError",
                $"Erreur lors de la clôture de l'épisode de soins {id}",
                modifiedBy,
                details: new { EpisodeId = id, Model = model, Error = ex.Message });
            return OperationResult.Error("Une erreur est survenue lors de la clôture de l'épisode de soins");
        }
    }

    // Interrompre un épisode de soins
    public async Task<OperationResult> InterruptCareEpisodeAsync(int id, InterruptCareEpisodeViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que l'épisode existe
            var episode = await _careEpisodeRepository.GetByIdAsync(id);
            if (episode == null)
            {
                return OperationResult.Error("Épisode de soins introuvable");
            }

            // Vérifier que l'épisode est actif
            if (episode.Status != "Active")
            {
                return OperationResult.Error("Impossible d'interrompre un épisode de soins qui n'est pas actif");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                Status = episode.Status,
                EpisodeEndDate = episode.EpisodeEndDate,
                InterruptionReason = episode.InterruptionReason
            };

            // Mettre à jour l'épisode
            episode.Status = "Interrupted";
            episode.EpisodeEndDate = model.InterruptionDate;
            episode.InterruptionReason = model.InterruptionReason;
            episode.ModifiedBy = modifiedBy;
            episode.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _careEpisodeRepository.UpdateAsync(episode);

            // Audit
            var newValues = new
            {
                Status = episode.Status,
                EpisodeEndDate = episode.EpisodeEndDate,
                InterruptionReason = episode.InterruptionReason
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "CareEpisode",
                id,
                oldValues,
                newValues,
                $"Interruption de l'épisode de soins #{id}"
            );

            // Log
            await _logger.LogInfoAsync("CareEpisodeService", "CareEpisodeInterrupted",
                $"Épisode de soins #{id} interrompu: {model.InterruptionReason}",
                modifiedBy,
                episode.HospitalCenterId,
                details: new { EpisodeId = id, Reason = model.InterruptionReason });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "InterruptCareEpisodeError",
                $"Erreur lors de l'interruption de l'épisode de soins {id}",
                modifiedBy,
                details: new { EpisodeId = id, Model = model, Error = ex.Message });
            return OperationResult.Error("Une erreur est survenue lors de l'interruption de l'épisode de soins");
        }
    }

    // Récupérer les épisodes de soins d'un patient
    public async Task<List<CareEpisodeViewModel>> GetPatientCareEpisodesAsync(int patientId)
    {
        try
        {
            return await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(e => e.PatientId == patientId)
                 .Include(e => e.Diagnosis)
                 .Include(e => e.HospitalCenter)
                 .Include(e => e.PrimaryCaregiverNavigation)
                 .OrderByDescending(e => e.EpisodeStartDate)
                 .Select(e => new CareEpisodeViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     DiagnosisId = e.DiagnosisId,
                     DiagnosisName = e.Diagnosis.DiagnosisName,
                     HospitalCenterId = e.HospitalCenterId,
                     HospitalCenterName = e.HospitalCenter.Name,
                     PrimaryCaregiverId = e.PrimaryCaregiver,
                     PrimaryCaregiverName = $"{e.PrimaryCaregiverNavigation.FirstName} {e.PrimaryCaregiverNavigation.LastName}",
                     EpisodeStartDate = e.EpisodeStartDate,
                     EpisodeEndDate = e.EpisodeEndDate,
                     Status = e.Status,
                     InterruptionReason = e.InterruptionReason,
                     TotalCost = e.TotalCost,
                     AmountPaid = e.AmountPaid,
                     RemainingBalance = e.RemainingBalance
                 }));
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "GetPatientCareEpisodesError",
                $"Erreur lors de la récupération des épisodes de soins du patient {patientId}",
                details: new { PatientId = patientId, Error = ex.Message });
            throw;
        }
    }

    // Récupérer les services de soins d'un épisode
    public async Task<List<CareServiceViewModel>> GetCareServicesAsync(int episodeId)
    {
        try
        {
            return await _careServiceRepository.QueryListAsync(q =>
                q.Where(s => s.CareEpisodeId == episodeId)
                 .Include(s => s.CareType)
                 .Include(s => s.AdministeredByNavigation)
                 .Include(s => s.CareServiceProducts)
                    .ThenInclude(p => p.Product)
                 .OrderByDescending(s => s.ServiceDate)
                 .Select(s => new CareServiceViewModel
                 {
                     Id = s.Id,
                     CareEpisodeId = s.CareEpisodeId,
                     CareTypeId = s.CareTypeId,
                     CareTypeName = s.CareType.Name,
                     AdministeredById = s.AdministeredBy,
                     AdministeredByName = $"{s.AdministeredByNavigation.FirstName} {s.AdministeredByNavigation.LastName}",
                     ServiceDate = s.ServiceDate,
                     Duration = s.Duration,
                     Notes = s.Notes,
                     Cost = s.Cost,
                     UsedProducts = s.CareServiceProducts.Select(p => new CareServiceProductViewModel
                     {
                         Id = p.Id,
                         CareServiceId = p.CareServiceId,
                         ProductId = p.ProductId,
                         ProductName = p.Product.Name,
                         QuantityUsed = p.QuantityUsed,
                         UnitCost = p.UnitCost,
                         TotalCost = p.TotalCost
                     }).ToList()
                 }));
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "GetCareServicesError",
                $"Erreur lors de la récupération des services de soins de l'épisode {episodeId}",
                details: new { EpisodeId = episodeId, Error = ex.Message });
            throw;
        }
    }

    // Ajouter un service de soins
    public async Task<OperationResult<CareServiceViewModel>> AddCareServiceAsync(CreateCareServiceViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que l'épisode existe et est actif
            var episode = await _careEpisodeRepository.GetByIdAsync(model.CareEpisodeId);
            if (episode == null)
            {
                return OperationResult<CareServiceViewModel>.Error("Épisode de soins introuvable");
            }

            if (episode.Status != "Active")
            {
                return OperationResult<CareServiceViewModel>.Error("Impossible d'ajouter un service à un épisode terminé ou interrompu");
            }

            // Vérifier que le type de soin existe
            var careType = await _careTypeRepository.GetByIdAsync(model.CareTypeId);
            if (careType == null)
            {
                return OperationResult<CareServiceViewModel>.Error("Type de soin introuvable");
            }

            // Vérifier que le soignant existe
            var staff = await _userRepository.GetByIdAsync(model.AdministeredBy);
            if (staff == null)
            {
                return OperationResult<CareServiceViewModel>.Error("Soignant introuvable");
            }

            // Calcul du coût total (service + produits)
            decimal totalProductsCost = 0;
            foreach (var product in model.Products)
            {
                totalProductsCost += product.QuantityUsed * product.UnitCost;
            }

            decimal totalCost = model.Cost + totalProductsCost;

            // Créer le service de soins
            var careService = new CareService
            {
                CareEpisodeId = model.CareEpisodeId,
                CareTypeId = model.CareTypeId,
                AdministeredBy = model.AdministeredBy,
                ServiceDate = model.ServiceDate,
                Duration = model.Duration,
                Notes = model.Notes,
                Cost = totalCost,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdService = await _careServiceRepository.AddAsync(careService);

            // Mettre à jour l'épisode de soins (coût total)
            episode.TotalCost += totalCost;
            episode.RemainingBalance += totalCost;
            episode.ModifiedBy = createdBy;
            episode.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _careEpisodeRepository.UpdateAsync(episode);

            // Ajouter les produits utilisés (si présents)
            if (model.Products != null && model.Products.Any())
            {
                foreach (var productItem in model.Products)
                {
                    // Vérifier que le produit existe
                    var product = await _productRepository.GetByIdAsync(productItem.ProductId);
                    if (product == null)
                    {
                        continue; // Ignorer ce produit
                    }

                    // Vérifier le stock disponible
                    var stock = await _stockInventoryRepository.GetSingleAsync(q =>
                        q.Where(s => s.ProductId == productItem.ProductId && s.HospitalCenterId == episode.HospitalCenterId));

                    if (stock == null || stock.CurrentQuantity < productItem.QuantityUsed)
                    {
                        // Log l'erreur mais continuer
                        await _logger.LogWarningAsync("CareEpisodeService", "InsufficientStock",
                            $"Stock insuffisant pour le produit {product.Name} (ID: {product.Id})",
                            createdBy, episode.HospitalCenterId,
                            details: new { ProductId = product.Id, Required = productItem.QuantityUsed, Available = stock?.CurrentQuantity ?? 0 });

                        continue;
                    }

                    // Créer l'entrée de produit utilisé
                    var careServiceProduct = new CareServiceProduct
                    {
                        CareServiceId = createdService.Id,
                        ProductId = productItem.ProductId,
                        QuantityUsed = productItem.QuantityUsed,
                        UnitCost = product.SellingPrice,
                        TotalCost = productItem.QuantityUsed * product.SellingPrice,
                        CreatedBy = createdBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _careServiceProductRepository.AddAsync(careServiceProduct);

                    // Décrémenter le stock
                    stock.CurrentQuantity -= productItem.QuantityUsed;
                    stock.ModifiedBy = createdBy;
                    stock.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                    await _stockInventoryRepository.UpdateAsync(stock);

                    // Créer le mouvement de stock
                    var stockMovement = new StockMovement
                    {
                        ProductId = productItem.ProductId,
                        HospitalCenterId = episode.HospitalCenterId,
                        MovementType = "Care",
                        Quantity = -productItem.QuantityUsed, // Négatif car c'est une sortie
                        ReferenceType = "CareService",
                        ReferenceId = createdService.Id,
                        Notes = $"Utilisé pour le service de soins #{createdService.Id}",
                        MovementDate = TimeZoneHelper.GetCameroonTime(),
                        CreatedBy = createdBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _stockMovementRepository.AddAsync(stockMovement);
                }
            }

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "CareService",
                createdService.Id,
                null,
                new
                {
                    CareEpisodeId = model.CareEpisodeId,
                    CareTypeId = model.CareTypeId,
                    ServiceDate = model.ServiceDate,
                    Cost = totalCost,
                    ProductsCount = model.Products?.Count ?? 0
                },
                $"Ajout d'un service de soins à l'épisode #{model.CareEpisodeId}"
            );

            // Log
            await _logger.LogInfoAsync("CareEpisodeService", "CareServiceAdded",
                $"Service de soins ajouté à l'épisode #{model.CareEpisodeId}",
                createdBy,
                episode.HospitalCenterId,
                details: new { ServiceId = createdService.Id, EpisodeId = model.CareEpisodeId });

            // Récupérer le service créé
            var serviceViewModel = await _careServiceRepository.QuerySingleAsync<CareServiceViewModel>(q =>
                q.Where(s => s.Id == createdService.Id)
                 .Include(s => s.CareType)
                 .Include(s => s.AdministeredByNavigation)
                 .Include(s => s.CareServiceProducts)
                    .ThenInclude(p => p.Product)
                 .Select(s => new CareServiceViewModel
                 {
                     Id = s.Id,
                     CareEpisodeId = s.CareEpisodeId,
                     CareTypeId = s.CareTypeId,
                     CareTypeName = s.CareType.Name,
                     AdministeredById = s.AdministeredBy,
                     AdministeredByName = $"{s.AdministeredByNavigation.FirstName} {s.AdministeredByNavigation.LastName}",
                     ServiceDate = s.ServiceDate,
                     Duration = s.Duration,
                     Notes = s.Notes,
                     Cost = s.Cost,
                     UsedProducts = s.CareServiceProducts.Select(p => new CareServiceProductViewModel
                     {
                         Id = p.Id,
                         CareServiceId = p.CareServiceId,
                         ProductId = p.ProductId,
                         ProductName = p.Product.Name,
                         QuantityUsed = p.QuantityUsed,
                         UnitCost = p.UnitCost,
                         TotalCost = p.TotalCost
                     }).ToList()
                 }));

            return OperationResult<CareServiceViewModel>.Success(serviceViewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("CareEpisodeService", "AddCareServiceError",
                "Erreur lors de l'ajout du service de soins",
                createdBy,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<CareServiceViewModel>.Error("Une erreur est survenue lors de l'ajout du service de soins");
        }
    }
}