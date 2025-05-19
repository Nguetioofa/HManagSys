using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HManagSys.Services.Implementations
{
    /// <summary>
    /// Service pour la gestion des catégories de produits
    /// Implémentation avec logique métier complète et validation
    /// </summary>
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IGenericRepository<ProductCategory> _categoryRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IApplicationLogger _appLogger;
        private readonly IAuditService _auditService;

        public ProductCategoryService(
            IGenericRepository<ProductCategory> categoryRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<User> userRepository,
            IApplicationLogger appLogger,
            IAuditService auditService)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _appLogger = appLogger;
            _auditService = auditService;
        }

        // ===== OPÉRATIONS CRUD =====

        public async Task<(List<ProductCategoryViewModel> Categories, int TotalCount)> GetCategoriesAsync(ProductCategoryFilters filters)
        {
            try
            {
                var totalCount = 0;
                // Construire la requête avec filtres
                var query = await _categoryRepository.QueryListAsync(q =>
                {
                    var baseQuery = q.Include(c => c.Products)
                                     .AsQueryable();

                    // Filtre par recherche
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchLower = filters.SearchTerm.ToLower();
                        baseQuery = baseQuery.Where(c =>
                            c.Name.ToLower().Contains(searchLower) ||
                            (c.Description != null && c.Description.ToLower().Contains(searchLower)));
                    }

                    // Filtre par statut
                    if (filters.IsActive.HasValue)
                    {
                        baseQuery = baseQuery.Where(c => c.IsActive == filters.IsActive.Value);
                    }

                    // Compter le total
                     totalCount = baseQuery.Count();

                    // Pagination et tri
                    var categories = baseQuery
                        .OrderBy(c => c.Name)
                        .Skip((filters.PageIndex - 1) * filters.PageSize)
                        .Take(filters.PageSize)
                        .Select(c => new ProductCategoryViewModel
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Description = c.Description,
                            IsActive = c.IsActive,
                            ProductCount = c.Products.Count(p => p.IsActive),
                            CreatedAt = c.CreatedAt,
                            ModifiedAt = c.ModifiedAt,
                        });

                    return categories;
                });



                return (query.ToList(), totalCount);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetCategories",
                    "Erreur lors de la récupération des catégories",
                    details: new { Filters = filters, Error = ex.Message });
                throw;
            }
        }

        public async Task<ProductCategoryViewModel?> GetCategoryByIdAsync(int id)
        {
            try
            {

                var products = await _categoryRepository.QuerySingleAsync(query =>
                {
                    query = query.Where(p => p.Id == id)
                         .Include(p => p.Products)
                         .AsSplitQuery();

                    return query.Select(x => new ProductCategoryViewModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Description = x.Description,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        ModifiedAt = x.ModifiedAt,
                        ProductCount = x.Products.Count(),
                        Products = x.Products.Select(product => new ProductViewModel
                        {
                            Id = product.Id,
                            Name = product.Name,
                            Description = product.Description,
                            CategoryName = product.ProductCategory.Name,
                            ProductCategoryId = product.ProductCategoryId,
                            UnitOfMeasure = product.UnitOfMeasure,
                            SellingPrice = product.SellingPrice,
                            IsActive = product.IsActive,
                            CreatedAt = product.CreatedAt,
                            ModifiedAt = product.ModifiedAt,
                            TotalWithStock = product.StockInventories.First(x=>x.ProductId == product.Id).CurrentQuantity,
                            TotalCentersWithStock = product.StockInventories.Count(si => si.CurrentQuantity > 0),
                            HasLowStock = product.StockInventories.Any(si =>
                                si.CurrentQuantity <= (si.MinimumThreshold ?? 0) && si.CurrentQuantity > 0),
                            HasCriticalStock = product.StockInventories.Any(si =>
                                si.CurrentQuantity <= ((si.MinimumThreshold ?? 0) * 0.5m))
                        }).ToList()
                    });

                });

                return products;
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetCategoryById",
                    $"Erreur lors de la récupération de la catégorie {id}",
                    details: new { CategoryId = id, Error = ex.Message });
                throw;
            }
        }

        public async Task<OperationResult<ProductCategoryViewModel>> CreateCategoryAsync(
            CreateProductCategoryViewModel model,
            int createdBy)
        {
            try
            {
                // Validation
                var validation = await ValidateCategoryAsync(model.Name, model.Description);
                if (!validation.IsValid)
                {
                    return OperationResult<ProductCategoryViewModel>.ValidationError(validation.Errors);
                }

                // Créer l'entité
                var category = new ProductCategory
                {
                    Name = model.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    IsActive = model.IsActive,
                    CreatedBy = createdBy,
                    CreatedAt = TimeZoneHelper.GetCameroonTime()
                };

                var createdCategory = await _categoryRepository.AddAsync(category);

                // Audit
                await _auditService.LogActionAsync(
                    createdBy,
                    "CREATE",
                    "ProductCategory",
                    createdCategory.Id,
                    null,
                    new { Name = category.Name, IsActive = category.IsActive },
                    $"Création de la catégorie '{category.Name}'"
                );

                // Log applicatif
                await _appLogger.LogInfoAsync("Stock", "CategoryCreated",
                    $"Catégorie créée : {category.Name}",
                    createdBy,
                    details: new { CategoryId = createdCategory.Id });

                // Retourner le ViewModel
                var result = await GetCategoryByIdAsync(createdCategory.Id);
                return OperationResult<ProductCategoryViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "CreateCategory",
                    "Erreur lors de la création de la catégorie",
                    createdBy,
                    details: new { Model = model, Error = ex.Message });
                return OperationResult<ProductCategoryViewModel>.Error($"Erreur lors de la création : {ex.Message}");
            }
        }

        public async Task<OperationResult<ProductCategoryViewModel>> UpdateCategoryAsync(
            int id,
            EditProductCategoryViewModel model,
            int modifiedBy)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return OperationResult<ProductCategoryViewModel>.Error("Catégorie introuvable");
                }

                // Validation
                var validation = await ValidateCategoryAsync(model.Name, model.Description, id);
                if (!validation.IsValid)
                {
                    return OperationResult<ProductCategoryViewModel>.ValidationError(validation.Errors);
                }

                // Sauvegarder les anciennes valeurs pour l'audit
                var oldValues = new
                {
                    Name = existingCategory.Name,
                    Description = existingCategory.Description,
                    IsActive = existingCategory.IsActive
                };

                // Mettre à jour
                existingCategory.Name = model.Name.Trim();
                existingCategory.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
                existingCategory.IsActive = model.IsActive;
                existingCategory.ModifiedBy = modifiedBy;
                existingCategory.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _categoryRepository.UpdateAsync(existingCategory);

                // Audit
                var newValues = new
                {
                    Name = existingCategory.Name,
                    Description = existingCategory.Description,
                    IsActive = existingCategory.IsActive
                };

                await _auditService.LogActionAsync(
                    modifiedBy,
                    "UPDATE",
                    "ProductCategory",
                    id,
                    oldValues,
                    newValues,
                    $"Modification de la catégorie '{existingCategory.Name}'"
                );

                // Log applicatif
                await _appLogger.LogInfoAsync("Stock", "CategoryUpdated",
                    $"Catégorie modifiée : {existingCategory.Name}",
                    modifiedBy,
                    details: new { CategoryId = id });

                // Retourner le ViewModel mis à jour
                var result = await GetCategoryByIdAsync(id);
                return OperationResult<ProductCategoryViewModel>.Success(result!);
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "UpdateCategory",
                    $"Erreur lors de la modification de la catégorie {id}",
                    modifiedBy,
                    details: new { CategoryId = id, Model = model, Error = ex.Message });
                return OperationResult<ProductCategoryViewModel>.Error($"Erreur lors de la modification : {ex.Message}");
            }
        }

        public async Task<OperationResult> DeleteCategoryAsync(int id, int deletedBy)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return OperationResult.Error("Catégorie introuvable");
                }

                // Vérifier si la catégorie peut être supprimée
                var canDelete = await CanDeleteCategoryAsync(id);
                if (!canDelete)
                {
                    return OperationResult.Error("Impossible de supprimer cette catégorie car elle contient des produits");
                }

                // Soft delete (désactiver)
                category.IsActive = false;
                category.ModifiedBy = deletedBy;
                category.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _categoryRepository.UpdateAsync(category);

                // Audit
                await _auditService.LogActionAsync(
                    deletedBy,
                    "DELETE",
                    "ProductCategory",
                    id,
                    new { Name = category.Name, IsActive = true },
                    new { Name = category.Name, IsActive = false },
                    $"Suppression (désactivation) de la catégorie '{category.Name}'"
                );

                // Log applicatif
                await _appLogger.LogInfoAsync("Stock", "CategoryDeleted",
                    $"Catégorie supprimée : {category.Name}",
                    deletedBy,
                    details: new { CategoryId = id });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "DeleteCategory",
                    $"Erreur lors de la suppression de la catégorie {id}",
                    deletedBy,
                    details: new { CategoryId = id, Error = ex.Message });
                return OperationResult.Error($"Erreur lors de la suppression : {ex.Message}");
            }
        }

        public async Task<OperationResult> ToggleCategoryStatusAsync(int id, bool isActive, int modifiedBy)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return OperationResult.Error("Catégorie introuvable");
                }

                var oldValue = category.IsActive;
                category.IsActive = isActive;
                category.ModifiedBy = modifiedBy;
                category.ModifiedAt = TimeZoneHelper.GetCameroonTime();

                await _categoryRepository.UpdateAsync(category);

                // Audit
                await _auditService.LogActionAsync(
                    modifiedBy,
                    "STATUS_CHANGE",
                    "ProductCategory",
                    id,
                    new { IsActive = oldValue },
                    new { IsActive = isActive },
                    $"Changement de statut de la catégorie '{category.Name}' : {(isActive ? "activée" : "désactivée")}"
                );

                // Log applicatif
                await _appLogger.LogInfoAsync("Stock", "CategoryStatusChanged",
                    $"Statut de la catégorie modifié : {category.Name} -> {(isActive ? "activée" : "désactivée")}",
                    modifiedBy,
                    details: new { CategoryId = id, NewStatus = isActive });

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "ToggleCategoryStatus",
                    $"Erreur lors du changement de statut de la catégorie {id}",
                    modifiedBy,
                    details: new { CategoryId = id, IsActive = isActive, Error = ex.Message });
                return OperationResult.Error($"Erreur lors du changement de statut : {ex.Message}");
            }
        }

        // ===== REQUÊTES SPÉCIALISÉES =====

        public async Task<List<ProductCategorySelectViewModel>> GetActiveCategoriesForSelectAsync()
        {
            return (await _categoryRepository.QueryListAsync<ProductCategorySelectViewModel>(q =>
                q.Where(c => c.IsActive)
                 .OrderBy(c => c.Name)
                 .Select(c => new ProductCategorySelectViewModel
                 {
                     Id = c.Id,
                     Name = c.Name,
                     IsActive = c.IsActive
                 }))).ToList();
        }

        public async Task<bool> CanDeleteCategoryAsync(int id)
        {
            // Une catégorie peut être supprimée si elle n'a pas de produits actifs
            var productCount = await _productRepository.CountAsync(q =>
                q.Where(p => p.ProductCategoryId == id && p.IsActive));
            return productCount == 0;
        }

        public async Task<CategoryStatistics> GetCategoryStatisticsAsync()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                var products = await _productRepository.GetAllAsync();

                return new CategoryStatistics
                {
                    TotalCategories = categories.Count,
                    ActiveCategories = categories.Count(c => c.IsActive),
                    InactiveCategories = categories.Count(c => !c.IsActive),
                    TotalProducts = products.Count(p => p.IsActive)
                };
            }
            catch (Exception ex)
            {
                await _appLogger.LogErrorAsync("Stock", "GetCategoryStatistics",
                    "Erreur lors du calcul des statistiques des catégories",
                    details: new { Error = ex.Message });
                throw;
            }
        }

        public async Task<List<ProductCategorySelectViewModel>> SearchCategoriesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveCategoriesForSelectAsync();

            var searchLower = searchTerm.ToLower();
            return (await _categoryRepository.QueryListAsync<ProductCategorySelectViewModel>(q =>
                q.Where(c => c.IsActive && c.Name.ToLower().Contains(searchLower))
                 .OrderBy(c => c.Name)
                 .Select(c => new ProductCategorySelectViewModel
                 {
                     Id = c.Id,
                     Name = c.Name,
                     IsActive = c.IsActive
                 }))).ToList();
        }

        public async Task<List<CategoryUsageViewModel>> GetMostUsedCategoriesAsync(int limit = 10)
        {
            return (await _categoryRepository.QueryListAsync<CategoryUsageViewModel>(q =>
                q.Where(c => c.IsActive)
                 .Select(c => new CategoryUsageViewModel
                 {
                     CategoryId = c.Id,
                     CategoryName = c.Name,
                     ProductCount = c.Products.Count(p => p.IsActive),
                     TotalStockMovements = c.Products
                         .SelectMany(p => p.StockMovements)
                         .Count(),
                     TotalStockValue = c.Products
                         .Where(p => p.IsActive)
                         .SelectMany(p => p.StockInventories)
                         .Sum(si => si.CurrentQuantity /** si.SellingPrice*/),
                     LastUsed = c.Products
                         .SelectMany(p => p.StockMovements)
                         .Max(sm => (DateTime?)sm.MovementDate) ?? c.CreatedAt
                 })
                 .OrderByDescending(cu => cu.ProductCount)
                 .ThenByDescending(cu => cu.TotalStockMovements)
                 .Take(limit))).ToList();
        }

        // ===== VALIDATION =====

        public async Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null)
        {
            var nameLower = name.ToLower().Trim();
            return !await _categoryRepository.AnyAsync(q =>
                q.Where(c => c.Name.ToLower() == nameLower && c.Id != (excludeId ?? 0)));
        }

        public async Task<ValidationResult> ValidateCategoryAsync(
            string name,
            string? description,
            int? excludeId = null)
        {
            var errors = new List<string>();

            // Validation du nom
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Le nom de la catégorie est requis");
            }
            else
            {
                if (name.Trim().Length > 100)
                    errors.Add("Le nom ne peut pas dépasser 100 caractères");

                // Vérifier l'unicité
                var isUnique = await IsCategoryNameUniqueAsync(name, excludeId);
                if (!isUnique)
                    errors.Add("Une catégorie avec ce nom existe déjà");
            }

            // Validation de la description
            if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 500)
            {
                errors.Add("La description ne peut pas dépasser 500 caractères");
            }

            return errors.Any()
                ? ValidationResult.Invalid(errors.ToArray())
                : ValidationResult.Valid();
        }

        // ===== MÉTHODES PRIVÉES =====

        private async Task LoadCreatorNamesAsync(List<ProductCategoryViewModel> categories)
        {
            //if (!categories.Any()) return;

            //// Récupérer tous les IDs d'utilisateurs uniques
            //var userIds = categories
            //    .Select(c => c.CreatedByName)
            //    .Union(categories.Where(c => c.ModifiedByName).Select(c => c.ModifiedBy!.Value))
            //    .Distinct()
            //    .ToList();

            //// Charger les utilisateurs
            //var users = await _userRepository.GetByIdsAsync(userIds);
            //var userDict = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

            //// Mapper les noms
            //foreach (var category in categories)
            //{
            //    category.CreatedByName = userDict.GetValueOrDefault(category.CreatedBy, "Utilisateur inconnu");
            //    if (category.ModifiedBy.HasValue)
            //    {
            //        category.ModifiedByName = userDict.GetValueOrDefault(category.ModifiedBy.Value, "Utilisateur inconnu");
            //    }
            //}
        }
    }
}