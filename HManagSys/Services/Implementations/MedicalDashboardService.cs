using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Dashboard;
using HManagSys.Models.ViewModels.Patients;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    public class MedicalDashboardService : IMedicalDashboardService
    {
        private readonly IGenericRepository<Patient> _patientRepository;
        private readonly IGenericRepository<Diagnosis> _diagnosisRepository;
        private readonly IGenericRepository<CareEpisode> _careEpisodeRepository;
        private readonly IGenericRepository<CareService> _careServiceRepository;
        private readonly IGenericRepository<Examination> _examinationRepository;
        private readonly IGenericRepository<Prescription> _prescriptionRepository;
        private readonly IGenericRepository<Payment> _paymentRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IApplicationLogger _appLogger;

        public MedicalDashboardService(
            IGenericRepository<Patient> patientRepository,
            IGenericRepository<Diagnosis> diagnosisRepository,
            IGenericRepository<CareEpisode> careEpisodeRepository,
            IGenericRepository<CareService> careServiceRepository,
            IGenericRepository<Examination> examinationRepository,
            IGenericRepository<Prescription> prescriptionRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IApplicationLogger appLogger)
        {
            _patientRepository = patientRepository;
            _diagnosisRepository = diagnosisRepository;
            _careEpisodeRepository = careEpisodeRepository;
            _careServiceRepository = careServiceRepository;
            _examinationRepository = examinationRepository;
            _prescriptionRepository = prescriptionRepository;
            _paymentRepository = paymentRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _appLogger = appLogger;
        }

        public async Task<MedicalDashboardViewModel> GetMedicalDashboardDataAsync(int hospitalCenterId)
        {
            try
            {
                var center = await _hospitalCenterRepository.GetByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    throw new ArgumentException($"Centre hospitalier introuvable: {hospitalCenterId}");
                }

                var now = TimeZoneHelper.GetCameroonTime();
                var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
                var thirtyDaysAgo = now.AddDays(-30);

                // Récupérer les statistiques générales
                var model = new MedicalDashboardViewModel
                {
                    HospitalCenterId = hospitalCenterId,
                    HospitalCenterName = center.Name
                };

                // Statistiques patients
                var patients = await _patientRepository.QueryListAsync(q =>
                    q.Where(p => p.CareEpisodes.Any(ce => ce.HospitalCenterId == hospitalCenterId))
                     .Select(p => new {
                         IsActive = p.IsActive,
                         CreatedAt = p.CreatedAt
                     })
                );

                // Statistiques épisodes de soins
                var careEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                    q.Where(ce => ce.HospitalCenterId == hospitalCenterId)
                     .Select(ce => new {
                         Id = ce.Id,
                         Status = ce.Status,
                         StartDate = ce.EpisodeStartDate,
                         EndDate = ce.EpisodeEndDate,
                         TotalCost = ce.TotalCost,
                         AmountPaid = ce.AmountPaid
                     })
                );

                // Statistiques examens
                var examinations = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.HospitalCenterId == hospitalCenterId)
                     .Select(e => new {
                         Id = e.Id,
                         Status = e.Status,
                         RequestDate = e.RequestDate,
                         PerformedDate = e.PerformedDate,
                         ExaminationTypeId = e.ExaminationTypeId,
                         ExaminationTypeName = e.ExaminationType.Name
                     })
                );

                // Statistiques prescriptions
                var prescriptions = await _prescriptionRepository.QueryListAsync(q =>
                    q.Where(p => p.HospitalCenterId == hospitalCenterId)
                     .Select(p => new {
                         Id = p.Id,
                         Status = p.Status,
                         PrescriptionDate = p.PrescriptionDate
                     })
                );



                // Patients
                model.TotalPatientsCount = patients.Count;
                model.ActivePatientsCount = patients.Count(p => p.IsActive);
                model.NewPatientsThisMonth = patients.Count(p => p.CreatedAt >= firstDayOfMonth);

                // Épisodes de soins
                model.ActiveCareEpisodesCount = careEpisodes.Count(ce => ce.Status == "Active");
                model.CompletedCareEpisodesThisMonth = careEpisodes.Count(ce =>
                    ce.Status == "Completed" && ce.EndDate.HasValue && ce.EndDate.Value >= firstDayOfMonth);
                model.InterruptedCareEpisodesThisMonth = careEpisodes.Count(ce =>
                    ce.Status == "Interrupted" && ce.EndDate.HasValue && ce.EndDate.Value >= firstDayOfMonth);

                // Examens
                model.PendingExaminationsCount = examinations.Count(e => e.Status == "Requested");
                model.ScheduledExaminationsCount = examinations.Count(e => e.Status == "Scheduled");
                model.CompletedExaminationsThisMonth = examinations.Count(e =>
                    e.Status == "Completed" && e.PerformedDate.HasValue && e.PerformedDate.Value >= firstDayOfMonth);

                // Prescriptions
                model.PendingPrescriptionsCount = prescriptions.Count(p => p.Status == "Pending");
                model.DispensedPrescriptionsThisMonth = prescriptions.Count(p =>
                    p.Status == "Dispensed" && p.PrescriptionDate >= firstDayOfMonth);

                // Finances
                model.TotalCareRevenue = careEpisodes.Sum(ce => ce.AmountPaid);
                model.OutstandingCareBalance = careEpisodes.Sum(ce => ce.TotalCost - ce.AmountPaid);

                // Données pour graphiques
                model.DailyPatientAdmissions = await GetDailyPatientAdmissionsData(hospitalCenterId, thirtyDaysAgo);
                model.MonthlyCareEpisodes = await GetMonthlyCareEpisodesData(hospitalCenterId);
                model.ExaminationsByType = await GetExaminationsByTypeData(hospitalCenterId, thirtyDaysAgo);

                // Activités récentes
                model.RecentActivities = await GetRecentMedicalActivities(hospitalCenterId);

                // Alertes
                model.Alerts = await GenerateMedicalAlerts(hospitalCenterId);

                return model;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("MedicalDashboardService", "GetMedicalDashboardDataError",
                    $"Erreur lors de la récupération des données du tableau de bord médical pour le centre {hospitalCenterId}",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        public async Task<PatientDashboardViewModel> GetPatientDashboardDataAsync(int patientId, int hospitalCenterId)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                {
                    throw new ArgumentException($"Patient introuvable: {patientId}");
                }

                var model = new PatientDashboardViewModel
                {
                    PatientId = patient.Id,
                    PatientName = $"{patient.FirstName} {patient.LastName}",
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    PhoneNumber = patient.PhoneNumber,
                    Email = patient.Email,
                    BloodType = patient.BloodType,
                    Allergies = patient.Allergies,
                    IsActive = patient.IsActive
                };

                // Récupérer les données du patient
                var diagnoses = await _diagnosisRepository.QueryListAsync(q =>
                    q.Where(d => d.PatientId == patientId && d.HospitalCenterId == hospitalCenterId)
                     .OrderByDescending(d => d.DiagnosisDate)
                     .Select(d => new DiagnosisViewModel
                     {
                         Id = d.Id,
                         PatientId = d.PatientId,
                         HospitalCenterId = d.HospitalCenterId,
                         DiagnosedById = d.DiagnosedBy,
                         DiagnosisCode = d.DiagnosisCode,
                         DiagnosisName = d.DiagnosisName,
                         Description = d.Description,
                         Severity = d.Severity,
                         DiagnosisDate = d.DiagnosisDate,
                         IsActive = d.IsActive
                     })
                );

                var careEpisodes = await  _careEpisodeRepository.QueryListAsync(q =>
                    q.Where(ce => ce.PatientId == patientId && ce.HospitalCenterId == hospitalCenterId)
                     .OrderByDescending(ce => ce.EpisodeStartDate)
                     .Select(ce => new CareEpisodeViewModel
                     {
                         Id = ce.Id,
                         PatientId = ce.PatientId,
                         DiagnosisId = ce.DiagnosisId,
                         DiagnosisName = ce.Diagnosis.DiagnosisName,
                         HospitalCenterId = ce.HospitalCenterId,
                         PrimaryCaregiverId = ce.PrimaryCaregiver,
                         EpisodeStartDate = ce.EpisodeStartDate,
                         EpisodeEndDate = ce.EpisodeEndDate,
                         Status = ce.Status,
                         TotalCost = ce.TotalCost,
                         AmountPaid = ce.AmountPaid,
                         RemainingBalance = ce.RemainingBalance
                     })
                );

                var examinations = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.PatientId == patientId && e.HospitalCenterId == hospitalCenterId)
                     .Select(e => new
                     {
                         Id = e.Id,
                         Status = e.Status,
                         FinalPrice = e.FinalPrice,
                         DiscountAmount = e.DiscountAmount,
                         RequestDate = e.RequestDate
                     })
                );

                var prescriptions = await _prescriptionRepository.QueryListAsync(q =>
                    q.Where(p => p.PatientId == patientId && p.HospitalCenterId == hospitalCenterId)
                     .Select(p => new
                     {
                         Id = p.Id,
                         Status = p.Status,
                         PrescriptionDate = p.PrescriptionDate
                     })
                );

                var payments = await  _paymentRepository.QueryListAsync(q =>
                    q.Where(p => p.PatientId == patientId && p.HospitalCenterId == hospitalCenterId)
                     .Select(p => new
                     {
                         Id = p.Id,
                         Amount = p.Amount,
                         PaymentDate = p.PaymentDate
                     })
                );


                // Remplir les statistiques
                model.TotalDiagnosesCount = diagnoses.Count;
                model.TotalCareEpisodesCount = careEpisodes.Count;
                model.ActiveCareEpisodesCount = careEpisodes.Count(ce => ce.Status == "Active");
                model.TotalExaminationsCount = examinations.Count;
                model.PendingExaminationsCount = examinations.Count(e => e.Status == "Requested" || e.Status == "Scheduled");
                model.TotalPrescriptionsCount = prescriptions.Count;
                model.PendingPrescriptionsCount = prescriptions.Count(p => p.Status == "Pending");

                // Finances
                model.TotalAmountBilled = careEpisodes.Sum(ce => ce.TotalCost) + examinations.Sum(e => e.FinalPrice);
                model.TotalAmountPaid = payments.Sum(p => p.Amount);
                model.OutstandingBalance = model.TotalAmountBilled - model.TotalAmountPaid;

                // Déterminer la dernière visite
                var lastVisitDate = careEpisodes.Count > 0
                    ? careEpisodes.Max(ce => ce.EpisodeStartDate)
                    : (DateTime?)null;

                var lastExamDate = examinations.Count > 0
                    ? examinations.Max(e => e.RequestDate)
                    : (DateTime?)null;

                if (lastVisitDate.HasValue && lastExamDate.HasValue)
                {
                    model.LastVisitDate = lastVisitDate > lastExamDate ? lastVisitDate : lastExamDate;
                }
                else if (lastVisitDate.HasValue)
                {
                    model.LastVisitDate = lastVisitDate;
                }
                else
                {
                    model.LastVisitDate = lastExamDate;
                }

                // Diagnostics actifs et épisodes de soins pour l'affichage
                model.ActiveDiagnoses = diagnoses.Where(d => d.IsActive).Take(5).ToList();
                model.ActiveCareEpisodes = careEpisodes.Where(ce => ce.Status == "Active").Take(3).ToList();

                // Récupérer les activités récentes
                model.RecentActivities = await GetPatientRecentActivities(patientId, hospitalCenterId);

                // Données pour graphiques
                model.CareProgressData = await GetPatientCareProgressData(patientId, hospitalCenterId);
                model.PaymentHistoryData = await GetPatientPaymentHistoryData(patientId, hospitalCenterId);

                return model;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("MedicalDashboardService", "GetPatientDashboardDataError",
                    $"Erreur lors de la récupération des données du tableau de bord du patient {patientId}",
                    details: new { PatientId = patientId, HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        public async Task<CareEpisodeSummaryViewModel> GetCareEpisodeSummaryAsync(int episodeId)
        {
            try
            {
                var careEpisode = await _careEpisodeRepository.QuerySingleAsync(q =>
                    q.Where(ce => ce.Id == episodeId)
                     .Include(ce => ce.Patient)
                     .Include(ce => ce.Diagnosis)
                     .Include(ce => ce.PrimaryCaregiverNavigation)
                     .Include(ce => ce.CareServices.Take(5))
                     .Include(ce => ce.Examinations.Take(5))
                     .Include(ce => ce.Prescriptions.Take(5))
                     .Select(ce => new CareEpisodeSummaryViewModel
                     {
                         CareEpisodeId = ce.Id,
                         PatientId = ce.PatientId,
                         PatientName = $"{ce.Patient.FirstName} {ce.Patient.LastName}",
                         DiagnosisId = ce.DiagnosisId,
                         DiagnosisName = ce.Diagnosis.DiagnosisName,
                         PrimaryCaregiverName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                         EpisodeStartDate = ce.EpisodeStartDate,
                         EpisodeEndDate = ce.EpisodeEndDate,
                         Status = ce.Status,
                         TotalCost = ce.TotalCost,
                         AmountPaid = ce.AmountPaid,
                         RemainingBalance = ce.RemainingBalance,
                         CareServicesCount = ce.CareServices.Count,
                         ExaminationsCount = ce.Examinations.Count,
                         PrescriptionsCount = ce.Prescriptions.Count
                     })
                );

                if (careEpisode == null)
                {
                    throw new ArgumentException($"Épisode de soins introuvable: {episodeId}");
                }

                // Récupérer les services récents
                careEpisode.RecentCareServices = await _careServiceRepository.QueryListAsync(q =>
                    q.Where(cs => cs.CareEpisodeId == episodeId)
                     .OrderByDescending(cs => cs.ServiceDate)
                     .Take(5)
                     .Include(cs => cs.CareType)
                     .Include(cs => cs.AdministeredByNavigation)
                     .Select(cs => new CareServiceViewModel
                     {
                         Id = cs.Id,
                         CareEpisodeId = cs.CareEpisodeId,
                         CareTypeId = cs.CareTypeId,
                         CareTypeName = cs.CareType.Name,
                         AdministeredById = cs.AdministeredBy,
                         AdministeredByName = $"{cs.AdministeredByNavigation.FirstName} {cs.AdministeredByNavigation.LastName}",
                         ServiceDate = cs.ServiceDate,
                         Duration = cs.Duration,
                         Notes = cs.Notes,
                         Cost = cs.Cost
                     })
                );

                // Récupérer les examens récents
                careEpisode.RecentExaminations = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.CareEpisodeId == episodeId)
                     .OrderByDescending(e => e.RequestDate)
                     .Take(5)
                     .Include(e => e.ExaminationType)
                     .Include(e => e.RequestedByNavigation)
                     .Select(e => new ExaminationViewModel
                     {
                         Id = e.Id,
                         PatientId = e.PatientId,
                         ExaminationTypeId = e.ExaminationTypeId,
                         ExaminationTypeName = e.ExaminationType.Name,
                         CareEpisodeId = e.CareEpisodeId,
                         RequestedById = e.RequestedBy,
                         RequestedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                         RequestDate = e.RequestDate,
                         ScheduledDate = e.ScheduledDate,
                         PerformedDate = e.PerformedDate,
                         Status = e.Status,
                         FinalPrice = e.FinalPrice
                     })
                );

                // Récupérer les prescriptions récentes
                careEpisode.RecentPrescriptions = await _prescriptionRepository.QueryListAsync(q =>
                    q.Where(p => p.CareEpisodeId == episodeId)
                     .OrderByDescending(p => p.PrescriptionDate)
                     .Take(5)
                     .Include(p => p.PrescribedByNavigation)
                     .Select(p => new PrescriptionViewModel
                     {
                         Id = p.Id,
                         PatientId = p.PatientId,
                         CareEpisodeId = p.CareEpisodeId,
                         PrescribedById = p.PrescribedBy,
                         PrescribedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                         PrescriptionDate = p.PrescriptionDate,
                         Status = p.Status
                     })
                );

                return careEpisode;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("MedicalDashboardService", "GetCareEpisodeSummaryError",
                    $"Erreur lors de la récupération du résumé de l'épisode de soins {episodeId}",
                    details: new { EpisodeId = episodeId, Error = ex.Message });
                throw;
            }
        }

        public async Task<TreatmentProgressViewModel> GetTreatmentProgressAsync(int episodeId)
        {
            try
            {
                var careEpisode = await _careEpisodeRepository.QuerySingleAsync(q =>
                    q.Where(ce => ce.Id == episodeId)
                     .Include(ce => ce.Patient)
                     .Include(ce => ce.Diagnosis)
                     .Select(ce => new TreatmentProgressViewModel
                     {
                         CareEpisodeId = ce.Id,
                         PatientId = ce.PatientId,
                         PatientName = $"{ce.Patient.FirstName} {ce.Patient.LastName}",
                         DiagnosisName = ce.Diagnosis.DiagnosisName,
                         EpisodeStartDate = ce.EpisodeStartDate,
                         EpisodeEndDate = ce.EpisodeEndDate,
                         Status = ce.Status,
                         TotalCost = ce.TotalCost,
                         AmountPaid = ce.AmountPaid,
                         RemainingBalance = ce.RemainingBalance
                     })
                );

                if (careEpisode == null)
                {
                    throw new ArgumentException($"Épisode de soins introuvable: {episodeId}");
                }

                // Récupérer le nombre total et complété de services
                var careServicesData = await _careServiceRepository.QueryListAsync(q =>
                    q.Where(cs => cs.CareEpisodeId == episodeId)
                     .OrderByDescending(cs => cs.ServiceDate)
                     .Select(cs => new
                     {
                         Id = cs.Id,
                         ServiceDate = cs.ServiceDate,
                         CareTypeName = cs.CareType.Name,
                         Cost = cs.Cost
                     })
                );

                // Récupérer les examens liés
                var examinationsData = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.CareEpisodeId == episodeId)
                     .Select(e => new
                     {
                         Id = e.Id,
                         Status = e.Status,
                         RequestDate = e.RequestDate,
                         PerformedDate = e.PerformedDate,
                         ExaminationTypeName = e.ExaminationType.Name
                     })
                );

                // Récupérer les prescriptions liées
                var prescriptionsData = await _prescriptionRepository.QueryListAsync(q =>
                    q.Where(p => p.CareEpisodeId == episodeId)
                     .Select(p => new
                     {
                         Id = p.Id,
                         Status = p.Status,
                         PrescriptionDate = p.PrescriptionDate
                     })
                );

                // Récupérer un échantillon de services récents pour l'affichage
                careEpisode.RecentCareServices = await _careServiceRepository.QueryListAsync(q =>
                    q.Where(cs => cs.CareEpisodeId == episodeId)
                     .OrderByDescending(cs => cs.ServiceDate)
                     .Take(5)
                     .Include(cs => cs.CareType)
                     .Include(cs => cs.AdministeredByNavigation)
                     .Select(cs => new CareServiceViewModel
                     {
                         Id = cs.Id,
                         CareEpisodeId = cs.CareEpisodeId,
                         CareTypeId = cs.CareTypeId,
                         CareTypeName = cs.CareType.Name,
                         AdministeredById = cs.AdministeredBy,
                         AdministeredByName = $"{cs.AdministeredByNavigation.FirstName} {cs.AdministeredByNavigation.LastName}",
                         ServiceDate = cs.ServiceDate,
                         Duration = cs.Duration,
                         Notes = cs.Notes,
                         Cost = cs.Cost
                     })
                );

                // Compiler les statistiques de progression
                careEpisode.TotalServices = careServicesData.Count;
                careEpisode.CompletedServices = careServicesData.Count; // Tous les services enregistrés sont considérés comme terminés

                careEpisode.TotalExaminations = examinationsData.Count;
                careEpisode.CompletedExaminations = examinationsData.Count(e => e.Status == "Completed");

                careEpisode.TotalPrescriptions = prescriptionsData.Count;
                careEpisode.DispensedPrescriptions = prescriptionsData.Count(p => p.Status == "Dispensed");

                // Générer des données pour les graphiques de progression
                careEpisode.ServiceProgressByDate = GenerateServiceProgressChartData(careServicesData, examinationsData);

                // Générer les prochaines étapes
                careEpisode.NextSteps = GenerateNextSteps(episodeId, examinationsData, prescriptionsData);

                return careEpisode;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("MedicalDashboardService", "GetTreatmentProgressError",
                    $"Erreur lors de la récupération de la progression du traitement pour l'épisode {episodeId}",
                    details: new { EpisodeId = episodeId, Error = ex.Message });
                throw;
            }
        }

        public async Task<RecentMedicalStatisticsViewModel> GetRecentMedicalStatisticsAsync(int hospitalCenterId, int days = 30)
        {
            try
            {
                var center = await _hospitalCenterRepository.GetByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    throw new ArgumentException($"Centre hospitalier introuvable: {hospitalCenterId}");
                }

                var now = TimeZoneHelper.GetCameroonTime();
                var fromDate = now.AddDays(-days);

                var model = new RecentMedicalStatisticsViewModel
                {
                    HospitalCenterId = hospitalCenterId,
                    HospitalCenterName = center.Name,
                    Days = days,
                    FromDate = fromDate,
                    ToDate = now
                };

                // Statistiques patients
                var newPatientsCount = await _patientRepository.CountAsync(q =>
                    q.Where(p => p.CareEpisodes.Any(ce => ce.HospitalCenterId == hospitalCenterId) &&
                           p.CreatedAt >= fromDate)
                );

                // Statistiques épisodes de soins
                var careEpisodesStats = await _careEpisodeRepository.QueryListAsync(q =>
                    q.Where(ce => ce.HospitalCenterId == hospitalCenterId &&
                                 (ce.EpisodeStartDate >= fromDate ||
                                  (ce.EpisodeEndDate.HasValue && ce.EpisodeEndDate >= fromDate)))
                     .Select(ce => new {
                         Id = ce.Id,
                         Status = ce.Status,
                         StartDate = ce.EpisodeStartDate,
                         EndDate = ce.EpisodeEndDate,
                         TotalCost = ce.TotalCost,
                         AmountPaid = ce.AmountPaid
                     })
                );

                var newCareEpisodesCount = careEpisodesStats.Count(ce => ce.StartDate >= fromDate);
                var completedCareEpisodesCount = careEpisodesStats.Count(ce =>
                    ce.Status == "Completed" && ce.EndDate.HasValue && ce.EndDate.Value >= fromDate);

                // Statistiques examens
                var examinationsStats = await _examinationRepository.QueryListAsync(q =>
                    q.Where(e => e.HospitalCenterId == hospitalCenterId &&
                                (e.RequestDate >= fromDate ||
                                 (e.PerformedDate.HasValue && e.PerformedDate >= fromDate)))
                     .Select(e => new {
                         Id = e.Id,
                         Status = e.Status,
                         RequestDate = e.RequestDate,
                         PerformedDate = e.PerformedDate,
                         FinalPrice = e.FinalPrice,
                         ExaminationTypeName = e.ExaminationType.Name
                     })
                );

                var newExaminationsCount = examinationsStats.Count(e => e.RequestDate >= fromDate);
                var completedExaminationsCount = examinationsStats.Count(e =>
                    e.Status == "Completed" && e.PerformedDate.HasValue && e.PerformedDate.Value >= fromDate);

                // Statistiques prescriptions
                var prescriptionsStats = await _prescriptionRepository.QueryListAsync(q =>
                    q.Where(p => p.HospitalCenterId == hospitalCenterId && p.PrescriptionDate >= fromDate)
                     .Select(p => new {
                         Id = p.Id,
                         Status = p.Status,
                         PrescriptionDate = p.PrescriptionDate
                     })
                );

                var newPrescriptionsCount = prescriptionsStats.Count;
                var dispensedPrescriptionsCount = prescriptionsStats.Count(p => p.Status == "Dispensed");

                // Statistiques financières
                var careRevenue = careEpisodesStats.Sum(ce => ce.AmountPaid);
                var examinationRevenue = examinationsStats
                    .Where(e => e.Status == "Completed")
                    .Sum(e => e.FinalPrice);

                // Compiler les statistiques
                model.NewPatientsCount = newPatientsCount;
                model.NewCareEpisodesCount = newCareEpisodesCount;
                model.CompletedCareEpisodesCount = completedCareEpisodesCount;
                model.NewExaminationsCount = newExaminationsCount;
                model.CompletedExaminationsCount = completedExaminationsCount;
                model.NewPrescriptionsCount = newPrescriptionsCount;
                model.DispensedPrescriptionsCount = dispensedPrescriptionsCount;
                model.CareRevenueAmount = careRevenue;
                model.ExaminationRevenueAmount = examinationRevenue;
                model.TotalRevenue = careRevenue + examinationRevenue;

                // Générer les données pour les graphiques
                model.DailyRevenueData = await GetDailyRevenueData(hospitalCenterId, fromDate);
                model.ServiceTypeDistribution = GetServiceTypeDistribution(careEpisodesStats, examinationsStats);
                model.DiagnosisDistribution = await GetDiagnosisDistribution(hospitalCenterId, fromDate);

                return model;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("MedicalDashboardService", "GetRecentMedicalStatisticsError",
                    $"Erreur lors de la récupération des statistiques médicales récentes pour le centre {hospitalCenterId}",
                    details: new { HospitalCenterId = hospitalCenterId, Days = days, Error = ex.Message });
                throw;
            }
        }

        #region Private helper methods

        private async Task<List<ChartDataPoint>> GetDailyPatientAdmissionsData(int hospitalCenterId, DateTime fromDate)
        {
            var now = TimeZoneHelper.GetCameroonTime();
            var dailyData = new List<ChartDataPoint>();

            // Créer une liste de dates pour les 30 derniers jours
            var dates = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            // Récupérer les admissions de patients par jour (via les épisodes de soins)
            var admissionsData = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.HospitalCenterId == hospitalCenterId && ce.EpisodeStartDate >= fromDate)
                 .Select(ce => new { Date = ce.EpisodeStartDate.Date })
                 .GroupBy(d => d.Date)
                 .Select(g => new { Date = g.Key, Count = g.Count() })
            );

            // Créer les points de données pour chaque jour
            foreach (var date in dates)
            {
                var admissionsCount = admissionsData
                    .FirstOrDefault(d => d.Date == date.Date)?.Count ?? 0;

                dailyData.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = admissionsCount,
                    Date = date,
                    Color = "#4e73df" // Couleur Bootstrap primary
                });
            }

            return dailyData;
        }

        private async Task<List<ChartDataPoint>> GetMonthlyCareEpisodesData(int hospitalCenterId)
        {
            var now = TimeZoneHelper.GetCameroonTime();
            var monthlyData = new List<ChartDataPoint>();

            // Créer une liste des 6 derniers mois
            var months = Enumerable.Range(0, 6)
                .Select(i => now.AddMonths(-i))
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .OrderBy(d => d)
                .ToList();

            // Récupérer les épisodes de soins groupés par mois
            var episodesData = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.HospitalCenterId == hospitalCenterId &&
                              ce.EpisodeStartDate >= months.First() &&
                              ce.EpisodeStartDate <= now)
                 .Select(ce => new {
                     Month = new DateTime(ce.EpisodeStartDate.Year, ce.EpisodeStartDate.Month, 1),
                     Status = ce.Status
                 })
                 .GroupBy(d => new { d.Month, d.Status })
                 .Select(g => new {
                     Month = g.Key.Month,
                     Status = g.Key.Status,
                     Count = g.Count()
                 })
            );

            // Générer les données pour chaque mois
            foreach (var month in months)
            {
                var activeCount = episodesData
                    .Where(d => d.Month == month && d.Status == "Active")
                    .Sum(d => d.Count);

                var completedCount = episodesData
                    .Where(d => d.Month == month && d.Status == "Completed")
                    .Sum(d => d.Count);

                var interruptedCount = episodesData
                    .Where(d => d.Month == month && d.Status == "Interrupted")
                    .Sum(d => d.Count);

                monthlyData.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = activeCount + completedCount + interruptedCount,
                    Category = "Total",
                    Date = month,
                    Color = "#4e73df" // Bleu
                });

                monthlyData.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = activeCount,
                    Category = "Actifs",
                    Date = month,
                    Color = "#1cc88a" // Vert
                });

                monthlyData.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = completedCount,
                    Category = "Terminés",
                    Date = month,
                    Color = "#36b9cc" // Cyan
                });

                monthlyData.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = interruptedCount,
                    Category = "Interrompus",
                    Date = month,
                    Color = "#f6c23e" // Jaune
                });
            }

            return monthlyData;
        }

        private async Task<List<ChartDataPoint>> GetExaminationsByTypeData(int hospitalCenterId, DateTime fromDate)
        {
            // Récupérer les examens groupés par type
            var examinationsData = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.HospitalCenterId == hospitalCenterId && e.RequestDate >= fromDate)
                 .Select(e => new { TypeName = e.ExaminationType.Name })
                 .GroupBy(e => e.TypeName)
                 .Select(g => new { TypeName = g.Key, Count = g.Count() })
                 .OrderByDescending(g => g.Count)
                 .Take(10) // Limiter aux 10 types les plus courants
            );

            // Générer un tableau de couleurs pour le graphique
            var colors = new[] {
                "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b",
                "#5a5c69", "#858796", "#f8f9fc", "#d1d3e2", "#b7b9cc"
            };

            var chartData = new List<ChartDataPoint>();

            // Créer les points de données
            for (int i = 0; i < examinationsData.Count; i++)
            {
                var item = examinationsData[i];
                chartData.Add(new ChartDataPoint
                {
                    Label = item.TypeName,
                    Value = item.Count,
                    Color = colors[i % colors.Length]
                });
            }

            return chartData;
        }

        private async Task<List<RecentActivityItem>> GetRecentMedicalActivities(int hospitalCenterId)
        {
            var now = TimeZoneHelper.GetCameroonTime();
            var activities = new List<RecentActivityItem>();

            // Récupérer les épisodes de soins récents
            var recentCareEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(ce => ce.EpisodeStartDate)
                 .Take(5)
                 .Include(ce => ce.Patient)
                 .Include(ce => ce.PrimaryCaregiverNavigation)
                 .Select(ce => new RecentActivityItem
                 {
                     ActivityType = "CareEpisode",
                     ReferenceId = ce.Id,
                     Description = $"Épisode de soins: {ce.Diagnosis.DiagnosisName}",
                     PatientName = $"{ce.Patient.FirstName} {ce.Patient.LastName}",
                     UserName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                     ActivityDate = ce.EpisodeStartDate,
                     Status = ce.Status
                 })
            );

            // Récupérer les examens récents
            var recentExaminations = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(e => e.RequestDate)
                 .Take(5)
                 .Include(e => e.Patient)
                 .Include(e => e.RequestedByNavigation)
                 .Select(e => new RecentActivityItem
                 {
                     ActivityType = "Examination",
                     ReferenceId = e.Id,
                     Description = $"Examen: {e.ExaminationType.Name}",
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     UserName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     ActivityDate = e.RequestDate,
                     Status = e.Status
                 })
            );

            // Récupérer les prescriptions récentes
            var recentPrescriptions = await _prescriptionRepository.QueryListAsync(q =>
                q.Where(p => p.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(p => p.PrescriptionDate)
                 .Take(5)
                 .Include(p => p.Patient)
                 .Include(p => p.PrescribedByNavigation)
                 .Select(p => new RecentActivityItem
                 {
                     ActivityType = "Prescription",
                     ReferenceId = p.Id,
                     Description = "Prescription médicale",
                     PatientName = $"{p.Patient.FirstName} {p.Patient.LastName}",
                     UserName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     ActivityDate = p.PrescriptionDate,
                     Status = p.Status
                 })
            );

            // Combiner et trier toutes les activités
            activities.AddRange(recentCareEpisodes);
            activities.AddRange(recentExaminations);
            activities.AddRange(recentPrescriptions);

            return activities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();
        }

        private async Task<List<AlertItem>> GenerateMedicalAlerts(int hospitalCenterId)
        {
            var alerts = new List<AlertItem>();
            var now = TimeZoneHelper.GetCameroonTime();

            // Alertes pour les examens en attente depuis plus de 3 jours
            var pendingExaminations = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.HospitalCenterId == hospitalCenterId &&
                             e.Status == "Requested" &&
                             e.RequestDate <= now.AddDays(-3))
                 .Include(e => e.Patient)
                 .OrderBy(e => e.RequestDate)
                 .Take(5)
                 .Select(e => new
                 {
                     Id = e.Id,
                     PatientName = $"{e.Patient.FirstName} {e.Patient.LastName}",
                     ExaminationTypeName = e.ExaminationType.Name,
                     RequestDate = e.RequestDate
                 })
            );

            // Alertes pour les épisodes de soins actifs sans services récents
            var careEpisodesWithoutRecentServices = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.HospitalCenterId == hospitalCenterId &&
                              ce.Status == "Active" &&
                              !ce.CareServices.Any(cs => cs.ServiceDate >= now.AddDays(-7)))
                 .Include(ce => ce.Patient)
                 .OrderBy(ce => ce.EpisodeStartDate)
                 .Take(5)
                 .Select(ce => new
                 {
                     Id = ce.Id,
                     PatientName = $"{ce.Patient.FirstName} {ce.Patient.LastName}",
                     DiagnosisName = ce.Diagnosis.DiagnosisName,
                     LastServiceDate = ce.CareServices.Any() ? ce.CareServices.Max(cs => cs.ServiceDate) : (DateTime?)null
                 })
            );

            // Créer les alertes pour les examens en attente
            foreach (var exam in pendingExaminations)
            {
                var daysPending = (int)(now - exam.RequestDate).TotalDays;
                alerts.Add(new AlertItem
                {
                    AlertType = daysPending > 7 ? "Danger" : "Warning",
                    Title = "Examen en attente",
                    Message = $"L'examen {exam.ExaminationTypeName} pour {exam.PatientName} est en attente depuis {daysPending} jours",
                    CreatedAt = now,
                    ReferenceId = exam.Id,
                    ReferenceType = "Examination"
                });
            }

            // Créer les alertes pour les épisodes de soins sans services récents
            foreach (var episode in careEpisodesWithoutRecentServices)
            {
                var message = episode.LastServiceDate.HasValue
                    ? $"Aucun service de soins depuis {(int)(now - episode.LastServiceDate.Value).TotalDays} jours"
                    : "Aucun service de soins enregistré";

                alerts.Add(new AlertItem
                {
                    AlertType = "Warning",
                    Title = "Suivi de soins",
                    Message = $"Patient {episode.PatientName} ({episode.DiagnosisName}): {message}",
                    CreatedAt = now,
                    ReferenceId = episode.Id,
                    ReferenceType = "CareEpisode"
                });
            }

            // Trier les alertes par type et date
            return alerts
                .OrderBy(a => a.AlertType == "Danger" ? 0 : a.AlertType == "Warning" ? 1 : 2)
                .ThenBy(a => a.CreatedAt)
                .Take(10)
                .ToList();
        }

        private async Task<List<PatientActivityItem>> GetPatientRecentActivities(int patientId, int hospitalCenterId)
        {
            var activities = new List<PatientActivityItem>();

            // Récupérer les diagnostics récents
            var recentDiagnoses = await _diagnosisRepository.QueryListAsync(q =>
                q.Where(d => d.PatientId == patientId && d.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(d => d.DiagnosisDate)
                 .Take(3)
                 .Include(d => d.DiagnosedByNavigation)
                 .Select(d => new PatientActivityItem
                 {
                     ActivityType = "Diagnosis",
                     ReferenceId = d.Id,
                     Description = $"Diagnostic: {d.DiagnosisName}",
                     PerformedByName = $"{d.DiagnosedByNavigation.FirstName} {d.DiagnosedByNavigation.LastName}",
                     ActivityDate = d.DiagnosisDate,
                     Status = d.IsActive ? "Active" : "Inactive"
                 })
            );

            // Récupérer les épisodes de soins récents
            var recentCareEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.PatientId == patientId && ce.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(ce => ce.EpisodeStartDate)
                 .Take(3)
                 .Include(ce => ce.PrimaryCaregiverNavigation)
                 .Select(ce => new PatientActivityItem
                 {
                     ActivityType = "CareEpisode",
                     ReferenceId = ce.Id,
                     Description = $"Épisode de soins: {ce.Diagnosis.DiagnosisName}",
                     PerformedByName = $"{ce.PrimaryCaregiverNavigation.FirstName} {ce.PrimaryCaregiverNavigation.LastName}",
                     ActivityDate = ce.EpisodeStartDate,
                     Status = ce.Status
                 })
            );

            // Récupérer les examens récents
            var recentExaminations = await _examinationRepository.QueryListAsync(q =>
                q.Where(e => e.PatientId == patientId && e.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(e => e.RequestDate)
                 .Take(3)
                 .Include(e => e.RequestedByNavigation)
                 .Select(e => new PatientActivityItem
                 {
                     ActivityType = "Examination",
                     ReferenceId = e.Id,
                     Description = $"Examen: {e.ExaminationType.Name}",
                     PerformedByName = $"{e.RequestedByNavigation.FirstName} {e.RequestedByNavigation.LastName}",
                     ActivityDate = e.RequestDate,
                     Status = e.Status,
                     Amount = e.FinalPrice
                 })
            );

            // Récupérer les prescriptions récentes
            var recentPrescriptions = await _prescriptionRepository.QueryListAsync(q =>
                q.Where(p => p.PatientId == patientId && p.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(p => p.PrescriptionDate)
                 .Take(3)
                 .Include(p => p.PrescribedByNavigation)
                 .Select(p => new PatientActivityItem
                 {
                     ActivityType = "Prescription",
                     ReferenceId = p.Id,
                     Description = "Prescription médicale",
                     PerformedByName = $"{p.PrescribedByNavigation.FirstName} {p.PrescribedByNavigation.LastName}",
                     ActivityDate = p.PrescriptionDate,
                     Status = p.Status
                 })
            );

            // Récupérer les paiements récents
            var recentPayments = await _paymentRepository.QueryListAsync(q =>
                q.Where(p => p.PatientId == patientId && p.HospitalCenterId == hospitalCenterId)
                 .OrderByDescending(p => p.PaymentDate)
                 .Take(3)
                 .Include(p => p.ReceivedByNavigation)
                 .Select(p => new PatientActivityItem
                 {
                     ActivityType = "Payment",
                     ReferenceId = p.Id,
                     Description = $"Paiement ({p.ReferenceType} #{p.ReferenceId})",
                     PerformedByName = $"{p.ReceivedByNavigation.FirstName} {p.ReceivedByNavigation.LastName}",
                     ActivityDate = p.PaymentDate,
                     Status = "Completed",
                     Amount = p.Amount
                 })
            );

            // Combiner et trier toutes les activités
            activities.AddRange(recentDiagnoses);
            activities.AddRange(recentCareEpisodes);
            activities.AddRange(recentExaminations);
            activities.AddRange(recentPrescriptions);
            activities.AddRange(recentPayments);

            return activities
                .OrderByDescending(a => a.ActivityDate)
                .Take(10)
                .ToList();
        }

        private async Task<List<ChartDataPoint>> GetPatientCareProgressData(int patientId, int hospitalCenterId)
        {
            var data = new List<ChartDataPoint>();

            // Récupérer les épisodes de soins
            var careEpisodes = await _careEpisodeRepository.QueryListAsync(q =>
                q.Where(ce => ce.PatientId == patientId && ce.HospitalCenterId == hospitalCenterId)
                 .OrderBy(ce => ce.EpisodeStartDate)
                 .Select(ce => new
                 {
                     Id = ce.Id,
                     StartDate = ce.EpisodeStartDate,
                     EndDate = ce.EpisodeEndDate,
                     Status = ce.Status,
                     DiagnosisName = ce.Diagnosis.DiagnosisName,
                     ServicesCount = ce.CareServices.Count
                 })
            );

            // Générer les données pour le graphique
            foreach (var episode in careEpisodes)
            {
                var progressPercentage = episode.Status == "Completed" ? 100 :
                                        episode.Status == "Interrupted" ? 50 :
                                        CalculateProgressPercentage(episode.StartDate, episode.EndDate);

                data.Add(new ChartDataPoint
                {
                    Label = episode.DiagnosisName,
                    Value = progressPercentage,
                    Category = episode.Status,
                    Color = episode.Status == "Completed" ? "#1cc88a" :
                            episode.Status == "Active" ? "#4e73df" :
                            "#f6c23e"
                });
            }

            return data;
        }

        private async Task<List<ChartDataPoint>> GetPatientPaymentHistoryData(int patientId, int hospitalCenterId)
        {
            var now = TimeZoneHelper.GetCameroonTime();
            var sixMonthsAgo = now.AddMonths(-6);
            var data = new List<ChartDataPoint>();

            // Créer une liste des 6 derniers mois
            var months = Enumerable.Range(0, 6)
                .Select(i => now.AddMonths(-i))
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .OrderBy(d => d)
                .ToList();

            // Récupérer les paiements groupés par mois
            var paymentsData = await _paymentRepository.QueryListAsync(q =>
                q.Where(p => p.PatientId == patientId &&
                            p.HospitalCenterId == hospitalCenterId &&
                            p.PaymentDate >= sixMonthsAgo)
                 .Select(p => new {
                     Amount = p.Amount,
                     Month = new DateTime(p.PaymentDate.Year, p.PaymentDate.Month, 1),
                 })
                 .GroupBy(p => p.Month)
                 .Select(g => new {
                     Month = g.Key,
                     Amount = g.Sum(p => p.Amount)
                 })
            );

            // Créer les points de données pour chaque mois
            foreach (var month in months)
            {
                var amount = paymentsData
                    .FirstOrDefault(p => p.Month == month)?.Amount ?? 0;

                data.Add(new ChartDataPoint
                {
                    Label = month.ToString("MMM yyyy"),
                    Value = (double)amount,
                    Date = month,
                    Color = "#4e73df"
                });
            }

            return data;
        }

        private List<ChartDataPoint> GenerateServiceProgressChartData(
            IEnumerable<dynamic> careServicesData,
            IEnumerable<dynamic> examinationsData)
        {
            var data = new List<ChartDataPoint>();
            var now = TimeZoneHelper.GetCameroonTime();

            // Regrouper les services par semaine
            var startDate = now.AddDays(-60); // Remontez jusqu'à 60 jours
            var weekGroups = Enumerable.Range(0, 9) // 9 semaines
                .Select(i => new
                {
                    WeekStart = now.AddDays(-7 * i),
                    WeekEnd = now.AddDays(-7 * i + 6),
                    WeekNumber = i
                })
                .Where(w => w.WeekStart >= startDate)
                .ToList();

            // Compter les services par semaine
            foreach (var week in weekGroups)
            {
                var careServicesInWeek = careServicesData.Count(cs =>
                    ((DateTime)cs.ServiceDate) >= week.WeekStart &&
                    ((DateTime)cs.ServiceDate) <= week.WeekEnd);

                var completedExamsInWeek = examinationsData.Count(e =>
                    e.Status == "Completed" &&
                    e.PerformedDate.HasValue &&
                    ((DateTime)e.PerformedDate) >= week.WeekStart &&
                    ((DateTime)e.PerformedDate) <= week.WeekEnd);

                data.Add(new ChartDataPoint
                {
                    Label = $"S{week.WeekNumber + 1}",
                    Value = careServicesInWeek + completedExamsInWeek,
                    Category = "Services",
                    Date = week.WeekStart,
                    Color = "#4e73df"
                });
            }

            return data;
        }

        private List<TreatmentNextStep> GenerateNextSteps(
            int episodeId,
            IEnumerable<dynamic> examinationsData,
            IEnumerable<dynamic> prescriptionsData)
        {
            var steps = new List<TreatmentNextStep>();
            var now = TimeZoneHelper.GetCameroonTime();

            // Examens planifiés mais pas encore réalisés
            var pendingExams = examinationsData
                .Where(e => e.Status == "Scheduled" || e.Status == "Requested")
                .ToList();

            foreach (var exam in pendingExams)
            {
                var dueDate = exam.Status == "Scheduled" ? exam.PerformedDate : now.AddDays(3);
                var priority = exam.Status == "Scheduled" ? "High" : "Normal";

                steps.Add(new TreatmentNextStep
                {
                    StepType = "Examination",
                    Description = $"Réaliser l'examen {exam.ExaminationTypeName}",
                    DueDate = dueDate,
                    Priority = priority
                });
            }

            // Prescriptions en attente
            var pendingPrescriptions = prescriptionsData
                .Where(p => p.Status == "Pending")
                .ToList();

            foreach (var prescription in pendingPrescriptions)
            {
                steps.Add(new TreatmentNextStep
                {
                    StepType = "Prescription",
                    Description = "Dispenser la prescription médicale",
                    DueDate = ((DateTime)prescription.PrescriptionDate).AddDays(1),
                    Priority = "Normal"
                });
            }

            // Prochain service de soins (fréquence recommandée)
            steps.Add(new TreatmentNextStep
            {
                StepType = "CareService",
                Description = "Planifier le prochain service de soins",
                DueDate = now.AddDays(7),
                Priority = "Normal"
            });

            return steps
                .OrderBy(s => s.Priority == "Urgent" ? 0 : s.Priority == "High" ? 1 : 2)
                .ThenBy(s => s.DueDate)
                .Take(5)
                .ToList();
        }

        private static int CalculateProgressPercentage(DateTime startDate, DateTime? endDate)
        {
            var now = TimeZoneHelper.GetCameroonTime();

            // Si la date de fin est définie, calculer par rapport à elle
            if (endDate.HasValue)
            {
                return 100;
            }

            // Sinon, estimer la progression (supposons qu'un épisode typique dure 30 jours)
            var totalDuration = TimeSpan.FromDays(30);
            var elapsed = now - startDate;

            var progressPercentage = (int)Math.Min(100, (elapsed.TotalDays / totalDuration.TotalDays) * 100);
            return progressPercentage;
        }

        private async Task<List<ChartDataPoint>> GetDailyRevenueData(int hospitalCenterId, DateTime fromDate)
        {
            var now = TimeZoneHelper.GetCameroonTime();
            var data = new List<ChartDataPoint>();

            // Créer une liste de dates pour les 30 derniers jours
            var dates = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            // Récupérer les revenus quotidiens des paiements
            var paymentsData = await _paymentRepository.QueryListAsync(q =>
                q.Where(p => p.HospitalCenterId == hospitalCenterId && p.PaymentDate >= fromDate)
                 .Select(p => new { Date = p.PaymentDate.Date, Amount = p.Amount })
                 .GroupBy(p => p.Date)
                 .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) })
            );

            // Créer les points de données pour chaque jour
            foreach (var date in dates)
            {
                var amount = paymentsData
                    .FirstOrDefault(p => p.Date == date.Date)?.Amount ?? 0;

                data.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = (double)amount,
                    Date = date,
                    Color = "#1cc88a" // Vert
                });
            }

            return data;
        }

        private List<ChartDataPoint> GetServiceTypeDistribution(
            IEnumerable<dynamic> careEpisodesData,
            IEnumerable<dynamic> examinationsData)
        {
            var data = new List<ChartDataPoint>();
            var colors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b" };

            // Revenus des épisodes de soins
            var careEpisodesRevenue = careEpisodesData.Sum(ce => (decimal)ce.AmountPaid);

            // Revenus des examens complétés
            var examinationsRevenue = examinationsData
                .Where(e => e.Status == "Completed")
                .Sum(e => (decimal)e.FinalPrice);

            // Créer les points de données
            data.Add(new ChartDataPoint
            {
                Label = "Soins médicaux",
                Value = (double)careEpisodesRevenue,
                Color = colors[0]
            });

            data.Add(new ChartDataPoint
            {
                Label = "Examens",
                Value = (double)examinationsRevenue,
                Color = colors[1]
            });

            return data;
        }

        private async Task<List<ChartDataPoint>> GetDiagnosisDistribution(int hospitalCenterId, DateTime fromDate)
        {
            // Récupérer les diagnostics les plus fréquents
            var diagnosisData = await _diagnosisRepository.QueryListAsync(q =>
                q.Where(d => d.HospitalCenterId == hospitalCenterId && d.DiagnosisDate >= fromDate)
                 .GroupBy(d => d.DiagnosisName)
                 .Select(g => new { Name = g.Key, Count = g.Count() })
                 .OrderByDescending(g => g.Count)
                 .Take(5)
            );

            var colors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b" };
            var data = new List<ChartDataPoint>();

            // Créer les points de données
            for (int i = 0; i < diagnosisData.Count; i++)
            {
                var item = diagnosisData[i];
                data.Add(new ChartDataPoint
                {
                    Label = item.Name,
                    Value = item.Count,
                    Color = colors[i % colors.Length]
                });
            }

            return data;
        }

        #endregion
    }
}