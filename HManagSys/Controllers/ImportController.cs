using HManagSys.Attributes;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HManagSys.Controllers
{
    [RequireAuthentication]
    [RequireCurrentCenter]
    public class ImportController : BaseController
    {
        private readonly IProductExcelService _productExcelService;
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IApplicationLogger _logger;

        public ImportController(
            IProductExcelService productExcelService,
            IProductService productService,
            IStockService stockService,
            IApplicationLogger logger)
        {
            _productExcelService = productExcelService;
            _productService = productService;
            _stockService = stockService;
            _logger = logger;
        }

        /// <summary>
        /// Affiche la page d'importation de produits et stock
        /// </summary>
        [SuperAdmin]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Télécharge le modèle Excel pour l'importation de produits et stock
        /// </summary>
        [HttpGet]
        [SuperAdmin]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                if (!CurrentCenterId.HasValue)
                {
                    TempData["ErrorMessage"] = "Veuillez sélectionner un centre hospitalier.";
                    return RedirectToAction("Index");
                }

                var excelBytes = await _productExcelService.GenerateImportTemplate(CurrentCenterId.Value);

                // Log du téléchargement
                await _logger.LogInfoAsync("Import", "TemplateDownloaded",
                    "Téléchargement du modèle Excel d'importation",
                    CurrentUserId, CurrentCenterId);

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Import_Produits_{CurrentCenterName}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Import", "TemplateDownloadError",
                    "Erreur lors de la génération du modèle Excel",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = "Erreur lors de la génération du modèle Excel.";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Importe les données d'un fichier Excel
        /// </summary>
        [HttpPost]
        [SuperAdmin]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner un fichier Excel.";
                return RedirectToAction("Index");
            }

            if (!CurrentCenterId.HasValue)
            {
                TempData["ErrorMessage"] = "Veuillez sélectionner un centre hospitalier.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // Traiter le fichier Excel
                    var (products, stockEntries, errors) = await _productExcelService.ProcessImportedExcel(stream, CurrentCenterId.Value);

                    // Vérifier s'il y a des erreurs bloquantes
                    if (errors.Count > 0)
                    {
                        // Stocker les erreurs dans TempData pour affichage
                        TempData["ImportErrors"] = errors;
                        TempData["ErrorMessage"] = $"L'importation a échoué avec {errors.Count} erreur(s).";

                        // Log des erreurs
                        await _logger.LogWarningAsync("Import", "ImportValidationFailed",
                            $"Validation de l'import Excel échouée avec {errors.Count} erreur(s)",
                            CurrentUserId, CurrentCenterId,
                            details: new { ErrorCount = errors.Count, Errors = errors });

                        return RedirectToAction("Index");
                    }

                    // Importer les données validées
                    int productsCreated = 0;
                    int productsUpdated = 0;
                    int stockEntriesCreated = 0;

                    // 1. Créer/mettre à jour les produits
                    foreach (var product in products)
                    {
                        // Vérifier si le produit existe déjà
                        var existingProduct = await _productService.GetProductByNameAsync(product.Name);

                        if (existingProduct == null)
                        {
                            // Créer un nouveau produit
                            var createProductModel = new CreateProductViewModel
                            {
                                Name = product.Name,
                                Description = product.Description,
                                ProductCategoryId = product.CategoryId,
                                UnitOfMeasure = product.UnitOfMeasure,
                                SellingPrice = product.SellingPrice,
                                IsActive = product.IsActive
                            };

                            var result = await _productService.CreateProductAsync(createProductModel, CurrentUserId.Value);

                            if (result.IsSuccess)
                            {
                                productsCreated++;
                            }
                            else
                            {
                                errors.Add($"Erreur lors de la création du produit '{product.Name}': {result.ErrorMessage ?? "Erreur inconnue"}");
                            }
                        }
                        else
                        {
                            // Mettre à jour le produit existant
                            var updateProductModel = new EditProductViewModel
                            {
                                Id = existingProduct.Id,
                                Name = product.Name,
                                Description = product.Description,
                                ProductCategoryId = product.CategoryId,
                                UnitOfMeasure = product.UnitOfMeasure,
                                SellingPrice = product.SellingPrice,
                                IsActive = product.IsActive
                            };

                            var result = await _productService.UpdateProductAsync(existingProduct.Id, updateProductModel, CurrentUserId.Value);

                            if (result.IsSuccess)
                            {
                                productsUpdated++;
                            }
                            else
                            {
                                errors.Add($"Erreur lors de la mise à jour du produit '{product.Name}': {result.ErrorMessage ?? "Erreur inconnue"}");
                            }
                        }
                    }

                    // 2. Créer les entrées de stock
                    foreach (var stockEntry in stockEntries)
                    {
                        int productId;

                        if (stockEntry.ProductId.HasValue)
                        {
                            // Produit existant
                            productId = stockEntry.ProductId.Value;
                        }
                        else
                        {
                            // Nouveau produit - trouver son ID
                            var product = await _productService.GetProductByNameAsync(stockEntry.ProductName);

                            if (product == null)
                            {
                                errors.Add($"Erreur lors de la création de l'entrée de stock: Produit '{stockEntry.ProductName}' introuvable");
                                continue;
                            }

                            productId = product.Id;
                        }

                        // Créer l'entrée de stock
                        var stockMovementRequest = new StockMovementRequest
                        {
                            ProductId = productId,
                            HospitalCenterId = stockEntry.HospitalCenterId,
                            MovementType = stockEntry.EntryType,
                            Quantity = stockEntry.Quantity,
                            ReferenceType = "Import",
                            Notes = $"{stockEntry.Notes} | Lot: {stockEntry.BatchNumber} | Fournisseur: {stockEntry.Supplier}",
                            MovementDate = stockEntry.EntryDate,
                            CreatedBy = CurrentUserId.Value
                        };

                        var result = await _stockService.RecordMovementAsync(stockMovementRequest);

                        if (result.IsSuccess)
                        {
                            stockEntriesCreated++;
                        }
                        else
                        {
                            errors.Add($"Erreur lors de la création de l'entrée de stock pour '{stockEntry.ProductName}': {result.ErrorMessage ?? "Erreur inconnue"}");
                        }
                    }

                    // Vérifier s'il y a eu des erreurs d'importation
                    if (errors.Count > 0)
                    {
                        TempData["ImportErrors"] = errors;
                        TempData["WarningMessage"] = $"L'importation a réussi partiellement. {errors.Count} erreur(s) sont survenues.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "L'importation a réussi.";
                    }

                    // Résumé de l'importation
                    TempData["ImportSummary"] = new[]
                    {
                        $"Produits créés: {productsCreated}",
                        $"Produits mis à jour: {productsUpdated}",
                        $"Entrées en stock créées: {stockEntriesCreated}"
                    };

                    // Log du succès
                    await _logger.LogInfoAsync("Import", "ImportSuccess",
                        "Importation Excel réussie",
                        CurrentUserId, CurrentCenterId,
                        details: new
                        {
                            ProductsCreated = productsCreated,
                            ProductsUpdated = productsUpdated,
                            StockEntriesCreated = stockEntriesCreated,
                            ErrorCount = errors.Count
                        });

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Import", "ImportError",
                    "Erreur lors de l'importation Excel",
                    CurrentUserId, CurrentCenterId,
                    details: new { Error = ex.Message });

                TempData["ErrorMessage"] = $"Erreur lors de l'importation: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}