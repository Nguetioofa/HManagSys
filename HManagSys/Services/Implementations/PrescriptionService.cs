using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations;

public class PrescriptionService : IPrescriptionService
{
    private readonly IGenericRepository<Prescription> _prescriptionRepository;
    private readonly IGenericRepository<PrescriptionItem> _prescriptionItemRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Diagnosis> _diagnosisRepository;
    private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
    private readonly IGenericRepository<HospitalCenter> _centerRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IApplicationLogger _logger;
    private readonly IAuditService _auditService;

    public PrescriptionService(
        IGenericRepository<Prescription> prescriptionRepository,
        IGenericRepository<PrescriptionItem> prescriptionItemRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<Diagnosis> diagnosisRepository,
        IGenericRepository<CareEpisode> careEpisodeRepository,
        IGenericRepository<HospitalCenter> centerRepository,
        IGenericRepository<Product> productRepository,
        IApplicationLogger logger,
        IAuditService auditService)
    {
        _prescriptionRepository = prescriptionRepository;
        _prescriptionItemRepository = prescriptionItemRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _diagnosisRepository = diagnosisRepository;
        _careEpisodeRepository = careEpisodeRepository;
        _centerRepository = centerRepository;
        _productRepository = productRepository;
        _logger = logger;
        _auditService = auditService;
    }

