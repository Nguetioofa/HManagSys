using AutoMapper;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations;

/// <summary>
/// Service pour la gestion des patients
/// </summary>
public class PatientService : IPatientService
{
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<Diagnosis> _diagnosisRepository;
    private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
    private readonly IGenericRepository<Examination> _examinationRepository;
    private readonly IGenericRepository<Prescription> _prescriptionRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<HospitalCenter> _centerRepository;
    private readonly IApplicationLogger _appLogger;
    private readonly IAuditService _auditService;

    public PatientService(
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Diagnosis> diagnosisRepository,
        IGenericRepository<CareEpisode> careEpisodeRepository,
        IGenericRepository<Examination> examinationRepository,
        IGenericRepository<Prescription> prescriptionRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<HospitalCenter> centerRepository,
        IApplicationLogger appLogger,
        IAuditService auditService)
    {
        _patientRepository = patientRepository;
        _diagnosisRepository = diagnosisRepository;
        _careEpisodeRepository = careEpisodeRepository;
        _examinationRepository = examinationRepository;
        _prescriptionRepository = prescriptionRepository;
        _userRepository = userRepository;
        _centerRepository = centerRepository;
        _appLogger = appLogger;
        _auditService = auditService;
    }

    // Implémentation des méthodes pour les patients
    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        return await _patientRepository.GetByIdAsync(id);
    }

    public async Task<PatientDetailsViewModel?> GetPatientDetailsAsync(int id)
    {
        try
        {
            // Récupérer le patient avec toutes ses relations
            var patientQuery = await _patientRepository.QuerySingleAsync<PatientDetailsViewModel>(q =>
                q.Where(p => p.Id == id)
                 .Include(p => p.Diagnoses)
                 .Include(p => p.CareEpisodes)
                    .ThenInclude(ce => ce.HospitalCenter)
                 .Include(p => p.CareEpisodes)
                    .ThenInclude(ce => ce.PrimaryCaregiverNavigation)
                 .Include(p => p.Examinations)
                    .ThenInclude(e => e.ExaminationType)
                 .Include(p => p.Prescriptions)
                    .ThenInclude(pr => pr.PrescribedByNavigation)
                 .Select(p => new PatientDetailsViewModel
                 {
                     Id = p.Id,
                     FirstName = p.FirstName,
                     LastName = p.LastName,
                     DateOfBirth = p.DateOfBirth,
                     Gender = p.Gender,
                     PhoneNumber = p.PhoneNumber,
                     Email = p.Email,
                     Address = p.Address,
                     EmergencyContactName = p.EmergencyContactName,
                     EmergencyContactPhone = p.EmergencyContactPhone,
                     BloodType = p.BloodType,
                     Allergies = p.Allergies,
                     IsActive = p.IsActive,
                     CreatedAt = p.CreatedAt,
                     ModifiedAt = p.ModifiedAt,

                     // Calculer les relations et statistiques
                     TotalSpent = p.Payments.Sum(pm => pm.Amount),

                     // Les collections seront remplies séparément
                 })
            );

            if (patientQuery == null)
                return null;

            // Récupérer les diagnostics
            patientQuery.Diagnoses = await GetPatientDiagnosesAsync(id);

            // Récupérer les épisodes de soins
            patientQuery.CareEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.PatientId == id)
                 .OrderByDescending(ce => ce.EpisodeStartDate)
                 .Select(ce => new CareEpisodeViewModel
                 {
                     Id = ce.Id,
                     PatientId = ce.PatientId,
                     DiagnosisId = ce.DiagnosisId,
                     DiagnosisName = ce.Diagnosis.DiagnosisName,
                     HospitalCenterId = ce.HospitalCenterId,
                     HospitalCenterName = ce.HospitalCenter.Name,
                     PrimaryCaregiverId = ce.PrimaryCaregiver,
                     PrimaryCaregiverName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                     EpisodeStartDate = ce.EpisodeStartDate,
                     EpisodeEndDate = ce.EpisodeEndDate,
                     Status = ce.Status,
                     TotalCost = ce.TotalCost,
                     AmountPaid = ce.AmountPaid,
                     RemainingBalance = ce.RemainingBalance
                 })
            );

            // Récupérer les examens
            patientQuery.Examinations = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.PatientId == id)
                 .OrderByDescending(e => e.RequestDate)
                 .Select(e => new ExaminationViewModel
                 {
                     Id = e.Id,
                     PatientId = e.PatientId,
                     ExaminationTypeId = e.ExaminationTypeId,
                     ExaminationTypeName = e.ExaminationType.Name,
                     RequestDate = e.RequestDate,
                     PerformedDate = e.PerformedDate,
                     Status = e.Status,
                     FinalPrice = e.FinalPrice
                 })
            );

            // Récupérer les prescriptions
            patientQuery.Prescriptions = await _prescriptionRepository.QueryListAsync(q =>
                q.Where(p => p.PatientId == id)
                 .OrderByDescending(p => p.PrescriptionDate)
                 .Select(p => new PrescriptionViewModel
                 {
                     Id = p.Id,
                     PatientId = p.PatientId,
                     PrescribedById = p.PrescribedBy,
                     PrescribedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     PrescriptionDate = p.PrescriptionDate,
                     Status = p.Status,
                     Instructions = p.Instructions
                 })
            );

            return patientQuery;
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "GetPatientDetailsError",
                $"Erreur lors de la récupération des détails du patient {id}",
                details: new { PatientId = id, Error = ex.Message });
            throw;
        }
    }

    public async Task<(List<PatientViewModel> Patients, int TotalCount)> SearchPatientsAsync(PatientFilters filters)
    {
        try
        {
            int TotalCount = 0;
            // Construire la requête avec filtres
            var query = await _patientRepository.QueryListAsync<PatientViewModel>(q =>
            {
                var baseQuery = q.AsQueryable();

                // Filtre par centre hospitalier
                if (filters.HospitalCenterId.HasValue)
                {
                    // On filtre indirectement par les épisodes de soins ou diagnostics
                    // Cette partie sera adaptée selon la logique métier réelle
                }

                // Filtre par recherche
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchLower = filters.SearchTerm.ToLower();
                    baseQuery = baseQuery.Where(p =>
                        p.FirstName.ToLower().Contains(searchLower) ||
                        p.LastName.ToLower().Contains(searchLower) ||
                        p.PhoneNumber.Contains(filters.SearchTerm) ||
                        (p.Email != null && p.Email.ToLower().Contains(searchLower)));
                }

                // Filtre par statut
                if (filters.IsActive.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.IsActive == filters.IsActive.Value);
                }

                // Filtre par groupe sanguin
                if (!string.IsNullOrWhiteSpace(filters.BloodType))
                {
                    baseQuery = baseQuery.Where(p => p.BloodType == filters.BloodType);
                }

                // Filtre par genre
                if (!string.IsNullOrWhiteSpace(filters.Gender))
                {
                    baseQuery = baseQuery.Where(p => p.Gender == filters.Gender);
                }

                // Compter le total avant pagination
                TotalCount = baseQuery.Count();

                // Pagination et tri
                var patients = baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((filters.PageIndex - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(p => new PatientViewModel
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        DateOfBirth = p.DateOfBirth,
                        Gender = p.Gender,
                        PhoneNumber = p.PhoneNumber,
                        Email = p.Email,
                        BloodType = p.BloodType,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        DiagnosisCount = p.Diagnoses.Count,
                        CareEpisodeCount = p.CareEpisodes.Count,
                        LastVisitDate = p.CareEpisodes.Any()
                            ? p.CareEpisodes.Max(ce => ce.EpisodeStartDate)
                            : null
                    });

                return patients;
            });

 

            return (query, TotalCount);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "SearchPatientsError",
                "Erreur lors de la recherche des patients",
                details: new { Filters = filters, Error = ex.Message });
            throw;
        }
    }

    public async Task<OperationResult<PatientViewModel>> CreatePatientAsync(CreatePatientViewModel model, int createdBy)
    {
        try
        {
            // Validation
            var validation = await ValidatePatientAsync(model);
            if (!validation.IsValid)
            {
                return OperationResult<PatientViewModel>.ValidationError(validation.Errors);
            }

            // Créer l'entité
            var patient = new Patient
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                PhoneNumber = model.PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLower(),
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                EmergencyContactName = string.IsNullOrWhiteSpace(model.EmergencyContactName) ? null : model.EmergencyContactName.Trim(),
                EmergencyContactPhone = string.IsNullOrWhiteSpace(model.EmergencyContactPhone) ? null : model.EmergencyContactPhone.Trim(),
                BloodType = model.BloodType,
                Allergies = string.IsNullOrWhiteSpace(model.Allergies) ? null : model.Allergies.Trim(),
                IsActive = model.IsActive,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdPatient = await _patientRepository.AddAsync(patient);

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "Patient",
                createdPatient.Id,
                null,
                new { Name = $"{patient.FirstName} {patient.LastName}", IsActive = patient.IsActive },
                $"Création du patient '{patient.FirstName} {patient.LastName}'"
            );

            // Log applicatif
            await _appLogger.LogInfoAsync("PatientService", "PatientCreated",
                $"Patient créé : {patient.FirstName} {patient.LastName}",
                createdBy,
                details: new { PatientId = createdPatient.Id });

            // Retourner le ViewModel
            var patientViewModel = new PatientViewModel
            {
                Id = createdPatient.Id,
                FirstName = createdPatient.FirstName,
                LastName = createdPatient.LastName,
                DateOfBirth = createdPatient.DateOfBirth,
                Gender = createdPatient.Gender,
                PhoneNumber = createdPatient.PhoneNumber,
                Email = createdPatient.Email,
                BloodType = createdPatient.BloodType,
                IsActive = createdPatient.IsActive,
                CreatedAt = createdPatient.CreatedAt,
                DiagnosisCount = 0,
                CareEpisodeCount = 0
            };

            return OperationResult<PatientViewModel>.Success(patientViewModel);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "CreatePatientError",
                "Erreur lors de la création du patient",
                createdBy,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<PatientViewModel>.Error($"Erreur lors de la création : {ex.Message}");
        }
    }

    public async Task<OperationResult<PatientViewModel>> UpdatePatientAsync(int id, EditPatientViewModel model, int modifiedBy)
    {
        try
        {
            var existingPatient = await _patientRepository.GetByIdAsync(id);
            if (existingPatient == null)
            {
                return OperationResult<PatientViewModel>.Error("Patient introuvable");
            }

            // Validation
            var validation = await ValidatePatientAsync(model, id);
            if (!validation.IsValid)
            {
                return OperationResult<PatientViewModel>.ValidationError(validation.Errors);
            }

            // Sauvegarder les anciennes valeurs pour l'audit
            var oldValues = new
            {
                FirstName = existingPatient.FirstName,
                LastName = existingPatient.LastName,
                DateOfBirth = existingPatient.DateOfBirth,
                Gender = existingPatient.Gender,
                PhoneNumber = existingPatient.PhoneNumber,
                Email = existingPatient.Email,
                Address = existingPatient.Address,
                EmergencyContactName = existingPatient.EmergencyContactName,
                EmergencyContactPhone = existingPatient.EmergencyContactPhone,
                BloodType = existingPatient.BloodType,
                Allergies = existingPatient.Allergies,
                IsActive = existingPatient.IsActive
            };

            // Mettre à jour
            existingPatient.FirstName = model.FirstName.Trim();
            existingPatient.LastName = model.LastName.Trim();
            existingPatient.DateOfBirth = model.DateOfBirth;
            existingPatient.Gender = model.Gender;
            existingPatient.PhoneNumber = model.PhoneNumber.Trim();
            existingPatient.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLower();
            existingPatient.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            existingPatient.EmergencyContactName = string.IsNullOrWhiteSpace(model.EmergencyContactName) ? null : model.EmergencyContactName.Trim();
            existingPatient.EmergencyContactPhone = string.IsNullOrWhiteSpace(model.EmergencyContactPhone) ? null : model.EmergencyContactPhone.Trim();
            existingPatient.BloodType = model.BloodType;
            existingPatient.Allergies = string.IsNullOrWhiteSpace(model.Allergies) ? null : model.Allergies.Trim();
            existingPatient.IsActive = model.IsActive;
            existingPatient.ModifiedBy = modifiedBy;
            existingPatient.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _patientRepository.UpdateAsync(existingPatient);

            // Audit
            var newValues = new
            {
                FirstName = existingPatient.FirstName,
                LastName = existingPatient.LastName,
                DateOfBirth = existingPatient.DateOfBirth,
                Gender = existingPatient.Gender,
                PhoneNumber = existingPatient.PhoneNumber,
                Email = existingPatient.Email,
                Address = existingPatient.Address,
                EmergencyContactName = existingPatient.EmergencyContactName,
                EmergencyContactPhone = existingPatient.EmergencyContactPhone,
                BloodType = existingPatient.BloodType,
                Allergies = existingPatient.Allergies,
                IsActive = existingPatient.IsActive
            };

            await _auditService.LogActionAsync(
                modifiedBy,
                "UPDATE",
                "Patient",
                id,
                oldValues,
                newValues,
                $"Modification du patient '{existingPatient.FirstName} {existingPatient.LastName}'"
            );

            // Log applicatif
            await _appLogger.LogInfoAsync("PatientService", "PatientUpdated",
                $"Patient modifié : {existingPatient.FirstName} {existingPatient.LastName}",
                modifiedBy,
                details: new { PatientId = id });

            // Retourner le ViewModel
            var patientViewModel = new PatientViewModel
            {
                Id = existingPatient.Id,
                FirstName = existingPatient.FirstName,
                LastName = existingPatient.LastName,
                DateOfBirth = existingPatient.DateOfBirth,
                Gender = existingPatient.Gender,
                PhoneNumber = existingPatient.PhoneNumber,
                Email = existingPatient.Email,
                BloodType = existingPatient.BloodType,
                IsActive = existingPatient.IsActive,
                CreatedAt = existingPatient.CreatedAt,
                // Ces valeurs seraient normalement récupérées depuis la base de données
                DiagnosisCount = 0,
                CareEpisodeCount = 0
            };

            return OperationResult<PatientViewModel>.Success(patientViewModel);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "UpdatePatientError",
                $"Erreur lors de la modification du patient {id}",
                modifiedBy,
                details: new { PatientId = id, Model = model, Error = ex.Message });
            return OperationResult<PatientViewModel>.Error($"Erreur lors de la modification : {ex.Message}");
        }
    }

    public async Task<OperationResult> TogglePatientStatusAsync(int id, bool isActive, int modifiedBy)
    {
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                return OperationResult.Error("Patient introuvable");
            }

            var oldValue = patient.IsActive;
            patient.IsActive = isActive;
            patient.ModifiedBy = modifiedBy;
            patient.ModifiedAt = TimeZoneHelper.GetCameroonTime();

            await _patientRepository.UpdateAsync(patient);

            // Audit
            await _auditService.LogActionAsync(
                modifiedBy,
                "STATUS_CHANGE",
                "Patient",
                id,
                new { IsActive = oldValue },
                new { IsActive = isActive },
                $"Changement de statut du patient '{patient.FirstName} {patient.LastName}' : {(isActive ? "activé" : "désactivé")}"
            );

            // Log applicatif
            await _appLogger.LogInfoAsync("PatientService", "PatientStatusChanged",
                $"Statut du patient modifié : {patient.FirstName} {patient.LastName} -> {(isActive ? "activé" : "désactivé")}",
                modifiedBy,
                details: new { PatientId = id, NewStatus = isActive });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "TogglePatientStatusError",
                $"Erreur lors du changement de statut du patient {id}",
                modifiedBy,
                details: new { PatientId = id, IsActive = isActive, Error = ex.Message });
            return OperationResult.Error($"Erreur lors du changement de statut : {ex.Message}");
        }
    }

    // Implémentation des méthodes pour les diagnostics
    public async Task<OperationResult<DiagnosisViewModel>> AddDiagnosisAsync(CreateDiagnosisViewModel model, int createdBy)
    {
        try
        {
            // Vérifier si le patient existe
            var patient = await _patientRepository.GetByIdAsync(model.PatientId);
            if (patient == null)
            {
                return OperationResult<DiagnosisViewModel>.Error("Patient introuvable");
            }

            // Validation
            var validation = ValidateDiagnosis(model);
            if (!validation.IsValid)
            {
                return OperationResult<DiagnosisViewModel>.ValidationError(validation.Errors);
            }

            // Créer l'entité
            var diagnosis = new Diagnosis
            {
                PatientId = model.PatientId,
                HospitalCenterId = model.HospitalCenterId,
                DiagnosedBy = createdBy,
                DiagnosisCode = string.IsNullOrWhiteSpace(model.DiagnosisCode) ? null : model.DiagnosisCode.Trim(),
                DiagnosisName = model.DiagnosisName.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Severity = model.Severity,
                DiagnosisDate = model.DiagnosisDate,
                IsActive = model.IsActive,
                CreatedBy = createdBy,
                CreatedAt = TimeZoneHelper.GetCameroonTime()
            };

            var createdDiagnosis = await _diagnosisRepository.AddAsync(diagnosis);

            // Audit
            await _auditService.LogActionAsync(
                createdBy,
                "CREATE",
                "Diagnosis",
                createdDiagnosis.Id,
                null,
                new
                {
                    PatientId = model.PatientId,
                    DiagnosisName = model.DiagnosisName,
                    DiagnosisDate = model.DiagnosisDate
                },
                $"Ajout d'un diagnostic '{model.DiagnosisName}' pour le patient ID {model.PatientId}"
            );

            // Log applicatif
            await _appLogger.LogInfoAsync("PatientService", "DiagnosisAdded",
                $"Diagnostic ajouté pour le patient ID {model.PatientId}: {model.DiagnosisName}",
                createdBy,
                model.HospitalCenterId,
                details: new { DiagnosisId = createdDiagnosis.Id });

            // Récupérer le centre et l'utilisateur pour le ViewModel
            var center = await _centerRepository.GetByIdAsync(model.HospitalCenterId);
            var user = await _userRepository.GetByIdAsync(createdBy);

            // Retourner le ViewModel
            var diagnosisViewModel = new DiagnosisViewModel
            {
                Id = createdDiagnosis.Id,
                PatientId = createdDiagnosis.PatientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                HospitalCenterId = createdDiagnosis.HospitalCenterId,
                HospitalCenterName = center?.Name ?? "Centre inconnu",
                DiagnosedById = createdDiagnosis.DiagnosedBy,
                DiagnosedByName = user != null ? $"{user.FirstName} {user.LastName}" : "Utilisateur inconnu",
                DiagnosisCode = createdDiagnosis.DiagnosisCode,
                DiagnosisName = createdDiagnosis.DiagnosisName,
                Description = createdDiagnosis.Description,
                Severity = createdDiagnosis.Severity,
                DiagnosisDate = createdDiagnosis.DiagnosisDate,
                IsActive = createdDiagnosis.IsActive,
                CreatedAt = createdDiagnosis.CreatedAt
            };

            return OperationResult<DiagnosisViewModel>.Success(diagnosisViewModel);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "AddDiagnosisError",
                $"Erreur lors de l'ajout d'un diagnostic pour le patient {model.PatientId}",
                createdBy,
                model.HospitalCenterId,
                details: new { Model = model, Error = ex.Message });
            return OperationResult<DiagnosisViewModel>.Error($"Erreur lors de l'ajout du diagnostic : {ex.Message}");
        }
    }

    public async Task<List<DiagnosisViewModel>> GetPatientDiagnosesAsync(int patientId)
    {
        try
        {
            return await _diagnosisRepository.QueryListAsync(q =>
                q.Where(d => d.PatientId == patientId)
                 .Include(d => d.DiagnosedByNavigation)
                 .Include(d => d.HospitalCenter)
                 .OrderByDescending(d => d.DiagnosisDate)
                 .Select(d => new DiagnosisViewModel
                 {
                     Id = d.Id,
                     PatientId = d.PatientId,
                     PatientName = $"{d.Patient.FirstName} {d.Patient.LastName}",
                     HospitalCenterId = d.HospitalCenterId,
                     HospitalCenterName = d.HospitalCenter.Name,
                     DiagnosedById = d.DiagnosedBy,
                     DiagnosedByName = $"{d.DiagnosedByNavigation.FirstName} {d.DiagnosedByNavigation.LastName}",
                     DiagnosisCode = d.DiagnosisCode,
                     DiagnosisName = d.DiagnosisName,
                     Description = d.Description,
                     Severity = d.Severity,
                     DiagnosisDate = d.DiagnosisDate,
                     IsActive = d.IsActive,
                     CreatedAt = d.CreatedAt
                 })
            );
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "GetPatientDiagnosesError",
                $"Erreur lors de la récupération des diagnostics du patient {patientId}",
                details: new { PatientId = patientId, Error = ex.Message });
            throw;
        }
    }

    public async Task<DiagnosisViewModel?> GetDiagnosisAsync(int diagnosisId)
    {
        try
        {
            return await _diagnosisRepository.QuerySingleAsync(q =>
                q.Where(d => d.Id == diagnosisId)
                 .Include(d => d.Patient)
                 .Include(d => d.DiagnosedByNavigation)
                 .Include(d => d.HospitalCenter)
                 .Select(d => new DiagnosisViewModel
                 {
                     Id = d.Id,
                     PatientId = d.PatientId,
                     PatientName = $"{d.Patient.FirstName} {d.Patient.LastName}",
                     HospitalCenterId = d.HospitalCenterId,
                     HospitalCenterName = d.HospitalCenter.Name,
                     DiagnosedById = d.DiagnosedBy,
                     DiagnosedByName = $"{d.DiagnosedByNavigation.FirstName} {d.DiagnosedByNavigation.LastName}",
                     DiagnosisCode = d.DiagnosisCode,
                     DiagnosisName = d.DiagnosisName,
                     Description = d.Description,
                     Severity = d.Severity,
                     DiagnosisDate = d.DiagnosisDate,
                     IsActive = d.IsActive,
                     CreatedAt = d.CreatedAt
                 })
            );
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "GetDiagnosisError",
                $"Erreur lors de la récupération du diagnostic {diagnosisId}",
                details: new { DiagnosisId = diagnosisId, Error = ex.Message });
            throw;
        }
    }

    // Implémentation de l'historique patient
    public async Task<PatientHistoryViewModel> GetPatientHistoryAsync(int patientId)
    {
        try
        {
            // Vérifier si le patient existe
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                throw new ArgumentException($"Patient {patientId} introuvable");
            }

            var history = new PatientHistoryViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.FirstName} {patient.LastName}",
                ChronologicalHistory = new List<MedicalEvent>()
            };

            // Récupérer tous les diagnostics du patient
            var diagnoses = await _diagnosisRepository.QueryListAsync(q =>
                q.Where(d => d.PatientId == patientId)
                 .Include(d => d.DiagnosedByNavigation)
                 .Include(d => d.HospitalCenter)
                 .Select(d => new
                 {
                     Id = d.Id,
                     Date = d.DiagnosisDate,
                     Title = d.DiagnosisName,
                     Description = d.Description,
                     StaffName = $"{d.DiagnosedByNavigation.FirstName} {d.DiagnosedByNavigation.LastName}",
                     CenterName = d.HospitalCenter.Name
                 })
            );

            history.TotalDiagnoses = diagnoses.Count;

            // Ajouter les diagnostics à l'historique
            history.ChronologicalHistory.AddRange(diagnoses.Select(d => new MedicalEvent
            {
                Date = d.Date,
                EventType = "Diagnosis",
                EventId = d.Id,
                Title = d.Title,
                Description = d.Description,
                StaffName = d.StaffName,
                HospitalCenterName = d.CenterName
            }));

            // Récupérer tous les épisodes de soins du patient
            var careEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.PatientId == patientId)
                 .Include(ce => ce.PrimaryCaregiverNavigation)
                 .Include(ce => ce.HospitalCenter)
                 .Select(ce => new
                 {
                     Id = ce.Id,
                     Date = ce.EpisodeStartDate,
                     Title = $"Épisode de soins - {ce.Status}",
                     Description = ce.InterruptionReason,
                     StaffName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                     CenterName = ce.HospitalCenter.Name
                 })
            );

            history.TotalCareEpisodes = careEpisodes.Count;

            // Ajouter les épisodes de soins à l'historique
            history.ChronologicalHistory.AddRange(careEpisodes.Select(ce => new MedicalEvent
            {
                Date = ce.Date,
                EventType = "CareEpisode",
                EventId = ce.Id,
                Title = ce.Title,
                Description = ce.Description,
                StaffName = ce.StaffName,
                HospitalCenterName = ce.CenterName
            }));

            // Récupérer tous les examens du patient
            var examinations = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.PatientId == patientId)
                 .Include(e => e.RequestedByNavigation)
                 .Include(e => e.ExaminationType)
                 .Include(e => e.HospitalCenter)
                 .Select(e => new
                 {
                     Id = e.Id,
                     Date = e.RequestDate,
                     Title = $"Examen - {e.ExaminationType.Name}",
                     Description = e.Notes,
                     StaffName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     CenterName = e.HospitalCenter.Name
                 })
            );

            history.TotalExaminations = examinations.Count;

            // Ajouter les examens à l'historique
            history.ChronologicalHistory.AddRange(examinations.Select(e => new MedicalEvent
            {
                Date = e.Date,
                EventType = "Examination",
                EventId = e.Id,
                Title = e.Title,
                Description = e.Description,
                StaffName = e.StaffName,
                HospitalCenterName = e.CenterName
            }));

            // Récupérer toutes les prescriptions du patient
            var prescriptions = await _prescriptionRepository.QueryListAsync(q =>
                q.Where(p => p.PatientId == patientId)
                 .Include(p => p.PrescribedByNavigation)
                 .Include(p => p.HospitalCenter)
                 .Select(p => new
                 {
                     Id = p.Id,
                     Date = p.PrescriptionDate,
                     Title = "Prescription",
                     Description = p.Instructions,
                     StaffName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     CenterName = p.HospitalCenter.Name
                 })
            );

            history.TotalPrescriptions = prescriptions.Count;

            // Ajouter les prescriptions à l'historique
            history.ChronologicalHistory.AddRange(prescriptions.Select(p => new MedicalEvent
            {
                Date = p.Date,
                EventType = "Prescription",
                EventId = p.Id,
                Title = p.Title,
                Description = p.Description,
                StaffName = p.StaffName,
                HospitalCenterName = p.CenterName
            }));

            // Trier l'historique par date (du plus récent au plus ancien)
            history.ChronologicalHistory = history.ChronologicalHistory
                .OrderByDescending(e => e.Date)
                .ToList();

            return history;
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "GetPatientHistoryError",
                $"Erreur lors de la récupération de l'historique du patient {patientId}",
                details: new { PatientId = patientId, Error = ex.Message });
            throw;
        }
    }

    // Implémentation de la recherche
    public async Task<List<PatientSearchResultViewModel>> QuickSearchPatientsAsync(string searchTerm, int hospitalCenterId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                return new List<PatientSearchResultViewModel>();

            var searchLower = searchTerm.ToLower().Trim();
            var numericSearch = searchTerm.All(char.IsDigit);

            return await _patientRepository.QueryListAsync(q =>
                q.Where(p =>
                    p.FirstName.ToLower().Contains(searchLower) ||
                    p.LastName.ToLower().Contains(searchLower) ||
                    (numericSearch && p.PhoneNumber.Contains(searchTerm)))
                 .OrderByDescending(p => p.IsActive)
                 .ThenBy(p => p.LastName)
                 .ThenBy(p => p.FirstName)
                 .Take(10)
                 .Select(p => new PatientSearchResultViewModel
                 {
                     Id = p.Id,
                     Name = $"{p.FirstName} {p.LastName}",
                     PhoneNumber = p.PhoneNumber,
                     AdditionalInfo = p.DateOfBirth.HasValue ? $"Né(e) le {p.DateOfBirth.Value.ToString("dd/MM/yyyy")}" : null,
                     IsActive = p.IsActive
                 })
            );
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "QuickSearchPatientsError",
                "Erreur lors de la recherche rapide de patients",
                details: new { SearchTerm = searchTerm, HospitalCenterId = hospitalCenterId, Error = ex.Message });
            throw;
        }
    }

    public async Task<List<PatientViewModel>> CheckPotentialDuplicatesAsync(string firstName, string lastName, string phoneNumber)
    {
        try
        {
            var firstNameLower = firstName.ToLower().Trim();
            var lastNameLower = lastName.ToLower().Trim();
            // Normaliser le numéro de téléphone en supprimant les espaces et symboles
            var normalizedPhone = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Rechercher les patients avec le même nom, prénom ou numéro de téléphone
            return await _patientRepository.QueryListAsync(q =>
                q.Where(p =>
                    (p.FirstName.ToLower() == firstNameLower && p.LastName.ToLower() == lastNameLower) ||
                    (normalizedPhone.Length > 5 && p.PhoneNumber.Contains(normalizedPhone))
                )
                 .OrderBy(p => p.LastName)
                 .ThenBy(p => p.FirstName)
                 .Select(p => new PatientViewModel
                 {
                     Id = p.Id,
                     FirstName = p.FirstName,
                     LastName = p.LastName,
                     DateOfBirth = p.DateOfBirth,
                     Gender = p.Gender,
                     PhoneNumber = p.PhoneNumber,
                     Email = p.Email,
                     BloodType = p.BloodType,
                     IsActive = p.IsActive,
                     CreatedAt = p.CreatedAt
                 })
            );
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "CheckPotentialDuplicatesError",
                "Erreur lors de la vérification des doublons potentiels",
                details: new { FirstName = firstName, LastName = lastName, PhoneNumber = phoneNumber, Error = ex.Message });
            throw;
        }
    }

    // Statistiques
    public async Task<PatientStatisticsViewModel> GetPatientStatisticsAsync(int hospitalCenterId)
    {
        try
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Obtenir tous les patients
            var allPatients = await _patientRepository.GetAllAsync();

            // Filtrer par centre si nécessaire
            // Pour l'instant, sans filtrage par centre car cette logique dépend de l'implémentation

            // Calculer statistiques de base
            var activePatients = allPatients.Count(p => p.IsActive);
            var inactivePatients = allPatients.Count(p => !p.IsActive);

            // Calculer nouveaux patients ce mois
            var newPatientsThisMonth = allPatients.Count(p => p.CreatedAt >= startOfMonth);

            // Calculer patients par genre
            var malePatients = allPatients.Count(p => p.Gender == "M");
            var femalePatients = allPatients.Count(p => p.Gender == "F");
            var otherGenderPatients = allPatients.Count(p => p.Gender != "M" && p.Gender != "F" && p.Gender != null);

            // Calculer statistiques d'âge
            var patientsWithAge = allPatients.Where(p => p.DateOfBirth.HasValue).ToList();
            var patientsUnder18 = patientsWithAge.Count(p => CalculateAge(p.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue)) < 18);
            var patients18To40 = patientsWithAge.Count(p =>
            {
                var age = CalculateAge(p.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue));
                return age >= 18 && age <= 40;
            });
            var patientsOver40 = patientsWithAge.Count(p => CalculateAge(p.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue)) > 40);

            // Statistiques plus avancées qui nécessiteraient des jointures
            // Ces valeurs devraient être calculées par des requêtes spécifiques
            var patientsWithDiagnosis = 0; // Placeholder
            var patientsWithActiveCare = 0; // Placeholder

            return new PatientStatisticsViewModel
            {
                TotalPatients = allPatients.Count,
                ActivePatients = activePatients,
                InactivePatients = inactivePatients,
                NewPatientsThisMonth = newPatientsThisMonth,
                PatientsWithDiagnosis = patientsWithDiagnosis,
                PatientsWithActiveCare = patientsWithActiveCare,
                MalePatients = malePatients,
                FemalePatients = femalePatients,
                OtherGenderPatients = otherGenderPatients,
                PatientsUnder18 = patientsUnder18,
                Patients18To40 = patients18To40,
                PatientsOver40 = patientsOver40
            };
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync("PatientService", "GetPatientStatisticsError",
                "Erreur lors du calcul des statistiques des patients",
                details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
            throw;
        }
    }

    // Méthodes de validation
    private async Task<ValidationResult> ValidatePatientAsync(CreatePatientViewModel model, int? excludeId = null)
    {
        var errors = new List<string>();

        // Validation du numéro de téléphone (requis et unique)
        if (string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            errors.Add("Le numéro de téléphone est obligatoire");
        }
        else
        {
            // Vérifier l'unicité du numéro de téléphone
            var phoneExists = await _patientRepository.AnyAsync(q =>
                q.Where(p => p.PhoneNumber == model.PhoneNumber && p.Id != (excludeId ?? 0)));

            if (phoneExists)
            {
                errors.Add("Un patient avec ce numéro de téléphone existe déjà");
            }
        }

        // Validation de l'email (unique si renseigné)
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailExists = await _patientRepository.AnyAsync(q =>
                q.Where(p => p.Email == model.Email && p.Id != (excludeId ?? 0)));

            if (emailExists)
            {
                errors.Add("Un patient avec cet email existe déjà");
            }
        }

        // Validation de la date de naissance (ne peut pas être dans le futur)
        if (model.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (model.DateOfBirth > today)
            {
                errors.Add("La date de naissance ne peut pas être dans le futur");
            }
        }

        return errors.Any()
            ? ValidationResult.Invalid(errors.ToArray())
            : ValidationResult.Valid();
    }

    private ValidationResult ValidateDiagnosis(CreateDiagnosisViewModel model)
    {
        var errors = new List<string>();

        // Validation du nom de diagnostic
        if (string.IsNullOrWhiteSpace(model.DiagnosisName))
        {
            errors.Add("Le nom du diagnostic est obligatoire");
        }

        // Validation de la date de diagnostic (ne peut pas être dans le futur)
        if (model.DiagnosisDate > DateTime.Now)
        {
            errors.Add("La date du diagnostic ne peut pas être dans le futur");
        }

        return errors.Any()
            ? ValidationResult.Invalid(errors.ToArray())
            : ValidationResult.Valid();
    }

    // Utilitaires
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}