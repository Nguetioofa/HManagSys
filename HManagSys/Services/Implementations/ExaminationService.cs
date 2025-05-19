using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations;

public class ExaminationService : IExaminationService
{
    private readonly IGenericRepository<Examination> _examinationRepository;
    private readonly IGenericRepository<ExaminationResult> _resultRepository;
    private readonly IGenericRepository<ExaminationType> _typeRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
    private readonly IGenericRepository<HospitalCenter> _centerRepository;
    private readonly IApplicationLogger _logger;
    private readonly IAuditService _auditService;
    //private readonly IFileStorageService _fileStorageService;

    public ExaminationService(
        IGenericRepository<Examination> examinationRepository,
        IGenericRepository<ExaminationResult> resultRepository,
        IGenericRepository<ExaminationType> typeRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<CareEpisode> careEpisodeRepository,
        IGenericRepository<HospitalCenter> centerRepository,
        IApplicationLogger logger,
        IAuditService auditService
        /*IFileStorageService fileStorageService*/)
    {
        _examinationRepository = examinationRepository;
        _resultRepository = resultRepository;
        _typeRepository = typeRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _careEpisodeRepository = careEpisodeRepository;
        _centerRepository = centerRepository;
        _logger = logger;
        _auditService = auditService;
        //_fileStorageService = fileStorageService;
    }