    // Récupérer une prescription par ID
    public async Task<PrescriptionViewModel?> GetByIdAsync(int id)
    {
        try
        {
            var prescription = await _prescriptionRepository.QuerySingleAsync<PrescriptionViewModel>(q =>
                q.Where(p => p.Id == id)
                 .Include(p => p.Patient)
                 .Include(p => p.Diagnosis)
                 .Include(p => p.CareEpisode)
                 .Include(p => p.HospitalCenter)
                 .Include(p => p.PrescribedByNavigation)
                 .Include(p => p.PrescriptionItems)
                    .ThenInclude(i => i.Product)
                 .Select(p => new PrescriptionViewModel
                 {
                     Id = p.Id,
                     PatientId = p.PatientId,
                     PatientName = $"{p.Patient.FirstName} {p.Patient.LastName}",
                     DiagnosisId = p.DiagnosisId,
                     DiagnosisName = p.Diagnosis != null ? p.Diagnosis.DiagnosisName : null,
                     CareEpisodeId = p.CareEpisodeId,
                     HospitalCenterId = p.HospitalCenterId,
                     HospitalCenterName = p.HospitalCenter.Name,
                     PrescribedById = p.PrescribedBy,
                     PrescribedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     PrescriptionDate = p.PrescriptionDate,
                     Instructions = p.Instructions,
                     Status = p.Status,
                     Items = p.PrescriptionItems.Select(i => new PrescriptionItemViewModel
                     {
                         Id = i.Id,
                         PrescriptionId = i.PrescriptionId,
                         ProductId = i.ProductId,
                         ProductName = i.Product.Name,
                         Quantity = i.Quantity,
                         Dosage = i.Dosage,
                         Frequency = i.Frequency,
                         Duration = i.Duration,
                         Instructions = i.Instructions
                     }).ToList()
                 }));

            return prescription;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "GetByIdError",
                $"Erreur lors de la récupération de la prescription {id}",
                details: new { PrescriptionId = id, Error = ex.Message });
            throw;
        }
    }

    // Récupérer les prescriptions avec pagination et filtres
    public async Task<(List<PrescriptionViewModel> Items, int TotalCount)> GetPrescriptionsAsync(PrescriptionFilters filters)
    {
        try
        {
            int totalCount = 0;

           var query = await _prescriptionRepository.QueryListAsync<PrescriptionViewModel>(q =>
            {
                var baseQuery = q.Include(p => p.Patient)
                                .Include(p => p.Diagnosis)
                                .Include(p => p.HospitalCenter)
                                .Include(p => p.PrescribedByNavigation)
                                .Include(p => p.PrescriptionItems)
                                .AsQueryable();

                // Filtre par centre hospitalier
                if (filters.HospitalCenterId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.HospitalCenterId == filters.HospitalCenterId.Value);
                }

                // Filtre par patient
                if (filters.PatientId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.PatientId == filters.PatientId.Value);
                }

                // Filtre par prescripteur
                if (filters.PrescribedBy.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.PrescribedBy == filters.PrescribedBy.Value);
                }

                // Filtre par statut
                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    baseQuery = baseQuery.Where(p => p.Status == filters.Status);
                }

                // Filtre par période
                if (filters.FromDate.HasValue)
                {
                    var fromDate = filters.FromDate.Value.Date;
                    baseQuery = baseQuery.Where(p => p.PrescriptionDate >= fromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                    baseQuery = baseQuery.Where(p => p.PrescriptionDate <= toDate);
                }

                // Filtre par recherche
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    baseQuery = baseQuery.Where(p =>
                        p.Patient.FirstName.ToLower().Contains(searchTerm) ||
                        p.Patient.LastName.ToLower().Contains(searchTerm) ||
                        (p.Diagnosis != null && p.Diagnosis.DiagnosisName.ToLower().Contains(searchTerm)) ||
                        p.PrescribedByNavigation.FirstName.ToLower().Contains(searchTerm) ||
                        p.PrescribedByNavigation.LastName.ToLower().Contains(searchTerm) ||
                        p.PrescriptionItems.Any(i => i.Product.Name.ToLower().Contains(searchTerm)));
                }

                // Récupérer le nombre total
                 totalCount = baseQuery.Count();

                // Pagination
                var pagedQuery = baseQuery
                    .OrderByDescending(p => p.PrescriptionDate)
                    .Skip((filters.PageIndex - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(p => new PrescriptionViewModel
                    {
                        Id = p.Id,
                        PatientId = p.PatientId,
                        PatientName = $"{p.Patient.FirstName} {p.Patient.LastName}",
                        DiagnosisId = p.DiagnosisId,
                        DiagnosisName = p.Diagnosis != null ? p.Diagnosis.DiagnosisName : null,
                        CareEpisodeId = p.CareEpisodeId,
                        HospitalCenterId = p.HospitalCenterId,
                        HospitalCenterName = p.HospitalCenter.Name,
                        PrescribedById = p.PrescribedBy,
                        PrescribedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                        PrescriptionDate = p.PrescriptionDate,
                        Instructions = p.Instructions,
                        Status = p.Status,
                        Items = p.PrescriptionItems.Select(i => new PrescriptionItemViewModel
                        {
                            Id = i.Id,
                            PrescriptionId = i.PrescriptionId,
                            ProductId = i.ProductId,
                            ProductName = i.Product.Name,
                            Quantity = i.Quantity,
                            Dosage = i.Dosage,
                            Frequency = i.Frequency,
                            Duration = i.Duration,
                            Instructions = i.Instructions
                        }).ToList()
                    });

                return pagedQuery;
            });

            return (query,  totalCount);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "GetPrescriptionsError",
                "Erreur lors de la récupération des prescriptions",
                details: new { Filters = filters, Error = ex.Message });
            throw;
        }
    }

    // Créer une nouvelle prescription
    public async Task<OperationResult<PrescriptionViewModel>> CreatePrescriptionAsync(CreatePrescriptionViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que le patient existe
            var patient = await _patientRepository.GetByIdAsync(model.PatientId);
            if (patient == null)
            {
                return OperationResult<PrescriptionViewModel>.Error("Patient introuvable");
            }

            // Vérifier que le centre existe
            var center = await _centerRepository.GetByIdAsync(model.HospitalCenterId);
            if (center == null)
            {
                return OperationResult<PrescriptionViewModel>.Error("Centre hospitalier introuvable");
            }

            // Vérifier le diagnostic si spécifié
            if (model.DiagnosisId.HasValue)
            {
                var diagnosis = await _diagnosisRepository.GetByIdAsync(model.DiagnosisId.Value);
                if (diagnosis == null)
                {
                    return OperationResult<PrescriptionViewModel>.Error("Diagnostic introuvable");
                }

                if (diagnosis.PatientId != model.PatientId)
                {
                    return OperationResult<PrescriptionViewModel>.Error("Le diagnostic n'appartient pas à ce patient");
                }
            }

            // Vérifier l'épisode de soins si spécifié
            if (model.CareEpisodeId.HasValue)
            {
                var episode = await _careEpisodeRepository.GetByIdAsync(model.CareEpisodeId.Value);
                if (episode == null)
                {
                    return OperationResult<PrescriptionViewModel>.Error("Épisode de soins introuvable");
                }

                if (episode.PatientId != model.PatientId)
                {
                    return OperationResult<PrescriptionViewModel>.Error("L'épisode de soins n'appartient pas à ce patient");
                }
            }

            // Créer la prescription
            var prescription = new Prescription
            {
                PatientId = model.PatientId,
                DiagnosisId = model.DiagnosisId,
                CareEpisodeId = model.CareEpisodeId,
                HospitalCenterId = model.HospitalCenterId,
                PrescribedBy = createdBy,
                PrescriptionDate = model.PrescriptionDate,
                Instructions = model.Instructions,
                Status = "Pending",
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdPrescription = await _prescriptionRepository.AddAsync(prescription);

            // Ajouter les items de la prescription
            if (model.Items != null && model.Items.Any())
            {
                foreach (var item in model.Items)
                {
                    // Vérifier que le produit existe
                    var product = await _productRepository.GetByIdAsync(item.ProductId);
                    if (product == null)
                    {
                        continue; // Ignorer ce produit
                    }

                    var prescriptionItem = new PrescriptionItem
                    {
                        PrescriptionId = createdPrescription.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Dosage = item.Dosage,
                        Frequency = item.Frequency,
                        Duration = item.Duration,
                        Instructions = item.Instructions,
                        CreatedBy = createdBy,
                        CreatedAt = TimeZoneHelper.GetCameroonTime()
                    };

                    await _prescriptionItemRepository.AddAsync(prescriptionItem);
                }
            }

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "Prescription",
                createdPrescription.Id,
                null,
                new
                {
                    PatientId = model.PatientId,
                    PrescriptionDate = model.PrescriptionDate,
                    ItemsCount = model.Items?.Count ?? 0
                },
                $"Création d'une nouvelle prescription pour le patient {patient.FirstName} {patient.LastName}"
            );

            // Log
            await _logger.LogInfoAsync("PrescriptionService", "PrescriptionCreated",
                $"Nouvelle prescription créée pour le patient {patient.FirstName} {patient.LastName}",
                createdBy,
                model.HospitalCenterId,
                details: new { PrescriptionId = createdPrescription.Id, ItemsCount = model.Items?.Count ?? 0 });

            // Retourner la prescription créée
            var viewModel = await GetByIdAsync(createdPrescription.Id);
            return OperationResult<PrescriptionViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "CreatePrescriptionError",
                "Erreur lors de la création de la prescription",
                createdBy,
                model.HospitalCenterId,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<PrescriptionViewModel>.Error("Une erreur est survenue lors de la création de la prescription");
        }
    }

    // Méthodes à ajouter à PrescriptionService.cs

    // Mettre à jour une prescription existante
    public async Task<OperationResult<PrescriptionViewModel>> UpdatePrescriptionAsync(int id, EditPrescriptionViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que la prescription existe
            var prescription = await _prescriptionRepository.GetByIdAsync(id);
            if (prescription == null)
            {
                return OperationResult<PrescriptionViewModel>.Error("Prescription introuvable");
            }

            // Vérifier que la prescription n'est pas déjà dispensée
            if (prescription.Status == "Dispensed")
            {
                return OperationResult<PrescriptionViewModel>.Error("Impossible de modifier une prescription déjà dispensée");
            }

            // Vérifier le diagnostic si spécifié
            if (model.DiagnosisId.HasValue)
            {
                var diagnosis = await _diagnosisRepository.GetByIdAsync(model.DiagnosisId.Value);
                if (diagnosis == null)
                {
                    return OperationResult<PrescriptionViewModel>.Error("Diagnostic introuvable");
                }

                if (diagnosis.PatientId != prescription.PatientId)
                {
                    return OperationResult<PrescriptionViewModel>.Error("Le diagnostic n'appartient pas à ce patient");
                }
            }

            // Mettre à jour les propriétés de la prescription
            prescription.DiagnosisId = model.DiagnosisId;
            prescription.CareEpisodeId = model.CareEpisodeId;
            prescription.Instructions = model.Instructions;
            prescription.ModifiedBy = modifiedBy;
            prescription.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _prescriptionRepository.UpdateAsync(prescription);

            // Audit
            await _auditService.LogActionAsync(
                modifiedBy,
                "UPDATE",
                "Prescription",
                prescription.Id,
                new
                {
                    OldDiagnosisId = prescription.DiagnosisId,
                    OldCareEpisodeId = prescription.CareEpisodeId,
                    OldInstructions = prescription.Instructions
                },
                new
                {
                    NewDiagnosisId = model.DiagnosisId,
                    NewCareEpisodeId = model.CareEpisodeId,
                    NewInstructions = model.Instructions
                },
                $"Modification de la prescription {prescription.Id}"
            );

            // Log
            await _logger.LogInfoAsync("PrescriptionService", "PrescriptionUpdated",
                $"Prescription {prescription.Id} mise à jour",
                modifiedBy,
                prescription.HospitalCenterId,
                details: new { PrescriptionId = prescription.Id });

            // Retourner la prescription mise à jour
            var viewModel = await GetByIdAsync(prescription.Id);
            return OperationResult<PrescriptionViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "UpdatePrescriptionError",
                $"Erreur lors de la mise à jour de la prescription {id}",
                modifiedBy,
                details: new { PrescriptionId = id, Model = model, Error = ex.Message });
            return OperationResult<PrescriptionViewModel>.Error("Une erreur est survenue lors de la mise à jour de la prescription");
        }
    }

    // Marquer une prescription comme dispensée/annulée
    public async Task<OperationResult> DisposePrescriptionAsync(int id, int modifiedBy)
    {
        try
        {
            // Vérifier que la prescription existe
            var prescription = await _prescriptionRepository.GetByIdAsync(id);
            if (prescription == null)
            {
                return OperationResult.Error("Prescription introuvable");
            }

            // Vérifier que la prescription n'est pas déjà dispensée
            if (prescription.Status != "Pending")
            {
                return OperationResult.Error($"Impossible de disposer une prescription en statut '{prescription.Status}'");
            }

            // Mettre à jour le statut de la prescription
            prescription.Status = "Dispensed";
            prescription.ModifiedBy = modifiedBy;
            prescription.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _prescriptionRepository.UpdateAsync(prescription);

            // Audit
            await _auditService.LogActionAsync(
                modifiedBy,
                "DISPOSE",
                "Prescription",
                prescription.Id,
                new { OldStatus = "Pending" },
                new { NewStatus = "Dispensed" },
                $"Prescription {prescription.Id} dispensée"
            );

            // Log
            await _logger.LogInfoAsync("PrescriptionService", "PrescriptionDisposed",
                $"Prescription {prescription.Id} dispensée",
                modifiedBy,
                prescription.HospitalCenterId,
                details: new { PrescriptionId = prescription.Id });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "DisposePrescriptionError",
                $"Erreur lors de la disposition de la prescription {id}",
                modifiedBy,
                details: new { PrescriptionId = id, Error = ex.Message });
            return OperationResult.Error("Une erreur est survenue lors de la disposition de la prescription");
        }
    }

    // Récupérer les prescriptions d'un patient spécifique
    public async Task<List<PrescriptionViewModel>> GetPatientPrescriptionsAsync(int patientId)
    {
        try
        {
            var prescriptions = await _prescriptionRepository.QueryListAsync<PrescriptionViewModel>(q =>
                q.Where(p => p.PatientId == patientId)
                 .Include(p => p.Diagnosis)
                 .Include(p => p.CareEpisode)
                 .Include(p => p.HospitalCenter)
                 .Include(p => p.PrescribedByNavigation)
                 .Include(p => p.PrescriptionItems)
                    .ThenInclude(i => i.Product)
                 .OrderByDescending(p => p.PrescriptionDate)
                 .Select(p => new PrescriptionViewModel
                 {
                     Id = p.Id,
                     PatientId = p.PatientId,
                     DiagnosisId = p.DiagnosisId,
                     DiagnosisName = p.Diagnosis != null ? p.Diagnosis.DiagnosisName : null,
                     CareEpisodeId = p.CareEpisodeId,
                     HospitalCenterId = p.HospitalCenterId,
                     HospitalCenterName = p.HospitalCenter.Name,
                     PrescribedById = p.PrescribedBy,
                     PrescribedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     PrescriptionDate = p.PrescriptionDate,
                     Instructions = p.Instructions,
                     Status = p.Status,
                     Items = p.PrescriptionItems.Select(i => new PrescriptionItemViewModel
                     {
                         Id = i.Id,
                         PrescriptionId = i.PrescriptionId,
                         ProductId = i.ProductId,
                         ProductName = i.Product.Name,
                         Quantity = i.Quantity,
                         Dosage = i.Dosage,
                         Frequency = i.Frequency,
                         Duration = i.Duration,
                         Instructions = i.Instructions
                     }).ToList()
                 }));

            return prescriptions;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "GetPatientPrescriptionsError",
                $"Erreur lors de la récupération des prescriptions du patient {patientId}",
                details: new { PatientId = patientId, Error = ex.Message });
            throw;
        }
    }

    // Ajouter un produit à une prescription existante
    public async Task<OperationResult<PrescriptionItemViewModel>> AddPrescriptionItemAsync(int prescriptionId, CreatePrescriptionItemViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que la prescription existe
            var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
            if (prescription == null)
            {
                return OperationResult<PrescriptionItemViewModel>.Error("Prescription introuvable");
            }

            // Vérifier que la prescription n'est pas déjà dispensée
            if (prescription.Status == "Dispensed")
            {
                return OperationResult<PrescriptionItemViewModel>.Error("Impossible d'ajouter un produit à une prescription déjà dispensée");
            }

            // Vérifier que le produit existe
            var product = await _productRepository.GetByIdAsync(model.ProductId);
            if (product == null)
            {
                return OperationResult<PrescriptionItemViewModel>.Error("Produit introuvable");
            }

            // Créer le nouvel item
            var prescriptionItem = new PrescriptionItem
            {
                PrescriptionId = prescriptionId,
                ProductId = model.ProductId,
                Quantity = model.Quantity,
                Dosage = model.Dosage,
                Frequency = model.Frequency,
                Duration = model.Duration,
                Instructions = model.Instructions,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdItem = await _prescriptionItemRepository.AddAsync(prescriptionItem);

            // Mettre à jour la date de modification de la prescription
            prescription.ModifiedBy = createdBy;
            prescription.ModifiedAt = TimeZoneHelper.GetCameroonTime();
            await _prescriptionRepository.UpdateAsync(prescription);

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "ADD_ITEM",
                "PrescriptionItem",
                createdItem.Id,
                null,
                new
                {
                    PrescriptionId = prescriptionId,
                    ProductId = model.ProductId,
                    Quantity = model.Quantity
                },
                $"Ajout du produit {product.Name} à la prescription {prescriptionId}"
            );

            // Log
            await _logger.LogInfoAsync("PrescriptionService", "PrescriptionItemAdded",
                $"Produit ajouté à la prescription {prescriptionId}",
                createdBy,
                prescription.HospitalCenterId,
                details: new { PrescriptionId = prescriptionId, ProductId = model.ProductId });

            // Retourner l'item créé
            var viewModel = new PrescriptionItemViewModel
            {
                Id = createdItem.Id,
                PrescriptionId = createdItem.PrescriptionId,
                ProductId = createdItem.ProductId,
                ProductName = product.Name,
                Quantity = createdItem.Quantity,
                Dosage = createdItem.Dosage,
                Frequency = createdItem.Frequency,
                Duration = createdItem.Duration,
                Instructions = createdItem.Instructions
            };

            return OperationResult<PrescriptionItemViewModel>.Success(viewModel);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "AddPrescriptionItemError",
                $"Erreur lors de l'ajout d'un produit à la prescription {prescriptionId}",
                createdBy,
                details: new { PrescriptionId = prescriptionId, Model = model, Error = ex.Message });
            return OperationResult<PrescriptionItemViewModel>.Error("Une erreur est survenue lors de l'ajout du produit à la prescription");
        }
    }

    // Supprimer un produit d'une prescription
    public async Task<OperationResult> RemovePrescriptionItemAsync(int prescriptionItemId, int modifiedBy)
    {
        try
        {
            // Vérifier que l'item existe
            var item = await _prescriptionItemRepository.GetByIdAsync(prescriptionItemId);
            if (item == null)
            {
                return OperationResult.Error("Item de prescription introuvable");
            }

            // Vérifier que la prescription n'est pas déjà dispensée
            var prescription = await _prescriptionRepository.GetByIdAsync(item.PrescriptionId);
            if (prescription != null && prescription.Status == "Dispensed")
            {
                return OperationResult.Error("Impossible de supprimer un produit d'une prescription déjà dispensée");
            }

            // Récupérer les infos pour l'audit avant suppression
            var productId = item.ProductId;
            var prescriptionId = item.PrescriptionId;

            // Supprimer l'item
            await _prescriptionItemRepository.DeleteAsync(prescriptionItemId);

            // Mettre à jour la date de modification de la prescription
            if (prescription != null)
            {
                prescription.ModifiedBy = modifiedBy;
                prescription.ModifiedAt = TimeZoneHelper.GetCameroonTime();
                await _prescriptionRepository.UpdateAsync(prescription);
            }

            // Audit
            await _auditService.LogActionAsync(
                modifiedBy,
                "REMOVE_ITEM",
                "PrescriptionItem",
                prescriptionItemId,
                new
                {
                    PrescriptionId = prescriptionId,
                    ProductId = productId,
                    Quantity = item.Quantity
                },
                null,
                $"Suppression de l'item {prescriptionItemId} de la prescription {prescriptionId}"
            );

            // Log
            await _logger.LogInfoAsync("PrescriptionService", "PrescriptionItemRemoved",
                $"Produit supprimé de la prescription {prescriptionId}",
                modifiedBy,
                prescription?.HospitalCenterId,
                details: new { PrescriptionItemId = prescriptionItemId });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("PrescriptionService", "RemovePrescriptionItemError",
                $"Erreur lors de la suppression de l'item {prescriptionItemId}",
                modifiedBy,
                details: new { PrescriptionItemId = prescriptionItemId, Error = ex.Message });
            return OperationResult.Error("Une erreur est survenue lors de la suppression du produit de la prescription");
        }
    }

    // Mettre à jour une prescription
    //public async Task<OperationResult<PrescriptionViewModel>> UpdatePrescriptionAsync(int id, EditPrescriptionViewModel model, int modifiedBy)
    //{
    //    try
    //    {
    //        // Vérifier que la prescription existe
    //        var prescription = await _prescriptionRepository.GetByIdAsync(id);
    //        if (prescription == null)
    //        {
    //            return OperationResult<PrescriptionViewModel>.Error("Prescription introuvable");
    //        }

    //        // Vérifier que la prescription n'est pas déjà dispensée
    //        if (prescription.Status == "Dispensed")
    //        {
    //            return OperationResult<PrescriptionViewModel>.Error("Impossible de modifier une prescription déjà dispensée");
    //        }

    //        // Vérifier le diagnostic si spécifié
    //        if (model.DiagnosisId.HasValue)
    //        {
    //            var diagnosis = await _diagnosisRepository.GetByIdAsync(model.DiagnosisId.Value);
    //            if (diagnosis == null)
    //            {
    //                return OperationResult<PrescriptionViewModel>.Error("Diagnostic introuvable");
    //            }

    //            if (diagnosis.PatientI

}