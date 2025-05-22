using ClosedXML.Excel;
using HManagSys.Models.ViewModels.Stock;
using HManagSys.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HManagSys.Services.Implementations
{
    public class ProductExcelService : IProductExcelService
    {
        private readonly IProductCategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IHospitalCenterService _centerService;
        private readonly IApplicationLogger _logger;

        public ProductExcelService(
            IProductCategoryService categoryService,
            IProductService productService,
            IHospitalCenterService centerService,
            IApplicationLogger logger)
        {
            _categoryService = categoryService;
            _productService = productService;
            _centerService = centerService;
            _logger = logger;
        }

        /// <summary>
        /// Génère un modèle Excel pour l'importation des produits et entrées de stock
        /// </summary>
        public async Task<byte[]> GenerateImportTemplate(int hospitalCenterId)
        {
            try
            {
                // Récupérer les données de référence
                var categories = await _categoryService.GetActiveCategoriesForSelectAsync();
                var center = await _centerService.GetCenterByIdAsync(hospitalCenterId);

                if (center == null)
                {
                    throw new ArgumentException($"Centre hospitalier avec ID {hospitalCenterId} introuvable");
                }

                using (var workbook = new XLWorkbook())
                {
                    // 1. Créer la feuille d'instructions
                    var instructionSheet = workbook.Worksheets.Add("Instructions");
                    CreateInstructionsSheet(instructionSheet, center.Name);

                    // 2. Créer la feuille de références
                    var referenceSheet = workbook.Worksheets.Add("References");
                    CreateReferenceSheet(referenceSheet, categories);

                    // 3. Créer la feuille pour nouveaux produits
                    var productSheet = workbook.Worksheets.Add("Nouveaux Produits");
                    CreateProductSheet(productSheet);

                    // 4. Créer la feuille pour entrées de stock
                    var stockSheet = workbook.Worksheets.Add("Entrées Stock");
                    await CreateStockSheetAsync(stockSheet, center.Name, hospitalCenterId);

                    // Définir la feuille d'instructions comme active
                    workbook.Worksheet("Instructions").SetTabActive();

                    // Exporter le workbook en mémoire
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ProductExcelService", "GenerateImportTemplateFailed",
                    $"Erreur lors de la génération du modèle Excel",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });
                throw;
            }
        }

        /// <summary>
        /// Crée la feuille d'instructions
        /// </summary>
        private void CreateInstructionsSheet(IXLWorksheet sheet, string centerName)
        {
            // En-tête principal
            sheet.Cell("A1").Value = "Instructions pour l'importation des produits et entrées en stock";
            sheet.Range("A1:J1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 14;
            sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Centre hospitalier
            sheet.Cell("A3").Value = "Centre hospitalier:";
            sheet.Cell("B3").Value = centerName;
            sheet.Cell("B3").Style.Font.Bold = true;

            // Instructions générales
            sheet.Cell("A5").Value = "Présentation générale:";
            sheet.Cell("A5").Style.Font.Bold = true;
            sheet.Range("A5:F5").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            sheet.Cell("A6").Value = "• Ce fichier vous permet d'importer des produits et d'enregistrer des entrées en stock.";
            sheet.Cell("A7").Value = "• La feuille 'Nouveaux Produits' permet de définir de nouveaux produits à ajouter au système.";
            sheet.Cell("A8").Value = "• La feuille 'Entrées Stock' permet d'enregistrer les entrées en stock pour des produits existants ou nouveaux.";
            sheet.Cell("A9").Value = "• La feuille 'References' contient les listes de valeurs utilisées dans les menus déroulants.";

            // Instructions pour les nouveaux produits
            sheet.Cell("A11").Value = "Instructions pour la feuille 'Nouveaux Produits':";
            sheet.Cell("A11").Style.Font.Bold = true;
            sheet.Range("A11:F11").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            sheet.Cell("A12").Value = "1. Ajoutez un produit par ligne avec toutes les informations requises.";
            sheet.Cell("A13").Value = "2. Le nom du produit et la catégorie sont obligatoires.";
            sheet.Cell("A14").Value = "3. Sélectionnez une catégorie dans la liste déroulante.";
            sheet.Cell("A15").Value = "4. Sélectionnez une unité de mesure dans la liste déroulante ou saisissez-en une nouvelle.";
            sheet.Cell("A16").Value = "5. Le prix de vente doit être un nombre positif.";
            sheet.Cell("A17").Value = "6. La description est facultative mais recommandée.";

            // Instructions pour les entrées de stock
            sheet.Cell("A19").Value = "Instructions pour la feuille 'Entrées Stock':";
            sheet.Cell("A19").Style.Font.Bold = true;
            sheet.Range("A19:F19").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            sheet.Cell("A20").Value = "1. Pour les produits existants, saisissez l'ID du produit ou laissez vide pour les nouveaux produits.";
            sheet.Cell("A21").Value = "2. Pour les nouveaux produits, le nom doit correspondre exactement à celui saisi dans la feuille 'Nouveaux Produits'.";
            sheet.Cell("A22").Value = "3. La quantité entrée doit être un nombre positif.";
            sheet.Cell("A23").Value = "4. La date d'entrée est obligatoire (format JJ/MM/AAAA).";
            sheet.Cell("A24").Value = "5. Le numéro de lot et la date d'expiration sont facultatifs mais recommandés pour les médicaments.";
            sheet.Cell("A25").Value = "6. Les notes sont facultatives et peuvent contenir toute information complémentaire.";

            // Conseils et astuces
            sheet.Cell("A27").Value = "Conseils et astuces:";
            sheet.Cell("A27").Style.Font.Bold = true;
            sheet.Range("A27:F27").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            sheet.Cell("A28").Value = "• Enregistrez régulièrement votre travail.";
            sheet.Cell("A29").Value = "• Vérifiez les données avant de les importer pour éviter les erreurs.";
            sheet.Cell("A30").Value = "• L'importation crée automatiquement les produits et les entrées de stock dans le centre hospitalier sélectionné.";
            sheet.Cell("A31").Value = "• Les produits existants ne seront pas dupliqués, seules leurs informations pourront être mises à jour.";
            sheet.Cell("A32").Value = "• En cas d'erreur lors de l'importation, vérifiez les messages d'erreur et corrigez les données.";

            // Avertissements
            sheet.Cell("A34").Value = "Important:";
            sheet.Cell("A34").Style.Font.Bold = true;
            sheet.Cell("A34").Style.Font.FontColor = XLColor.Red;
            sheet.Range("A34:F34").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            sheet.Cell("A35").Value = "• Ne modifiez pas la structure de ce fichier (noms des feuilles, en-têtes, etc.).";
            sheet.Cell("A35").Style.Font.FontColor = XLColor.Red;
            sheet.Cell("A36").Value = "• N'utilisez pas de caractères spéciaux ou symboles dans les identifiants.";
            sheet.Cell("A36").Style.Font.FontColor = XLColor.Red;
            sheet.Cell("A37").Value = "• Assurez-vous que les noms des produits sont uniques dans le système.";
            sheet.Cell("A37").Style.Font.FontColor = XLColor.Red;

            // Ajuster automatiquement la largeur des colonnes
            sheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Crée la feuille de références
        /// </summary>
        private void CreateReferenceSheet(IXLWorksheet sheet, List<ProductCategorySelectViewModel> categories)
        {
            // En-tête principal
            sheet.Cell("A1").Value = "Données de référence";
            sheet.Range("A1:F1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 14;
            sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Instructions
            sheet.Cell("A2").Value = "Cette feuille contient les listes de valeurs utilisées par les menus déroulants. Ne pas modifier.";
            sheet.Range("A2:F2").Merge();
            sheet.Cell("A2").Style.Font.Italic = true;

            // Liste des catégories
            sheet.Cell("A4").Value = "Catégories de produits";
            sheet.Cell("A4").Style.Font.Bold = true;

            sheet.Cell("A5").Value = "ID#Nom";
            sheet.Cell("A5").Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 6;
            foreach (var category in categories)
            {
                // Format ID#Nom pour les catégories
                sheet.Cell($"A{row}").Value = $"{category.Id}#{category.Name}";
                row++;
            }

            // Nommer la plage pour les références dans les validations
            var categoriesRange = sheet.Range($"A6:A{row - 1}");
            categoriesRange.AddToNamed("CategoriesList", XLScope.Workbook);

            // Liste des unités de mesure
            sheet.Cell("D4").Value = "Unités de mesure";
            sheet.Cell("D4").Style.Font.Bold = true;
            sheet.Cell("D5").Value = "Code";
            sheet.Cell("E5").Value = "Description";
            sheet.Range("D5:E5").Style.Fill.BackgroundColor = XLColor.LightGray;

            // Liste des unités de mesure
            string[] unitCodes = new[] {
                "Comprimé", "Gélule", "Capsule", "Ampoule", "Flacon", "Tube", "Sachet", "Pot", "Unité",
                "Boîte", "Kit", "Paire", "Spray", "Patch", "Rouleau", "Seringue", "Suppositoire", "Ovule",
                "Poche", "Plaquette", "Bouteille", "ml", "L", "g", "kg", "mg", "mcg", "UI", "Dose", "Paquet"
            };

            string[] unitDescriptions = new[] {
                "Comprimé (cp)", "Gélule", "Capsule", "Ampoule injectable", "Flacon", "Tube (pommade/gel)", "Sachet (poudre/granules)", "Pot",
                "Unité individuelle", "Boîte (conditionnement)", "Kit complet", "Paire (gants, etc.)", "Spray/Pulvérisation", "Patch transdermique",
                "Rouleau (bandage, etc.)", "Seringue pré-remplie", "Suppositoire", "Ovule vaginal", "Poche (perfusion)", "Plaquette (comprimés)",
                "Bouteille", "Millilitre", "Litre", "Gramme", "Kilogramme", "Milligramme", "Microgramme", "Unité Internationale", "Dose unitaire", "Paquet"
            };

            row = 6;
            for (int i = 0; i < unitCodes.Length; i++)
            {
                sheet.Cell($"D{row}").Value = unitCodes[i];
                sheet.Cell($"E{row}").Value = unitDescriptions[i];
                row++;
            }

            // Nommer la plage pour les références dans les validations
            var unitsRange = sheet.Range($"D6:D{row - 1}");
            unitsRange.AddToNamed("UnitsList", XLScope.Workbook);

            // Ajuster automatiquement la largeur des colonnes
            sheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Crée la feuille pour les nouveaux produits
        /// </summary>
        private void CreateProductSheet(IXLWorksheet sheet)
        {
            // En-tête principal
            sheet.Cell("A1").Value = "Enregistrement de nouveaux produits";
            sheet.Range("A1:H1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 14;
            sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Instructions
            sheet.Cell("A2").Value = "Complétez ce tableau pour ajouter de nouveaux produits. Les champs marqués d'un astérisque (*) sont obligatoires.";
            sheet.Range("A2:H2").Merge();
            sheet.Cell("A2").Style.Font.Italic = true;

            // En-têtes des colonnes
            sheet.Cell("A4").Value = "Nom du produit*";
            sheet.Cell("B4").Value = "Description";
            sheet.Cell("C4").Value = "Catégorie*";
            sheet.Cell("D4").Value = "Unité de mesure*";
            sheet.Cell("E4").Value = "Prix de vente*";
            sheet.Cell("F4").Value = "Actif";
            sheet.Cell("G4").Value = "Notes";

            // Style pour les en-têtes
            var headerRange = sheet.Range("A4:G4");
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Marquer les champs obligatoires
            var requiredCells = new[] { "A4", "C4", "D4", "E4" };
            foreach (var cell in requiredCells)
            {
                sheet.Cell(cell).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
            }

            // Exemple de ligne
            sheet.Cell("A5").Value = "Paracétamol 500mg";
            sheet.Cell("B5").Value = "Analgésique et antipyrétique";
            sheet.Cell("C5").Value = "1#Médicaments"; // Catégorie au format ID#Nom
            sheet.Cell("D5").Value = "Comprimé";
            sheet.Cell("E5").Value = 1500; // Prix en FCFA
            sheet.Cell("F5").Value = true;
            sheet.Cell("G5").Value = "Boîte de 20 comprimés";

            // Style pour l'exemple
            var exampleRange = sheet.Range("A5:G5");
            exampleRange.Style.Font.Italic = true;
            exampleRange.Style.Fill.BackgroundColor = XLColor.LightYellow;

            // Validation des données

            // 1. Préparer les données pour la liste déroulante des catégories au format ID#Nom
            for (int row = 6; row <= sheet.LastRowUsed().RowNumber(); row++)
            {
                var id = sheet.Cell($"A{row}").GetValue<int>();
                var name = sheet.Cell($"B{row}").GetValue<string>();
                sheet.Cell($"A{row}").Value = $"{id}#{name}";
            }

            // 2. Liste déroulante pour la catégorie (ID#Nom)
            var categoryColumn = sheet.Range("C6:C1000");
            var categoryValidation = categoryColumn.CreateDataValidation();
            categoryValidation.List("=CategoriesList", true);
            categoryValidation.ErrorMessage = "Veuillez sélectionner une catégorie valide";
            categoryValidation.ErrorStyle = XLErrorStyle.Stop;
            categoryValidation.IgnoreBlanks = false;

            // 3. Liste déroulante pour l'unité de mesure
            var unitColumn = sheet.Range("D6:D1000");
            var unitValidation = unitColumn.CreateDataValidation();
            unitValidation.List("=UnitsList", true);
            unitValidation.ErrorMessage = "Veuillez sélectionner une unité de mesure valide ou en saisir une nouvelle";
            unitValidation.ErrorStyle = XLErrorStyle.Information;
            unitValidation.IgnoreBlanks = false;

            // 4. Validation du prix (nombre positif)
            var priceColumn = sheet.Range("E6:E1000");
            var priceValidation = priceColumn.CreateDataValidation();
            priceValidation.Decimal.GreaterThan(0);
            priceValidation.ErrorMessage = "Le prix doit être un nombre positif";
            priceValidation.ErrorStyle = XLErrorStyle.Stop;
            priceValidation.IgnoreBlanks = false;

            // 5. Validation booléenne pour actif (VRAI/FAUX)
            var activeColumn = sheet.Range("F6:F1000");
            var activeValidation = activeColumn.CreateDataValidation();
            //activeValidation.Boolean();
            activeValidation.ErrorMessage = "Veuillez saisir VRAI ou FAUX";
            activeValidation.ErrorStyle = XLErrorStyle.Information;
            activeValidation.IgnoreBlanks = true;

            // Ajouter des commentaires d'aide
            sheet.Cell("C4").CreateComment().AddText("Sélectionnez la catégorie dans la liste déroulante (format ID#Nom)");
            sheet.Cell("D4").CreateComment().AddText("Sélectionnez une unité de mesure dans la liste ou saisissez-en une nouvelle");
            sheet.Cell("E4").CreateComment().AddText("Prix en FCFA (nombre positif)");
            sheet.Cell("F4").CreateComment().AddText("VRAI pour actif, FAUX pour inactif");

            // Figer les volets
            sheet.SheetView.FreezeRows(4);
            sheet.SheetView.FreezeColumns(1);

            // Ajuster automatiquement la largeur des colonnes
            sheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Crée la feuille pour les entrées de stock avec la liste de tous les produits actifs
        /// </summary>
        private async Task<IXLWorksheet> CreateStockSheetAsync(IXLWorksheet workbook, string centerName, int hospitalCenterId)
        {
            var sheet = workbook/*.Add("Entrées Stock")*/;

            // En-tête principal
            sheet.Cell("A1").Value = "Entrées en stock - " + centerName;
            sheet.Range("A1:F1").Merge();
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 14;
            sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Instructions
            sheet.Cell("A2").Value = "Remplissez la quantité pour chaque produit à ajouter en stock. Les produits avec quantité 0 ou vide seront ignorés.";
            sheet.Range("A2:F2").Merge();
            sheet.Cell("A2").Style.Font.Italic = true;

            // Ajouter l'ID du centre en cellule cachée (pour référence lors du traitement)
            sheet.Cell("F3").Value = hospitalCenterId;
            //sheet.Cell("F3").Style.Visibility = XLCellVisibility.Hidden;

            // En-têtes des colonnes
            sheet.Cell("A4").Value = "ID Produit";
            sheet.Cell("B4").Value = "Nom du produit";
            sheet.Cell("C4").Value = "Quantité*";
            sheet.Cell("D4").Value = "Type d'entrée*";
            sheet.Cell("E4").Value = "Seuil minimum";
            sheet.Cell("F4").Value = "Seuil maximum";

            // Style pour les en-têtes
            var headerRange = sheet.Range("A4:F4");
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Marquer les champs obligatoires
            var requiredCells = new[] { "C4", "D4" };
            foreach (var cell in requiredCells)
            {
                sheet.Cell(cell).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
            }

            // Récupérer tous les produits actifs
            var products = await _productService.GetActiveProductsForSelectAsync();

            // Remplir le tableau avec tous les produits actifs
            int row = 5;
            foreach (var product in products)
            {
                sheet.Cell($"A{row}").Value = product.Id;
                sheet.Cell($"B{row}").Value = product.Name;
                sheet.Cell($"C{row}").Value = 0; // Quantité à remplir par l'utilisateur
                sheet.Cell($"D{row}").Value = "Initial"; // Type d'entrée par défaut
                // Les seuils minimum et maximum sont laissés vides
                row++;
            }

            // Style pour l'exemple (première ligne)
            var exampleRange = sheet.Range("A5:F5");
            exampleRange.Style.Font.Italic = true;
            exampleRange.Style.Fill.BackgroundColor = XLColor.LightYellow;

            // Validation des données

            // 1. Validation de la quantité (nombre positif)
            var quantityColumn = sheet.Range($"C5:C{row - 1}");
            var quantityValidation = quantityColumn.CreateDataValidation();
            quantityValidation.Decimal.EqualOrGreaterThan(0);
            quantityValidation.ErrorMessage = "La quantité doit être un nombre positif ou zéro";
            quantityValidation.ErrorStyle = XLErrorStyle.Stop;

            // 2. Type d'entrée (liste déroulante)
            var entryTypeColumn = sheet.Range($"D5:D{row - 1}");
            var entryTypeValidation = entryTypeColumn.CreateDataValidation();
            //entryTypeValidation.List(new string[] { "Initial", "Achat", "Don", "Transfert", "Retour", "Autre" });
            entryTypeValidation.ErrorMessage = "Veuillez sélectionner un type d'entrée valide";
            entryTypeValidation.ErrorStyle = XLErrorStyle.Stop;
            entryTypeValidation.IgnoreBlanks = false;

            // 3. Validation du seuil minimum (nombre positif)
            var minThresholdColumn = sheet.Range($"E5:E{row - 1}");
            var minThresholdValidation = minThresholdColumn.CreateDataValidation();
            minThresholdValidation.Decimal.EqualOrGreaterThan(0);
            minThresholdValidation.ErrorMessage = "Le seuil minimum doit être un nombre positif ou zéro";
            minThresholdValidation.ErrorStyle = XLErrorStyle.Information;
            minThresholdValidation.IgnoreBlanks = true;

            // 4. Validation du seuil maximum (nombre positif)
            var maxThresholdColumn = sheet.Range($"F5:F{row - 1}");
            var maxThresholdValidation = maxThresholdColumn.CreateDataValidation();
            maxThresholdValidation.Decimal.EqualOrGreaterThan(0);
            maxThresholdValidation.ErrorMessage = "Le seuil maximum doit être un nombre positif ou zéro";
            maxThresholdValidation.ErrorStyle = XLErrorStyle.Information;
            maxThresholdValidation.IgnoreBlanks = true;

            // Ajouter des commentaires d'aide
            sheet.Cell("C4").CreateComment().AddText("Saisissez la quantité à ajouter en stock (laissez 0 ou vide pour ignorer ce produit)");
            sheet.Cell("D4").CreateComment().AddText("Type d'entrée en stock: Initial, Achat, Don, Transfert, Retour ou Autre");
            sheet.Cell("E4").CreateComment().AddText("Seuil d'alerte minimum (optionnel)");
            sheet.Cell("F4").CreateComment().AddText("Seuil d'alerte maximum (optionnel)");

            // Figer les volets
            sheet.SheetView.FreezeRows(4);
            sheet.SheetView.FreezeColumns(2);

            // Ajuster automatiquement la largeur des colonnes
            sheet.Columns().AdjustToContents();

            return sheet;
        }

        /// <summary>
        /// Traite un fichier Excel importé pour extraire les données des produits et entrées de stock
        /// </summary>
        public async Task<(List<ProductImportDTO> Products, List<StockEntryImportDTO> StockEntries, List<string> Errors)> ProcessImportedExcel(Stream fileStream, int hospitalCenterId)
        {
            var products = new List<ProductImportDTO>();
            var stockEntries = new List<StockEntryImportDTO>();
            var errors = new List<string>();

            try
            {
                // Vérifier que le centre existe
                var center = await _centerService.GetCenterByIdAsync(hospitalCenterId);
                if (center == null)
                {
                    errors.Add($"Centre hospitalier avec ID {hospitalCenterId} introuvable");
                    return (products, stockEntries, errors);
                }

                using (var workbook = new XLWorkbook(fileStream))
                {
                    // Traiter la feuille des nouveaux produits
                    var productSheet = workbook.Worksheet("Nouveaux Produits");
                    if (productSheet != null)
                    {
                        await ProcessProductSheet(productSheet, products, errors);
                    }
                    else
                    {
                        errors.Add("Feuille 'Nouveaux Produits' introuvable dans le fichier Excel");
                    }

                    // Traiter la feuille des entrées en stock
                    var stockSheet = workbook.Worksheet("Entrées Stock");
                    if (stockSheet != null)
                    {
                        await ProcessStockSheet(stockSheet, stockEntries, errors, hospitalCenterId, products);
                    }
                    else
                    {
                        errors.Add("Feuille 'Entrées Stock' introuvable dans le fichier Excel");
                    }

                    // Vérifier la cohérence entre les deux feuilles
                    ValidateConsistency(products, stockEntries, errors);
                }

                // Retourner les résultats
                return (products, stockEntries, errors);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("ProductExcelService", "ProcessImportedExcelFailed",
                    $"Erreur lors du traitement du fichier Excel",
                    details: new { HospitalCenterId = hospitalCenterId, Error = ex.Message });

                errors.Add($"Erreur lors du traitement du fichier Excel: {ex.Message}");
                return (products, stockEntries, errors);
            }
        }

        /// <summary>
        /// Traite la feuille des nouveaux produits
        /// </summary>
        private async Task ProcessProductSheet(IXLWorksheet sheet, List<ProductImportDTO> products, List<string> errors)
        {
            int rowIndex = 5;

            // Ignorer la ligne d'exemple
            rowIndex++;

            while (!sheet.Cell($"A{rowIndex}").IsEmpty()) // Tant qu'il y a un nom de produit
            {
                try
                {
                    var product = new ProductImportDTO();
                    bool rowValid = true;

                    // Nom du produit (obligatoire)
                    if (sheet.Cell($"A{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: Le nom du produit est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        product.Name = sheet.Cell($"A{rowIndex}").GetString().Trim();
                    }

                    // Description (facultative)
                    if (!sheet.Cell($"B{rowIndex}").IsEmpty())
                    {
                        product.Description = sheet.Cell($"B{rowIndex}").GetString().Trim();
                    }

                    // ID de catégorie (obligatoire)
                    if (sheet.Cell($"C{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: L'ID de catégorie est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        try
                        {
                            product.CategoryId = sheet.Cell($"C{rowIndex}").GetValue<int>();

                            // Vérifier que la catégorie existe
                            var category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
                            if (category == null)
                            {
                                errors.Add($"Ligne {rowIndex}: La catégorie avec ID {product.CategoryId} n'existe pas");
                                rowValid = false;
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: L'ID de catégorie doit être un nombre entier");
                            rowValid = false;
                        }
                    }

                    // Unité de mesure (obligatoire)
                    if (sheet.Cell($"E{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: L'unité de mesure est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        product.UnitOfMeasure = sheet.Cell($"E{rowIndex}").GetString().Trim();
                    }

                    // Prix de vente (obligatoire)
                    if (sheet.Cell($"F{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: Le prix de vente est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        try
                        {
                            product.SellingPrice = sheet.Cell($"F{rowIndex}").GetValue<decimal>();

                            // Vérifier que le prix est positif
                            if (product.SellingPrice <= 0)
                            {
                                errors.Add($"Ligne {rowIndex}: Le prix de vente doit être supérieur à zéro");
                                rowValid = false;
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: Le prix de vente doit être un nombre");
                            rowValid = false;
                        }
                    }

                    // Statut actif (facultatif, par défaut true)
                    if (!sheet.Cell($"G{rowIndex}").IsEmpty())
                    {
                        try
                        {
                            product.IsActive = sheet.Cell($"G{rowIndex}").GetValue<bool>();
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: Le statut actif doit être VRAI ou FAUX");
                            // Ne pas invalider la ligne pour ce champ optionnel
                        }
                    }
                    else
                    {
                        product.IsActive = true; // Valeur par défaut
                    }

                    // Notes (facultatives)
                    if (!sheet.Cell($"H{rowIndex}").IsEmpty())
                    {
                        product.Notes = sheet.Cell($"H{rowIndex}").GetString().Trim();
                    }

                    // Ajouter le produit à la liste si la ligne est valide
                    if (rowValid)
                    {
                        // Vérifier l'unicité du nom dans la liste
                        if (products.Any(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"Ligne {rowIndex}: Un produit avec le nom '{product.Name}' existe déjà dans le fichier");
                        }
                        else
                        {
                            products.Add(product);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Ligne {rowIndex}: Erreur lors du traitement: {ex.Message}");
                }

                rowIndex++;
            }
        }

        /// <summary>
        /// Traite la feuille des entrées en stock
        /// </summary>
        private async Task ProcessStockSheet(IXLWorksheet sheet, List<StockEntryImportDTO> stockEntries, List<string> errors, int hospitalCenterId, List<ProductImportDTO> newProducts)
        {
            int rowIndex = 5;

            // Ignorer la ligne d'exemple
            rowIndex++;

            while (!sheet.Cell($"B{rowIndex}").IsEmpty()) // Tant qu'il y a un nom de produit
            {
                try
                {
                    var stockEntry = new StockEntryImportDTO
                    {
                        HospitalCenterId = hospitalCenterId
                    };

                    bool rowValid = true;

                    // ID Produit (facultatif, uniquement pour produits existants)
                    if (!sheet.Cell($"A{rowIndex}").IsEmpty())
                    {
                        try
                        {
                            stockEntry.ProductId = sheet.Cell($"A{rowIndex}").GetValue<int>();

                            // Vérifier que le produit existe
                            var product = await _productService.GetProductByIdAsync(stockEntry.ProductId.Value);
                            if (product == null)
                            {
                                errors.Add($"Ligne {rowIndex}: Le produit avec ID {stockEntry.ProductId} n'existe pas");
                                rowValid = false;
                            }
                            else
                            {
                                stockEntry.ProductName = product.Name;
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: L'ID de produit doit être un nombre entier");
                            rowValid = false;
                        }
                    }

                    // Nom du produit (obligatoire)
                    if (sheet.Cell($"B{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: Le nom du produit est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        stockEntry.ProductName = sheet.Cell($"B{rowIndex}").GetString().Trim();

                        // Si l'ID est vide, vérifier que le produit est dans la liste des nouveaux produits
                        if (!stockEntry.ProductId.HasValue)
                        {
                            if (!newProducts.Any(p => p.Name.Equals(stockEntry.ProductName, StringComparison.OrdinalIgnoreCase)))
                            {
                                errors.Add($"Ligne {rowIndex}: Le produit '{stockEntry.ProductName}' n'est pas défini dans la feuille des nouveaux produits et aucun ID existant n'est fourni");
                                rowValid = false;
                            }
                        }
                    }

                    // Quantité (obligatoire)
                    if (sheet.Cell($"C{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: La quantité est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        try
                        {
                            stockEntry.Quantity = sheet.Cell($"C{rowIndex}").GetValue<decimal>();

                            // Vérifier que la quantité est positive
                            if (stockEntry.Quantity <= 0)
                            {
                                errors.Add($"Ligne {rowIndex}: La quantité doit être supérieure à zéro");
                                rowValid = false;
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: La quantité doit être un nombre");
                            rowValid = false;
                        }
                    }

                    // Date d'entrée (obligatoire)
                    if (sheet.Cell($"D{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: La date d'entrée est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        try
                        {
                            stockEntry.EntryDate = sheet.Cell($"D{rowIndex}").GetDateTime();
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: Format de date d'entrée invalide. Utilisez le format JJ/MM/AAAA");
                            rowValid = false;
                        }
                    }

                    // Numéro de lot (facultatif)
                    if (!sheet.Cell($"E{rowIndex}").IsEmpty())
                    {
                        stockEntry.BatchNumber = sheet.Cell($"E{rowIndex}").GetString().Trim();
                    }

                    // Date d'expiration (facultative)
                    if (!sheet.Cell($"F{rowIndex}").IsEmpty())
                    {
                        try
                        {
                            stockEntry.ExpiryDate = sheet.Cell($"F{rowIndex}").GetDateTime();

                            // Vérifier que la date d'expiration est future
                            if (stockEntry.ExpiryDate <= DateTime.Now)
                            {
                                errors.Add($"Ligne {rowIndex}: La date d'expiration doit être dans le futur");
                                // Ne pas invalider la ligne pour ce champ facultatif
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: Format de date d'expiration invalide. Utilisez le format JJ/MM/AAAA");
                            // Ne pas invalider la ligne pour ce champ facultatif
                        }
                    }

                    // Prix d'achat (facultatif)
                    if (!sheet.Cell($"G{rowIndex}").IsEmpty())
                    {
                        try
                        {
                            stockEntry.PurchasePrice = sheet.Cell($"G{rowIndex}").GetValue<decimal>();

                            // Vérifier que le prix est positif ou zéro
                            if (stockEntry.PurchasePrice < 0)
                            {
                                errors.Add($"Ligne {rowIndex}: Le prix d'achat ne peut pas être négatif");
                                // Ne pas invalider la ligne pour ce champ facultatif
                            }
                        }
                        catch
                        {
                            errors.Add($"Ligne {rowIndex}: Le prix d'achat doit être un nombre");
                            // Ne pas invalider la ligne pour ce champ facultatif
                        }
                    }

                    // Fournisseur (facultatif)
                    if (!sheet.Cell($"H{rowIndex}").IsEmpty())
                    {
                        stockEntry.Supplier = sheet.Cell($"H{rowIndex}").GetString().Trim();
                    }

                    // Notes (facultatives)
                    if (!sheet.Cell($"I{rowIndex}").IsEmpty())
                    {
                        stockEntry.Notes = sheet.Cell($"I{rowIndex}").GetString().Trim();
                    }

                    // Type d'entrée (obligatoire)
                    if (sheet.Cell($"J{rowIndex}").IsEmpty())
                    {
                        errors.Add($"Ligne {rowIndex}: Le type d'entrée est obligatoire");
                        rowValid = false;
                    }
                    else
                    {
                        stockEntry.EntryType = sheet.Cell($"J{rowIndex}").GetString().Trim();

                        // Vérifier que le type d'entrée est valide
                        string[] validTypes = { "Initial", "Achat", "Don", "Transfert", "Retour", "Autre" };
                        if (!validTypes.Contains(stockEntry.EntryType))
                        {
                            errors.Add($"Ligne {rowIndex}: Type d'entrée invalide. Valeurs autorisées: Initial, Achat, Don, Transfert, Retour, Autre");
                            rowValid = false;
                        }
                    }

                    // Ajouter l'entrée à la liste si la ligne est valide
                    if (rowValid)
                    {
                        stockEntries.Add(stockEntry);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Ligne {rowIndex}: Erreur lors du traitement: {ex.Message}");
                }

                rowIndex++;
            }
        }

        /// <summary>
        /// Valide la cohérence entre les feuilles de nouveaux produits et d'entrées en stock
        /// </summary>
        private void ValidateConsistency(List<ProductImportDTO> products, List<StockEntryImportDTO> stockEntries, List<string> errors)
        {
            // Vérifier que tous les nouveaux produits référencés dans les entrées ont une définition
            var newProductNames = products.Select(p => p.Name.ToLower()).ToList();

            foreach (var entry in stockEntries.Where(e => !e.ProductId.HasValue))
            {
                if (!newProductNames.Contains(entry.ProductName.ToLower()))
                {
                    errors.Add($"L'entrée en stock pour '{entry.ProductName}' n'a pas d'ID produit existant et n'est pas définie dans la feuille des nouveaux produits");
                }
            }
        }
    }

    /// <summary>
    /// DTO pour l'importation d'un produit
    /// </summary>
    public class ProductImportDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }
    }

    /// <summary>
    /// DTO pour l'importation d'une entrée en stock
    /// </summary>
    public class StockEntryImportDTO
    {
        public int? ProductId { get; set; }
        public string ProductName { get; set; }
        public int HospitalCenterId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime EntryDate { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        public string Supplier { get; set; }
        public string Notes { get; set; }
        public string EntryType { get; set; }
    }
}