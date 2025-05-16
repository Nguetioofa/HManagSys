using HManagSys.Models.EfModels;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Services.Implementations
{
    /// <summary>
    /// Service de mapping pour convertir entre entités EF et ViewModels
    /// Centralise la logique de mapping pour éviter la duplication
    /// </summary>
    public static class StockMappingService
    {
        // ===== PRODUCT CATEGORY MAPPINGS =====

        public static ProductCategoryViewModel MapToViewModel(ProductCategory category, int productCount = 0)
        {
            return new ProductCategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                ProductCount = productCount,
                //CreatedBy = category.CreatedBy,
                CreatedAt = category.CreatedAt,
                //ModifiedBy = category.ModifiedBy,
                ModifiedAt = category.ModifiedAt
            };
        }

        public static ProductCategorySelectViewModel MapToSelectViewModel(ProductCategory category)
        {
            return new ProductCategorySelectViewModel
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            };
        }

        public static ProductCategory MapToEntity(CreateProductCategoryViewModel model, int createdBy)
        {
            return new ProductCategory
            {
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                IsActive = model.IsActive,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateEntity(ProductCategory entity, EditProductCategoryViewModel model, int modifiedBy)
        {
            entity.Name = model.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            entity.IsActive = model.IsActive;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedAt = DateTime.UtcNow;
        }

        // ===== PRODUCT MAPPINGS =====

        public static ProductViewModel MapToViewModel(Product product, int centersWithStock = 0, bool hasLowStock = false, bool hasCriticalStock = false)
        {
            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryName = product.ProductCategory?.Name ?? "",
                ProductCategoryId = product.ProductCategoryId,
                UnitOfMeasure = product.UnitOfMeasure,
                SellingPrice = product.SellingPrice,
                IsActive = product.IsActive,
               // cr = product.CreatedBy,
                CreatedAt = product.CreatedAt,
               // ModifiedBy = product.ModifiedBy,
                ModifiedAt = product.ModifiedAt,
                TotalCentersWithStock = centersWithStock,
                HasLowStock = hasLowStock,
                HasCriticalStock = hasCriticalStock
            };
        }

        public static ProductSelectViewModel MapToSelectViewModel(Product product)
        {
            return new ProductSelectViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryName = product.ProductCategory?.Name ?? "",
                UnitOfMeasure = product.UnitOfMeasure,
                SellingPrice = product.SellingPrice,
                IsActive = product.IsActive
            };
        }

        public static Product MapToEntity(CreateProductViewModel model, int createdBy)
        {
            return new Product
            {
                Name = model.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                ProductCategoryId = model.ProductCategoryId,
                UnitOfMeasure = model.UnitOfMeasure.Trim(),
                SellingPrice = model.SellingPrice,
                IsActive = model.IsActive,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateEntity(Product entity, EditProductViewModel model, int modifiedBy)
        {
            entity.Name = model.Name.Trim();
            entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            entity.ProductCategoryId = model.ProductCategoryId;
            entity.UnitOfMeasure = model.UnitOfMeasure.Trim();
            entity.SellingPrice = model.SellingPrice;
            entity.IsActive = model.IsActive;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedAt = DateTime.UtcNow;
        }

        // ===== STOCK MAPPINGS =====

        public static StockItemViewModel MapToStockItemViewModel(StockInventory stockInventory)
        {
            return new StockItemViewModel
            {
                ProductId = stockInventory.ProductId,
                ProductName = stockInventory.Product?.Name ?? "",
                CategoryName = stockInventory.Product?.ProductCategory?.Name ?? "",
                UnitOfMeasure = stockInventory.Product?.UnitOfMeasure ?? "",
                CurrentQuantity = stockInventory.CurrentQuantity,
                MinimumThreshold = stockInventory.MinimumThreshold,
                MaximumThreshold = stockInventory.MaximumThreshold,
                StockStatus = CalculateStockStatus(stockInventory.CurrentQuantity, stockInventory.MinimumThreshold, stockInventory.MaximumThreshold),
                UnitPrice = stockInventory.Product?.SellingPrice ?? 0,
                LastMovementDate = stockInventory.Product?.StockMovements
                    ?.Where(sm => sm.HospitalCenterId == stockInventory.HospitalCenterId)
                    ?.Max(sm => (DateTime?)sm.MovementDate),
                MovementsLast30Days = stockInventory.Product?.StockMovements
                    ?.Where(sm => sm.HospitalCenterId == stockInventory.HospitalCenterId &&
                                sm.MovementDate >= DateTime.Now.AddDays(-30))
                    ?.Count() ?? 0
            };
        }

        public static ProductStockByCenterViewModel MapToProductStockByCenterViewModel(StockInventory stockInventory)
        {
            return new ProductStockByCenterViewModel
            {
                HospitalCenterId = stockInventory.HospitalCenterId,
                CenterName = stockInventory.HospitalCenter?.Name ?? "",
                CurrentQuantity = stockInventory.CurrentQuantity,
                MinimumThreshold = stockInventory.MinimumThreshold,
                MaximumThreshold = stockInventory.MaximumThreshold,
                StockStatus = CalculateStockStatus(stockInventory.CurrentQuantity, stockInventory.MinimumThreshold, stockInventory.MaximumThreshold),
                LastMovementDate = stockInventory.Product?.StockMovements
                    ?.Where(sm => sm.HospitalCenterId == stockInventory.HospitalCenterId)
                    ?.Max(sm => (DateTime?)sm.MovementDate)
            };
        }

        public static StockAlertViewModel MapToStockAlertViewModel(StockInventory stockInventory, string severity)
        {
            return new StockAlertViewModel
            {
                ProductId = stockInventory.ProductId,
                ProductName = stockInventory.Product?.Name ?? "",
                CategoryName = stockInventory.Product?.ProductCategory?.Name ?? "",
                CurrentQuantity = stockInventory.CurrentQuantity,
                MinimumThreshold = stockInventory.MinimumThreshold,
                UnitOfMeasure = stockInventory.Product?.UnitOfMeasure ?? "",
                Severity = severity,
                LastMovementDate = stockInventory.Product?.StockMovements
                    ?.Where(sm => sm.HospitalCenterId == stockInventory.HospitalCenterId)
                    ?.Max(sm => (DateTime?)sm.MovementDate)
            };
        }

        public static RecentMovementViewModel MapToRecentMovementViewModel(StockMovement movement)
        {
            return new RecentMovementViewModel
            {
                MovementDate = movement.MovementDate,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                CenterName = movement.HospitalCenter?.Name ?? "",
                ReferenceType = movement.ReferenceType,
                ReferenceId = movement.ReferenceId,
                Notes = movement.Notes,
                //CreatedByName = movement.CreatedByNavigation != null
                //    ? $"{movement.CreatedByNavigation.FirstName} {movement.CreatedByNavigation.LastName}"
                //    : "Utilisateur inconnu"
            };
        }

        public static RecentStockMovementViewModel MapToRecentStockMovementViewModel(StockMovement movement)
        {
            return new RecentStockMovementViewModel
            {
                MovementDate = movement.MovementDate,
                ProductName = movement.Product?.Name ?? "",
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                UnitOfMeasure = movement.Product?.UnitOfMeasure ?? "",
                //CreatedByName = movement.CreatedByNavigation != null
                //    ? $"{movement.CreatedByNavigation.FirstName} {movement.CreatedByNavigation.LastName}"
                //    : "Utilisateur inconnu",
                ReferenceType = movement.ReferenceType,
                ReferenceId = movement.ReferenceId
            };
        }

        // ===== HELPER METHODS =====

        /// <summary>
        /// Calcule le statut du stock basé sur les seuils
        /// </summary>
        public static string CalculateStockStatus(decimal currentQuantity, decimal? minThreshold, decimal? maxThreshold)
        {
            if (currentQuantity <= 0)
                return "OutOfStock";

            if (minThreshold.HasValue)
            {
                if (currentQuantity <= minThreshold.Value * 0.5m)
                    return "Critical";
                if (currentQuantity <= minThreshold.Value)
                    return "Low";
            }

            if (maxThreshold.HasValue && currentQuantity >= maxThreshold.Value)
                return "High";

            return "Normal";
        }

        /// <summary>
        /// Mappe les noms des utilisateurs sur les ViewModels
        /// </summary>
        public static void MapUserNames(Dictionary<int, string> userNames, ProductCategoryViewModel viewModel)
        {
            //viewModel.CreatedByName = userNames.GetValueOrDefault(viewModel.CreatedBy, "Utilisateur inconnu");
            //if (viewModel.ModifiedBy.HasValue)
            //{
            //    viewModel.ModifiedByName = userNames.GetValueOrDefault(viewModel.ModifiedBy.Value, "Utilisateur inconnu");
            //}
        }

        /// <summary>
        /// Mappe les noms des utilisateurs sur les ViewModels de produits
        /// </summary>
        public static void MapUserNames(Dictionary<int, string> userNames, ProductViewModel viewModel)
        {
            //viewModel.CreatedByName = userNames.GetValueOrDefault(viewModel.CreatedBy, "Utilisateur inconnu");
            //if (viewModel.ModifiedBy.HasValue)
            //{
            //    viewModel.ModifiedByName = userNames.GetValueOrDefault(viewModel.ModifiedBy.Value, "Utilisateur inconnu");
            //}
        }

        /// <summary>
        /// Crée un dictionnaire de noms d'utilisateurs à partir d'une liste d'entités User
        /// </summary>
        public static Dictionary<int, string> CreateUserNameDictionary(IEnumerable<User> users)
        {
            return users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");
        }

        // ===== STOCK INITIALIZATION MAPPINGS =====

        public static ProductStockInitViewModel MapToProductStockInitViewModel(Product product, StockInventory? existingStock = null)
        {
            return new ProductStockInitViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CategoryName = product.ProductCategory?.Name ?? "",
                UnitOfMeasure = product.UnitOfMeasure,
                HasExistingStock = existingStock != null,
                CurrentQuantity = existingStock?.CurrentQuantity ?? 0,
                InitialQuantity = existingStock?.CurrentQuantity ?? 0,
                MinimumThreshold = existingStock?.MinimumThreshold,
                MaximumThreshold = existingStock?.MaximumThreshold,
                IsEnabled = true
            };
        }

        // ===== COLLECTION MAPPINGS =====

        /// <summary>
        /// Mappe une collection de ProductCategory vers ProductCategoryViewModel avec comptage de produits
        /// </summary>
        public static List<ProductCategoryViewModel> MapCategoryCollectionToViewModels(
            IEnumerable<ProductCategory> categories,
            Dictionary<int, int> productCounts,
            Dictionary<int, string> userNames)
        {
            return categories.Select(c =>
            {
                var viewModel = MapToViewModel(c, productCounts.GetValueOrDefault(c.Id, 0));
                MapUserNames(userNames, viewModel);
                return viewModel;
            }).ToList();
        }

        /// <summary>
        /// Mappe une collection de Product vers ProductViewModel
        /// </summary>
        public static List<ProductViewModel> MapProductCollectionToViewModels(
            IEnumerable<Product> products,
            Dictionary<int, string> userNames)
        {
            return products.Select(p =>
            {
                var viewModel = MapToViewModel(p);
                MapUserNames(userNames, viewModel);
                return viewModel;
            }).ToList();
        }

        /// <summary>
        /// Mappe une collection de StockInventory vers StockItemViewModel
        /// </summary>
        public static List<StockItemViewModel> MapStockInventoryCollectionToViewModels(
            IEnumerable<StockInventory> stockInventories)
        {
            return stockInventories.Select(MapToStockItemViewModel).ToList();
        }
    }
}