    // Récupérer un examen par ID
    public async Task<ExaminationViewModel?> GetByIdAsync(int id)
    {
        try
        {
            var examination = await _examinationRepository.QuerySingleAsync<ExaminationViewModel>(q =>
                q.Where(e => e.Id == id)
                 .Include(e => e.Patient)
                 .Include(e => e.ExaminationType)
                 .Include(e => e.HospitalCenter)
                 .Include(e => e.RequestedByNavigation)
                 .Include(e => e.PerformedByNavigation)
                 .Include(e => e.CareEpisode)
                 .Select(e => new ExaminationViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     ExaminationTypeId = e.ExaminationTypeId,
                     ExaminationTypeName = e.ExaminationType.Name,
                     CareEpisodeId = e.CareEpisodeId,
                     HospitalCenterId = e.HospitalCenterId,
                     HospitalCenterName = e.HospitalCenter.Name,
                     RequestedById = e.RequestedBy,
                     RequestedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     PerformedById = e.PerformedBy,
                     PerformedByName = e.PerformedBy.HasValue ? $"{e.PerformedByNavigation.FirstName} {e.PerformedByNavigation.LastName}" : null,
                     RequestDate = e.RequestDate,
                     ScheduledDate = e.ScheduledDate,
                     PerformedDate = e.PerformedDate,
                     Status = e.Status,
                     FinalPrice = e.FinalPrice,
                     DiscountAmount = e.DiscountAmount,
                     Notes = e.Notes
                 }));

            if (examination == null)
                return null;

            // Récupérer le résultat s'il existe
            examination.Result = await _resultRepository.QuerySingleAsync<ExaminationResultViewModel>(q =>
                q.Where(r => r.ExaminationId == id)
                 .Include(r => r.ReportedByNavigation)
                 .Select(r => new ExaminationResultViewModel
                 {
                     Id = r.Id,
                     ExaminationId = r.ExaminationId,
                     ResultData = r.ResultData,
                     ResultNotes = r.ResultNotes,
                     AttachmentPath = r.AttachmentPath,
                     ReportedById = r.ReportedBy,
                     ReportedByName = $"{r.ReportedByNavigation.FirstName} {r.ReportedByNavigation.LastName}",
                     ReportDate = r.ReportDate
                 }));

            return examination;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "GetByIdError",
                $"Erreur lors de la récupération de l'examen {id}",
                details: new { ExaminationId = id, Error = ex.Message });
            throw;
        }
    }


    public async Task<List<ExaminationViewModel>> GetByEpisodeAsync(int episodeId)
    {
        try
        {
            return await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.CareEpisodeId == episodeId)
                 .Include(e => e.Patient)
                 .Include(e => e.ExaminationType)
                 .Include(e => e.HospitalCenter)
                 .Include(e => e.RequestedByNavigation)
                 .Include(e => e.PerformedByNavigation)
                 .OrderByDescending(e => e.RequestDate)
                 .Select(e => new ExaminationViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     ExaminationTypeId = e.ExaminationTypeId,
                     ExaminationTypeName = e.ExaminationType.Name,
                     CareEpisodeId = e.CareEpisodeId,
                     HospitalCenterId = e.HospitalCenterId,
                     HospitalCenterName = e.HospitalCenter.Name,
                     RequestedById = e.RequestedBy,
                     RequestedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     PerformedById = e.PerformedBy,
                     PerformedByName = e.PerformedBy.HasValue ? $"{e.PerformedByNavigation.FirstName} {e.PerformedByNavigation.LastName}" : null,
                     RequestDate = e.RequestDate,
                     ScheduledDate = e.ScheduledDate,
                     PerformedDate = e.PerformedDate,
                     Status = e.Status,
                     FinalPrice = e.FinalPrice,
                     DiscountAmount = e.DiscountAmount,
                     Notes = e.Notes
                 }));

        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "GetExaminationsError",
                "Erreur lors de la récupération des examens",
                details: new { episodeId = episodeId, Error = ex.Message});
            throw;
        }
    }


    // Récupérer les examens avec pagination et filtres
    public async Task<(List<ExaminationViewModel> Items, int TotalCount)> GetExaminationsAsync(ExaminationFilters filters)
    {
        try
        {
            int TotalCount = 0;
            var query = await _examinationRepository.QueryListAsync<ExaminationViewModel>(q =>
            {
                var baseQuery = q.Include(e => e.Patient)
                                .Include(e => e.ExaminationType)
                                .Include(e => e.HospitalCenter)
                                .Include(e => e.RequestedByNavigation)
                                .Include(e => e.PerformedByNavigation)
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

                // Filtre par type d'examen
                if (filters.ExaminationTypeId.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.ExaminationTypeId == filters.ExaminationTypeId.Value);
                }

                // Filtre par demandeur
                if (filters.RequestedBy.HasValue)
                {
                    baseQuery = baseQuery.Where(e => e.RequestedBy == filters.RequestedBy.Value);
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
                    baseQuery = baseQuery.Where(e => e.RequestDate >= fromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDate = filters.ToDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                    baseQuery = baseQuery.Where(e => e.RequestDate <= toDate);
                }

                // Filtre par recherche
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    baseQuery = baseQuery.Where(e =>
                        e.Patient.FirstName.ToLower().Contains(searchTerm) ||
                        e.Patient.LastName.ToLower().Contains(searchTerm) ||
                        e.ExaminationType.Name.ToLower().Contains(searchTerm) ||
                        e.Status.ToLower().Contains(searchTerm));
                }

                // Récupérer le nombre total
                TotalCount = baseQuery.Count();

                // Pagination
                var pagedQuery = baseQuery
                    .OrderByDescending(e => e.RequestDate)
                    .Skip((filters.PageIndex - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(e => new ExaminationViewModel
                    {
                        Id = e.Id,
                        PatientId = e.PatientId,
                        PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                        ExaminationTypeId = e.ExaminationTypeId,
                        ExaminationTypeName = e.ExaminationType.Name,
                        CareEpisodeId = e.CareEpisodeId,
                        HospitalCenterId = e.HospitalCenterId,
                        HospitalCenterName = e.HospitalCenter.Name,
                        RequestedById = e.RequestedBy,
                        RequestedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                        PerformedById = e.PerformedBy,
                        PerformedByName = e.PerformedBy.HasValue ? $"{e.PerformedByNavigation.FirstName} {e.PerformedByNavigation.LastName}" : null,
                        RequestDate = e.RequestDate,
                        ScheduledDate = e.ScheduledDate,
                        PerformedDate = e.PerformedDate,
                        Status = e.Status,
                        FinalPrice = e.FinalPrice,
                        DiscountAmount = e.DiscountAmount,
                        Notes = e.Notes
                    });

                return pagedQuery;
            });

            return (query, TotalCount);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "GetExaminationsError",
                "Erreur lors de la récupération des examens",
                details: new { Filters = filters, Error = ex.Message });
            throw;
        }
    }

    // Créer un nouvel examen
    public async Task<OperationResult<ExaminationViewModel>> CreateExaminationAsync(CreateExaminationViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que le patient existe
            var patient = await _patientRepository.GetByIdAsync(model.PatientId);
            if (patient == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Patient introuvable");
            }

            // Vérifier que le type d'examen existe
            var examinationType = await _typeRepository.GetByIdAsync(model.ExaminationTypeId);
            if (examinationType == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Type d'examen introuvable");
            }

            // Vérifier que le centre existe
            var center = await _centerRepository.GetByIdAsync(model.HospitalCenterId);
            if (center == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Centre hospitalier introuvable");
            }

            // Vérifier l'épisode de soins si spécifié
            if (model.CareEpisodeId.HasValue)
            {
                var episode = await _careEpisodeRepository.GetByIdAsync(model.CareEpisodeId.Value);
                if (episode == null)
                {
                    return OperationResult<ExaminationViewModel>.Error("Épisode de soins introuvable");
                }

                if (episode.PatientId != model.PatientId)
                {
                    return OperationResult<ExaminationViewModel>.Error("L'épisode de soins n'appartient pas à ce patient");
                }
            }

            // Créer l'examen
            var examination = new Examination
            {
                PatientId = model.PatientId,
                ExaminationTypeId = model.ExaminationTypeId,
                CareEpisodeId = model.CareEpisodeId,
                HospitalCenterId = model.HospitalCenterId,
                RequestedBy = createdBy,
                RequestDate = model.RequestDate,
                ScheduledDate = model.ScheduledDate,
                Status = "Requested",
                FinalPrice = model.FinalPrice,
                DiscountAmount = model.DiscountAmount,
                Notes = model.Notes,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdExamination = await _examinationRepository.AddAsync(examination);

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "Examination",
                createdExamination.Id,
                null,
                new
                {
                    PatientId = model.PatientId,
                    ExaminationTypeId = model.ExaminationTypeId,
                    RequestDate = model.RequestDate
                },
                $"Création d'un nouvel examen pour le patient {patient.FirstName} {patient.LastName}"
            );

            // Log
            await _logger.LogInfoAsync("ExaminationService", "ExaminationCreated",
                $"Nouvel examen créé pour le patient {patient.FirstName} {patient.LastName}",
                createdBy,
                model.HospitalCenterId,
                details: new { ExaminationId = createdExamination.Id });

            // Retourner l'examen créé
            var viewModel = await GetByIdAsync(createdExamination.Id);
            return OperationResult<ExaminationViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "CreateExaminationError",
                "Erreur lors de la création de l'examen",
                createdBy,
                model.HospitalCenterId,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<ExaminationViewModel>.Error("Une erreur est survenue lors de la création de l'examen");
        }
    }

    // Planifier un examen
    public async Task<OperationResult<ExaminationViewModel>> ScheduleExaminationAsync(int id, ScheduleExaminationViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que l'examen existe
            var examination = await _examinationRepository.GetByIdAsync(id);
            if (examination == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Examen introuvable");
            }

            // Vérifier que l'examen est en statut "Requested"
            if (examination.Status != "Requested")
            {
                return OperationResult<ExaminationViewModel>.Error("Impossible de planifier un examen qui n'est pas en attente");
            }

            // Vérifier que l'exécutant existe
            var performer = await _userRepository.GetByIdAsync(model.PerformedBy);
            if (performer == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Exécutant introuvable");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                Status = examination.Status,
                ScheduledDate = examination.ScheduledDate,
                PerformedBy = examination.PerformedBy
            };

            // Mettre à jour l'examen
            examination.Status = "Scheduled";
            examination.ScheduledDate = model.ScheduledDate;
            examination.PerformedBy = model.PerformedBy;
            examination.Notes = string.IsNullOrEmpty(examination.Notes)
                ? model.Notes
                : $"{examination.Notes}\n{model.Notes}";
            examination.ModifiedBy = modifiedBy;
            examination.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _examinationRepository.UpdateAsync(examination);

            // Audit
            var newValues = new
            {
                Status = examination.Status,
                ScheduledDate = examination.ScheduledDate,
                PerformedBy = examination.PerformedBy
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "Examination",
                id,
                oldValues,
                newValues,
                $"Planification de l'examen #{id}"
            );

            // Log
            await _logger.LogInfoAsync("ExaminationService", "ExaminationScheduled",
                $"Examen #{id} planifié pour le {model.ScheduledDate:dd/MM/yyyy HH:mm}",
                modifiedBy,
                examination.HospitalCenterId,
                details: new { ExaminationId = id, ScheduledDate = model.ScheduledDate, PerformedBy = model.PerformedBy });

            // Retourner l'examen mis à jour
            var viewModel = await GetByIdAsync(id);
            return OperationResult<ExaminationViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "ScheduleExaminationError",
                $"Erreur lors de la planification de l'examen {id}",
                modifiedBy,
                details: new { ExaminationId = id, Model = model, Error = ex.Message });
            return OperationResult<ExaminationViewModel>.Error("Une erreur est survenue lors de la planification de l'examen");
        }
    }

    // Terminer un examen
    public async Task<OperationResult<ExaminationViewModel>> CompleteExaminationAsync(int id, CompleteExaminationViewModel model, int modifiedBy)
    {
        try
        {
            // Vérifier que l'examen existe
            var examination = await _examinationRepository.GetByIdAsync(id);
            if (examination == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Examen introuvable");
            }

            // Vérifier que l'examen est en statut "Scheduled"
            if (examination.Status != "Scheduled")
            {
                return OperationResult<ExaminationViewModel>.Error("Impossible de terminer un examen qui n'est pas planifié");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                Status = examination.Status,
                PerformedDate = examination.PerformedDate
            };

            // Mettre à jour l'examen
            examination.Status = "Completed";
            examination.PerformedDate = model.PerformedDate;
            examination.ModifiedBy = modifiedBy;
            examination.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _examinationRepository.UpdateAsync(examination);

            // Audit
            var newValues = new
            {
                Status = examination.Status,
                PerformedDate = examination.PerformedDate
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "Examination",
                id,
                oldValues,
                newValues,
                $"Complétion de l'examen #{id}"
            );

            // Log
            await _logger.LogInfoAsync("ExaminationService", "ExaminationCompleted",
                $"Examen #{id} terminé",
                modifiedBy,
                examination.HospitalCenterId,
                details: new { ExaminationId = id, PerformedDate = model.PerformedDate });

            // Retourner l'examen mis à jour
            var viewModel = await GetByIdAsync(id);
            return OperationResult<ExaminationViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "CompleteExaminationError",
                $"Erreur lors de la complétion de l'examen {id}",
                modifiedBy,
                details: new { ExaminationId = id, Model = model, Error = ex.Message });
            return OperationResult<ExaminationViewModel>.Error("Une erreur est survenue lors de la complétion de l'examen");
        }
    }

    // Annuler un examen
    public async Task<OperationResult<ExaminationViewModel>> CancelExaminationAsync(int id, string reason, int modifiedBy)
    {
        try
        {
            // Vérifier que l'examen existe
            var examination = await _examinationRepository.GetByIdAsync(id);
            if (examination == null)
            {
                return OperationResult<ExaminationViewModel>.Error("Examen introuvable");
            }

            // Vérifier que l'examen n'est pas déjà terminé ou annulé
            if (examination.Status == "Completed" || examination.Status == "Cancelled")
            {
                return OperationResult<ExaminationViewModel>.Error("Impossible d'annuler un examen déjà terminé ou annulé");
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                Status = examination.Status,
                Notes = examination.Notes
            };

            // Mettre à jour l'examen
            examination.Status = "Cancelled";
            examination.Notes = string.IsNullOrEmpty(examination.Notes)
                ? $"Annulé: {reason}"
                : $"{examination.Notes}\nAnnulé: {reason}";
            examination.ModifiedBy = modifiedBy;
            examination.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _examinationRepository.UpdateAsync(examination);

            // Audit
            var newValues = new
            {
                Status = examination.Status,
                Notes = examination.Notes
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "Examination",
                id,
                oldValues,
                newValues,
                $"Annulation de l'examen #{id}: {reason}"
            );

            // Log
            await _logger.LogInfoAsync("ExaminationService", "ExaminationCancelled",
                $"Examen #{id} annulé: {reason}",
                modifiedBy,
                examination.HospitalCenterId,
                details: new { ExaminationId = id, Reason = reason });

            // Retourner l'examen mis à jour
            var viewModel = await GetByIdAsync(id);
            return OperationResult<ExaminationViewModel>.Success(viewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "CancelExaminationError",
                $"Erreur lors de l'annulation de l'examen {id}",
                modifiedBy,
                details: new { ExaminationId = id, Reason = reason, Error = ex.Message });
            return OperationResult<ExaminationViewModel>.Error("Une erreur est survenue lors de l'annulation de l'examen");
        }
    }

    // Ajouter un résultat d'examen
    public async Task<OperationResult<ExaminationResultViewModel>> AddExaminationResultAsync(int examinationId, CreateExaminationResultViewModel model, int createdBy)
    {
        try
        {
            // Vérifier que l'examen existe
            var examination = await _examinationRepository.GetByIdAsync(examinationId);
            if (examination == null)
            {
                return OperationResult<ExaminationResultViewModel>.Error("Examen introuvable");
            }

            // Vérifier que l'examen est en statut "Scheduled" ou "Completed"
            if (examination.Status != "Scheduled" && examination.Status != "Completed")
            {
                return OperationResult<ExaminationResultViewModel>.Error("Impossible d'ajouter un résultat à un examen non planifié ou non terminé");
            }

            // Vérifier qu'il n'y a pas déjà un résultat
            var existingResult = await _resultRepository.AnyAsync(q => q.Where(r => r.ExaminationId == examinationId));
            if (existingResult)
            {
                return OperationResult<ExaminationResultViewModel>.Error("Un résultat existe déjà pour cet examen");
            }

            string? attachmentPath = null;

            // Traiter la pièce jointe si présente
            //if (model.Attachment != null && model.Attachment.Length > 0)
            //{
            //    // Stocker le fichier
            //    attachmentPath = await _fileStorageService.SaveExaminationAttachmentAsync(
            //        model.Attachment,
            //        examinationId,
            //        examination.PatientId);
            //}

            // Créer le résultat
            var result = new ExaminationResult
            {
                ExaminationId = examinationId,
                ResultData = model.ResultData,
                ResultNotes = model.ResultNotes,
                AttachmentPath = attachmentPath,
                ReportedBy = createdBy,
                ReportDate = model.ReportDate,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdResult = await _resultRepository.AddAsync(result);

            // Si l'examen n'est pas encore terminé, le marquer comme terminé
            if (examination.Status != "Completed")
            {
                // Sauvegarder les anciennes valeurs pour l'audit
                var oldValues = new
                {
                    Status = examination.Status,
                    PerformedDate = examination.PerformedDate
                };

                examination.Status = "Completed";
                examination.PerformedDate = model.ReportDate;
                examination.ModifiedBy = createdBy;
                examination.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _examinationRepository.UpdateAsync(examination);

                // Audit du changement de statut
                var newValues = new
                {
                    Status = examination.Status,
                    PerformedDate = examination.PerformedDate
                };

                await _auditService.LogActionAsync(
                    createdBy,
                    "STATUS_CHANGE",
                    "Examination",
                    examinationId,
                    oldValues,
                    newValues,
                    $"Examen #{examinationId} marqué comme terminé suite à l'ajout d'un résultat"
                );
            }

            // Audit de l'ajout du résultat
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "ExaminationResult",
                createdResult.Id,
                null,
                new
                {
                    ExaminationId = examinationId,
                    ReportDate = model.ReportDate,
                    HasAttachment = !string.IsNullOrEmpty(attachmentPath)
                },
                $"Ajout d'un résultat pour l'examen #{examinationId}"
            );

            // Log
            await _logger.LogInfoAsync("ExaminationService", "ExaminationResultAdded",
                $"Résultat ajouté pour l'examen #{examinationId}",
                createdBy,
                examination.HospitalCenterId,
                details: new { ExaminationId = examinationId, ResultId = createdResult.Id });

            // Récupérer le résultat créé
            var resultViewModel = await _resultRepository.QuerySingleAsync<ExaminationResultViewModel>(q =>
                q.Where(r => r.Id == createdResult.Id)
                 .Include(r => r.ReportedByNavigation)
                 .Select(r => new ExaminationResultViewModel
                 {
                     Id = r.Id,
                     ExaminationId = r.ExaminationId,
                     ResultData = r.ResultData,
                     ResultNotes = r.ResultNotes,
                     AttachmentPath = r.AttachmentPath,
                     ReportedById = r.ReportedBy,
                     ReportedByName = $"{r.ReportedByNavigation.FirstName} {r.ReportedByNavigation.LastName}",
                     ReportDate = r.ReportDate
                 }));

            return OperationResult<ExaminationResultViewModel>.Success(resultViewModel!);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "AddExaminationResultError",
                $"Erreur lors de l'ajout du résultat pour l'examen {examinationId}",
                createdBy,
                details: new { ExaminationId = examinationId, Error = ex.Message });
            return OperationResult<ExaminationResultViewModel>.Error("Une erreur est survenue lors de l'ajout du résultat");
        }
    }

    // Récupérer les examens d'un patient
    public async Task<List<ExaminationViewModel>> GetPatientExaminationsAsync(int patientId)
    {
        try
        {
            return await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.PatientId == patientId)
                 .Include(e => e.ExaminationType)
                 .Include(e => e.HospitalCenter)
                 .Include(e => e.RequestedByNavigation)
                 .Include(e => e.PerformedByNavigation)
                 .OrderByDescending(e => e.RequestDate)
                 .Select(e => new ExaminationViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     ExaminationTypeId = e.ExaminationTypeId,
                     ExaminationTypeName = e.ExaminationType.Name,
                     CareEpisodeId = e.CareEpisodeId,
                     HospitalCenterId = e.HospitalCenterId,
                     HospitalCenterName = e.HospitalCenter.Name,
                     RequestedById = e.RequestedBy,
                     RequestedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     PerformedById = e.PerformedBy,
                     PerformedByName = e.PerformedBy.HasValue ? $"{e.PerformedByNavigation.FirstName} {e.PerformedByNavigation.LastName}" : null,
                     RequestDate = e.RequestDate,
                     ScheduledDate = e.ScheduledDate,
                     PerformedDate = e.PerformedDate,
                     Status = e.Status,
                     FinalPrice = e.FinalPrice,
                     DiscountAmount = e.DiscountAmount,
                     Notes = e.Notes,
                     // On ne charge pas le résultat ici pour des raisons de performance
                     //HasResult = e.ExaminationResult != null
                 }));
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "GetPatientExaminationsError",
                $"Erreur lors de la récupération des examens du patient {patientId}",
                details: new { PatientId = patientId, Error = ex.Message });
            throw;
        }
    }

    // Récupérer un résultat d'examen
    public async Task<ExaminationResultViewModel?> GetExaminationResultAsync(int examinationId)
    {
        try
        {
            return await _resultRepository.QuerySingleAsync<ExaminationResultViewModel>(q =>
                q.Where(r => r.ExaminationId == examinationId)
                 .Include(r => r.ReportedByNavigation)
                 .Select(r => new ExaminationResultViewModel
                 {
                     Id = r.Id,
                     ExaminationId = r.ExaminationId,
                     ResultData = r.ResultData,
                     ResultNotes = r.ResultNotes,
                     AttachmentPath = r.AttachmentPath,
                     ReportedById = r.ReportedBy,
                     ReportedByName = $"{r.ReportedByNavigation.FirstName} {r.ReportedByNavigation.LastName}",
                     ReportDate = r.ReportDate
                 }));
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("ExaminationService", "GetExaminationResultError",
                $"Erreur lors de la récupération du résultat pour l'examen {examinationId}",
                details: new { ExaminationId = examinationId, Error = ex.Message });
            throw;
        }
    }
}