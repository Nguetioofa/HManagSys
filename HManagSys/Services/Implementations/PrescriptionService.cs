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

    public Task<OperationResult<PrescriptionViewModel>> UpdatePrescriptionAsync(int id, EditPrescriptionViewModel model, int modifiedBy)
    {
        throw new NotImplementedException();
    }

    public Task<OperationResult> DisposePrescriptionAsync(int id, int modifiedBy)
    {
        throw new NotImplementedException();
    }

    public Task<List<PrescriptionViewModel>> GetPatientPrescriptionsAsync(int patientId)
    {
        throw new NotImplementedException();
    }

    public Task<OperationResult<PrescriptionItemViewModel>> AddPrescriptionItemAsync(int prescriptionId, CreatePrescriptionItemViewModel model, int createdBy)
    {
        throw new NotImplementedException();
    }

    public Task<OperationResult> RemovePrescriptionItemAsync(int prescriptionItemId, int modifiedBy)
    {
        throw new NotImplementedException();
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