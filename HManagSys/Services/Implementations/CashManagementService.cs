using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Finance;
using HManagSys.Models.ViewModels.Payments;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HManagSys.Services.Implementations
{
    public class CashManagementService : ICashManagementService
    {
        private readonly IGenericRepository<CashHandover> _cashHandoverRepository;
        private readonly IGenericRepository<Payment> _paymentRepository;
        private readonly IGenericRepository<Financier> _financierRepository;
        private readonly IGenericRepository<HospitalCenter> _hospitalCenterRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IDocumentGenerationService _documentGenerationService;
        private readonly IApplicationLogger _logger;
        private readonly IAuditService _auditService;

        public CashManagementService(
            IGenericRepository<CashHandover> cashHandoverRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<Financier> financierRepository,
            IGenericRepository<HospitalCenter> hospitalCenterRepository,
            IGenericRepository<User> userRepository,
            IDocumentGenerationService documentGenerationService,
            IApplicationLogger logger,
            IAuditService auditService)
        {
            _cashHandoverRepository = cashHandoverRepository;
            _paymentRepository = paymentRepository;
            _financierRepository = financierRepository;
            _hospitalCenterRepository = hospitalCenterRepository;
            _userRepository = userRepository;
            _documentGenerationService = documentGenerationService;
            _logger = logger;
            _auditService = auditService;
        }

        /// <summary>
        /// Récupère une remise par son ID
        /// </summary>
        public async Task<CashHandoverViewModel?> GetHandoverByIdAsync(int id)
        {
            try
            {
                var handover = await _cashHandoverRepository.QuerySingleAsync(q =>
                    q.Where(h => h.Id == id)
                     .Include(h => h.Financier)
                     .Include(h => h.HospitalCenter)
                     .Include(h => h.HandedOverByNavigation)
                     .Select(h => new CashHandoverViewModel
                     {
                         Id = h.Id,
                         HospitalCenterId = h.HospitalCenterId,
                         HospitalCenterName = h.HospitalCenter.Name,
                         FinancierId = h.FinancierId,
                         FinancierName = h.Financier.Name,
                         HandoverDate = h.HandoverDate,
                         TotalCashAmount = h.TotalCashAmount,
                         HandoverAmount = h.HandoverAmount,
                         RemainingCashAmount = h.RemainingCashAmount,
                         HandedOverBy = h.HandedOverBy,
                         HandedOverByName = $"{h.HandedOverByNavigation.FirstName} {h.HandedOverByNavigation.LastName}",
                         Notes = h.Notes,
                         CreatedAt = h.CreatedAt,
                         CreatedBy = h.CreatedBy
                     }));

                if (handover == null)
                    return null;

                // Récupérer le nom du créateur
                if (handover.CreatedBy > 0)
                {
                    var creator = await _userRepository.GetByIdAsync(handover.CreatedBy);
                    if (creator != null)
                    {
                        handover.CreatedByName = $"{creator.FirstName} {creator.LastName}";
                    }
                }

                return handover;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GetHandoverByIdError",
                    $"Erreur lors de la récupération de la remise {id}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère les remises avec pagination et filtres
        /// </summary>
        public async Task<(List<CashHandoverViewModel> Items, int TotalCount)> GetCashHandoversAsync(CashHandoverFilters filters)
        {
            try
            {

                // Construire la requête de base
                var query = _cashHandoverRepository.AsQueryable()
                    .Include(h => h.Financier)
                    .Include(h => h.HospitalCenter)
                    .Include(h => h.HandedOverByNavigation)
                    .AsQueryable();

                // Appliquer les filtres
                if (filters.FinancierId.HasValue)
                    query = query.Where(h => h.FinancierId == filters.FinancierId.Value);


                if (filters.HospitalCenterId.HasValue)
                    query = query.Where(h => h.HospitalCenterId == filters.HospitalCenterId.Value);


                if (filters.HandedOverBy.HasValue)
                    query = query.Where(h => h.HandedOverBy == filters.HandedOverBy.Value);


                if (filters.FromDate.HasValue)
                {
                    var fromDate = filters.FromDate.Value.Date;
                    query = query.Where(h => h.HandoverDate >= fromDate);
                }

                if (filters.ToDate.HasValue)
                {
                    var toDate = filters.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(h => h.HandoverDate <= toDate);
                }

                if (filters.MinAmount.HasValue)
                    query = query.Where(h => h.HandoverAmount >= filters.MinAmount.Value);


                if (filters.MaxAmount.HasValue)
                    query = query.Where(h => h.HandoverAmount <= filters.MaxAmount.Value);


                // Compter le nombre total
                var totalCount = await query.CountAsync();

                // Appliquer la pagination et projeter
                var items = await query
                    .OrderByDescending(h => h.HandoverDate)
                    .Skip((filters.PageIndex - 1) * filters.PageSize)
                    .Take(filters.PageSize)
                    .Select(h => new CashHandoverViewModel
                    {
                        Id = h.Id,
                        HospitalCenterId = h.HospitalCenterId,
                        HospitalCenterName = h.HospitalCenter.Name,
                        FinancierId = h.FinancierId,
                        FinancierName = h.Financier.Name,
                        HandoverDate = h.HandoverDate,
                        TotalCashAmount = h.TotalCashAmount,
                        HandoverAmount = h.HandoverAmount,
                        RemainingCashAmount = h.RemainingCashAmount,
                        HandedOverBy = h.HandedOverBy,
                        HandedOverByName = $"{h.HandedOverByNavigation.FirstName} {h.HandedOverByNavigation.LastName}",
                        Notes = h.Notes,
                        CreatedAt = h.CreatedAt,
                        CreatedBy = h.CreatedBy
                    })
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GetCashHandoversError",
                    "Erreur lors de la récupération des remises",
                    details: new { Filters = filters, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Crée une nouvelle remise d'espèces
        /// </summary>
        public async Task<OperationResult<CashHandoverViewModel>> CreateCashHandoverAsync(CreateCashHandoverViewModel model, int createdBy)
        {
            try
            {
                // Vérifier que le centre existe
                var center = await _hospitalCenterRepository.GetByIdAsync(model.HospitalCenterId);
                if (center == null)
                {
                    return OperationResult<CashHandoverViewModel>.Error("Centre hospitalier invalide");
                }

                // Vérifier que le financier existe et est actif
                var financier = await _financierRepository.GetByIdAsync(model.FinancierId);
                if (financier == null)
                {
                    return OperationResult<CashHandoverViewModel>.Error("Financier invalide");
                }

                if (!financier.IsActive)
                {
                    return OperationResult<CashHandoverViewModel>.Error("Ce financier est inactif");
                }

                // Vérifier que l'utilisateur qui remet les espèces existe
                var user = await _userRepository.GetByIdAsync(model.HandedOverBy);
                if (user == null)
                {
                    return OperationResult<CashHandoverViewModel>.Error("Utilisateur invalide");
                }

                // Vérifier que le montant restant est cohérent
                if (model.TotalCashAmount < model.HandoverAmount)
                {
                    return OperationResult<CashHandoverViewModel>.Error("Le montant remis ne peut pas être supérieur au montant total en caisse");
                }

                if (model.TotalCashAmount != model.HandoverAmount + model.RemainingCashAmount)
                {
                    model.RemainingCashAmount = model.TotalCashAmount - model.HandoverAmount;
                }

                // Créer la remise
                var handover = new CashHandover
                {
                    HospitalCenterId = model.HospitalCenterId,
                    FinancierId = model.FinancierId,
                    HandoverDate = model.HandoverDate,
                    TotalCashAmount = model.TotalCashAmount,
                    HandoverAmount = model.HandoverAmount,
                    RemainingCashAmount = model.RemainingCashAmount,
                    HandedOverBy = model.HandedOverBy,
                    Notes = model.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var createdHandover = await _cashHandoverRepository.AddAsync(handover);

                // Journaliser l'action
                await _auditService.LogActionAsync(
                    createdBy,
                    "CASH_HANDOVER_CREATE",
                    "CashHandover",
                    createdHandover.Id,
                    null,
                    new
                    {
                        createdHandover.FinancierId,
                        createdHandover.HandoverAmount,
                        createdHandover.TotalCashAmount,
                        createdHandover.RemainingCashAmount
                    },
                    $"Remise d'espèces de {createdHandover.HandoverAmount:N0} FCFA au financier {financier.Name}"
                );

                // Retourner le viewmodel
                var result = await GetHandoverByIdAsync(createdHandover.Id);
                return OperationResult<CashHandoverViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "CreateCashHandoverError",
                    "Erreur lors de la création de la remise d'espèces",
                    createdBy,
                    model.HospitalCenterId,
                    details: new { Model = model, Error = ex.Message });

                return OperationResult<CashHandoverViewModel>.Error("Une erreur est survenue lors de la création de la remise d'espèces");
            }
        }

        /// <summary>
        /// Récupère l'état de la caisse pour un centre
        /// </summary>
        public async Task<CashPositionViewModel> GetCashPositionAsync(int hospitalCenterId)
        {
            try
            {
                // Récupérer le centre
                var center = await _hospitalCenterRepository.GetByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    throw new Exception($"Centre hospitalier {hospitalCenterId} introuvable");
                }

                // Récupérer la dernière remise
                var lastHandover = await _cashHandoverRepository.QuerySingleAsync(q =>
                    q.Where(h => h.HospitalCenterId == hospitalCenterId)
                     .OrderByDescending(h => h.HandoverDate)
                     .Select(h => new {
                         h.HandoverDate,
                         h.HandoverAmount,
                         h.RemainingCashAmount
                     }));

                // Si aucune remise n'a été effectuée, initialiser avec des valeurs par défaut
                if (lastHandover == null)
                {
                    return new CashPositionViewModel
                    {
                        HospitalCenterId = hospitalCenterId,
                        HospitalCenterName = center.Name,
                        CurrentBalance = await GetCurrentCashBalanceAsync(hospitalCenterId),
                        LastHandoverDate = DateTime.MinValue,
                        LastHandoverAmount = 0,
                        ReceiptsSinceLastHandover = await GetCurrentCashBalanceAsync(hospitalCenterId),
                        DaysSinceLastHandover = 0,
                        AverageDailyReceipts = 0
                    };
                }

                // Calculer le montant reçu depuis la dernière remise
                decimal receiptsSinceLastHandover = 0;

                // Récupérer tous les paiements en espèces depuis la dernière remise
                var cashPayments = await _paymentRepository.SumAsync(q =>
                    q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                p.PaymentMethod.Name.Contains("Espèces") &&
                                p.PaymentDate > lastHandover.HandoverDate &&
                                (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]"))), x=> x.Amount);


                receiptsSinceLastHandover = cashPayments;

                // Calculer le solde actuel
                decimal currentBalance = lastHandover.RemainingCashAmount + receiptsSinceLastHandover;

                // Calculer le nombre de jours depuis la dernière remise
                int daysSinceLastHandover = (int)(DateTime.Now - lastHandover.HandoverDate).TotalDays;
                daysSinceLastHandover = Math.Max(1, daysSinceLastHandover); // Au moins 1 jour

                // Calculer la moyenne journalière des recettes
                decimal averageDailyReceipts = receiptsSinceLastHandover / daysSinceLastHandover;

                return new CashPositionViewModel
                {
                    HospitalCenterId = hospitalCenterId,
                    HospitalCenterName = center.Name,
                    CurrentBalance = currentBalance,
                    LastHandoverDate = lastHandover.HandoverDate,
                    LastHandoverAmount = lastHandover.HandoverAmount,
                    ReceiptsSinceLastHandover = receiptsSinceLastHandover,
                    DaysSinceLastHandover = daysSinceLastHandover,
                    AverageDailyReceipts = averageDailyReceipts
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GetCashPositionError",
                    $"Erreur lors de la récupération de l'état de la caisse pour le centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère l'historique des mouvements de caisse pour un centre
        /// </summary>
        public async Task<List<CashMovementViewModel>> GetCashMovementsAsync(int hospitalCenterId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                // Liste pour stocker tous les mouvements
                var allMovements = new List<CashMovementViewModel>();

                // Date de début par défaut (30 jours en arrière si non spécifiée)
                var startDate = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;

                // Date de fin par défaut (aujourd'hui si non spécifiée)
                var endDate = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

                // 1. Récupérer les paiements en espèces (entrées)
                var cashPayments = await _paymentRepository.QueryListAsync(q =>
                    q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                p.PaymentMethod.Name.Contains("Espèces") &&
                                p.PaymentDate >= startDate &&
                                p.PaymentDate <= endDate &&
                                (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]")))
                     .OrderBy(p => p.PaymentDate)
                     .Select(p => new {
                         Date = p.PaymentDate,
                         Amount = p.Amount,
                         Type = "Paiement",
                         Description = $"Paiement {p.ReferenceType} #{p.ReferenceId}",
                         ReferenceType = p.ReferenceType,
                         ReferenceId = p.ReferenceId
                     }));

                // Ajouter les paiements à la liste des mouvements
                foreach (var payment in cashPayments)
                {
                    allMovements.Add(new CashMovementViewModel
                    {
                        Date = payment.Date,
                        Type = payment.Type,
                        Description = payment.Description,
                        Amount = payment.Amount,
                        Direction = "IN",
                        ReferenceType = payment.ReferenceType,
                        ReferenceId = payment.ReferenceId,
                        Balance = 0 // Sera calculé plus tard
                    });
                }

                // 2. Récupérer les remises aux financiers (sorties)
                var handovers = await _cashHandoverRepository.QueryListAsync(q =>
                    q.Where(h => h.HospitalCenterId == hospitalCenterId &&
                               h.HandoverDate >= startDate &&
                               h.HandoverDate <= endDate)
                     .OrderBy(h => h.HandoverDate)
                     .Select(h => new {
                         Date = h.HandoverDate,
                         Amount = h.HandoverAmount,
                         Type = "Remise",
                         Description = $"Remise au financier {h.Financier.Name}",
                         ReferenceType = "CashHandover",
                         ReferenceId = h.Id
                     }));

                // Ajouter les remises à la liste des mouvements
                foreach (var handover in handovers)
                {
                    allMovements.Add(new CashMovementViewModel
                    {
                        Date = handover.Date,
                        Type = handover.Type,
                        Description = handover.Description,
                        Amount = handover.Amount,
                        Direction = "OUT",
                        ReferenceType = handover.ReferenceType,
                        ReferenceId = handover.ReferenceId,
                        Balance = 0 // Sera calculé plus tard
                    });
                }

                // Trier tous les mouvements par date
                allMovements = allMovements.OrderBy(m => m.Date).ToList();

                // Calculer le solde initial
                // Pour cela, on doit connaître le solde à la date de début, qui est :
                // - Le solde restant de la dernière remise avant la date de début
                // - Plus tous les paiements entre cette remise et la date de début
                decimal initialBalance = await CalculateBalanceAtDateAsync(hospitalCenterId, startDate);

                // Calculer les soldes progressifs
                decimal runningBalance = initialBalance;
                foreach (var movement in allMovements)
                {
                    if (movement.Direction == "IN")
                    {
                        runningBalance += movement.Amount;
                    }
                    else
                    {
                        runningBalance -= movement.Amount;
                    }
                    movement.Balance = runningBalance;
                }

                return allMovements;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GetCashMovementsError",
                    $"Erreur lors de la récupération des mouvements de caisse pour le centre {hospitalCenterId}",
                    details: new { FromDate = fromDate, ToDate = toDate, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Génère un bordereau de remise au format PDF
        /// </summary>
        public async Task<byte[]> GenerateHandoverReceiptAsync(int handoverId)
        {
            try
            {
                var handover = await GetHandoverByIdAsync(handoverId);
                if (handover == null)
                {
                    throw new Exception($"Remise {handoverId} introuvable");
                }

                // TODO: Implémenter la génération du PDF
                // Pour l'instant, on retourne un PDF vide
                return new byte[0];
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GenerateHandoverReceiptError",
                    $"Erreur lors de la génération du bordereau de remise {handoverId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Récupère le solde de caisse courant pour un centre
        /// </summary>
        public async Task<decimal> GetCurrentCashBalanceAsync(int hospitalCenterId)
        {
            try
            {
                // Récupérer la dernière remise
                var lastHandover = await _cashHandoverRepository.QuerySingleAsync(q =>
                    q.Where(h => h.HospitalCenterId == hospitalCenterId)
                     .OrderByDescending(h => h.HandoverDate)
                     .Select(h => new {
                         h.HandoverDate,
                         h.RemainingCashAmount
                     }));

                // Si aucune remise n'a été effectuée, commencer à zéro
                if (lastHandover == null)
                {
                    // Calculer la somme de tous les paiements en espèces
                    return await _paymentRepository.SumAsync(
                        q => q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                       p.PaymentMethod.Name.Contains("Espèces") &&
                                       (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]"))),
                        p => p.Amount);
                }

                // Sinon, prendre le montant restant de la dernière remise
                // et ajouter tous les paiements en espèces depuis cette date
                var cashReceiptsSince = await _paymentRepository.SumAsync(
                    q => q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                  p.PaymentMethod.Name.Contains("Espèces") &&
                                  p.PaymentDate > lastHandover.HandoverDate &&
                                  (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]"))),
                    p => p.Amount);

                return lastHandover.RemainingCashAmount + cashReceiptsSince;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "GetCurrentCashBalanceError",
                    $"Erreur lors de la récupération du solde de caisse pour le centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Calcule les recettes en espèces depuis la dernière remise
        /// </summary>
        public async Task<CashReconciliationViewModel> CalculateCashReceiptsSinceLastHandoverAsync(int hospitalCenterId)
        {
            try
            {
                // Récupérer le centre
                var center = await _hospitalCenterRepository.GetByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    throw new Exception($"Centre hospitalier {hospitalCenterId} introuvable");
                }

                // Récupérer la dernière remise
                var lastHandover = await _cashHandoverRepository.QuerySingleAsync(q =>
                    q.Where(h => h.HospitalCenterId == hospitalCenterId)
                     .OrderByDescending(h => h.HandoverDate)
                     .Select(h => new {
                         h.Id,
                         h.HandoverDate,
                         h.RemainingCashAmount
                     }));

                // Si aucune remise n'a été effectuée, on prend le début des temps
                DateTime lastHandoverDate = lastHandover?.HandoverDate ?? DateTime.MinValue;
                decimal lastHandoverRemainingAmount = lastHandover?.RemainingCashAmount ?? 0;

                // Récupérer tous les paiements en espèces depuis la dernière remise
                var payments = await _paymentRepository.QueryListAsync(q =>
                    q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                               p.PaymentMethod.Name.Contains("Espèces") &&
                               p.PaymentDate > lastHandoverDate &&
                               (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]")))
                     .Select(p => new {
                         p.Id,
                         p.Amount,
                         p.ReferenceType,
                         p.ReferenceId,
                         p.PaymentDate
                     }));

                // Calculer le montant total reçu
                decimal totalReceived = payments.Sum(p => p.Amount);

                // Grouper les paiements par type de référence
                var paymentsByType = payments
                    .GroupBy(p => p.ReferenceType)
                    .Select(g => new CashPaymentSummary
                    {
                        ReferenceType = g.Key,
                        TotalAmount = g.Sum(p => p.Amount),
                        Count = g.Count()
                    })
                    .ToList();

                return new CashReconciliationViewModel
                {
                    HospitalCenterId = hospitalCenterId,
                    HospitalCenterName = center.Name,
                    LastHandoverDate = lastHandoverDate,
                    LastHandoverRemainingAmount = lastHandoverRemainingAmount,
                    TotalCashReceiptsSince = totalReceived,
                    PaymentCount = payments.Count,
                    PaymentDetails = paymentsByType
                };
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "CalculateCashReceiptsSinceLastHandoverError",
                    $"Erreur lors du calcul des recettes en espèces pour le centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Calcule le solde de caisse à une date donnée
        /// </summary>
        private async Task<decimal> CalculateBalanceAtDateAsync(int hospitalCenterId, DateTime date)
        {
            try
            {
                // Trouver la dernière remise avant la date
                var lastHandover = await _cashHandoverRepository.QuerySingleAsync(q =>
                    q.Where(h => h.HospitalCenterId == hospitalCenterId &&
                               h.HandoverDate < date)
                     .OrderByDescending(h => h.HandoverDate)
                     .Select(h => new {
                         h.HandoverDate,
                         h.RemainingCashAmount
                     }));

                // Si aucune remise n'a été trouvée, on commence à zéro
                if (lastHandover == null)
                {
                    // Calculer tous les paiements en espèces jusqu'à la date
                    return await _paymentRepository.SumAsync(
                        q => q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                       p.PaymentMethod.Name.Contains("Espèces") &&
                                       p.PaymentDate < date &&
                                       (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]"))),
                        p => p.Amount);
                }

                // Sinon, prendre le montant restant de la dernière remise
                // et ajouter tous les paiements en espèces entre cette date et la date demandée
                var cashReceiptsSince = await _paymentRepository.SumAsync(
                    q => q.Where(p => p.HospitalCenterId == hospitalCenterId &&
                                  p.PaymentMethod.Name.Contains("Espèces") &&
                                  p.PaymentDate > lastHandover.HandoverDate &&
                                  p.PaymentDate < date &&
                                  (p.Notes == null || !p.Notes.StartsWith("[CANCELLED]"))),
                    p => p.Amount);

                return lastHandover.RemainingCashAmount + cashReceiptsSince;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("CashManagementService", "CalculateBalanceAtDateError",
                    $"Erreur lors du calcul du solde de caisse à la date {date} pour le centre {hospitalCenterId}",
                    details: new { Error = ex.Message });
                throw;
            }
        }
    }
}