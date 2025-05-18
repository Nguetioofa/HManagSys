using System;
using System.Collections.Generic;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour le suivi des mouvements de stock
    /// </summary>
    public class StockMovementTrackingViewModel
    {
        /// <summary>
        /// Liste des mouvements de stock générés
        /// </summary>
        public List<StockMovementResultItem> Movements { get; set; } = new List<StockMovementResultItem>();

        /// <summary>
        /// Informations sur l'opération source
        /// </summary>
        public SourceOperationInfo SourceOperation { get; set; } = new SourceOperationInfo();

        /// <summary>
        /// Nombre total de produits impactés
        /// </summary>
        public int TotalProductsAffected => Movements.Count;

        /// <summary>
        /// Quantité totale de produits décrémentés
        /// </summary>
        public decimal TotalQuantityAffected => Movements.Sum(m => Math.Abs(m.Quantity));

        /// <summary>
        /// Liste des produits en quantité insuffisante
        /// </summary>
        public List<StockShortageItem> ShortageItems { get; set; } = new List<StockShortageItem>();

        /// <summary>
        /// Indique si des produits étaient en quantité insuffisante
        /// </summary>
        public bool HasShortages => ShortageItems.Any();
    }

    /// <summary>
    /// Information sur un mouvement de stock individuel
    /// </summary>
    public class StockMovementResultItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal NewStockLevel { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public DateTime MovementDate { get; set; }

        // Propriétés calculées pour l'affichage
        public string QuantityText => $"{Quantity:N2} {UnitOfMeasure}";
        public string NewStockText => $"{NewStockLevel:N2} {UnitOfMeasure}";
        public string MovementTypeText => MovementType switch
        {
            "Sale" => "Vente",
            "Care" => "Soin",
            "Prescription" => "Prescription",
            _ => MovementType
        };
    }

    /// <summary>
    /// Informations sur l'opération source du mouvement
    /// </summary>
    public class SourceOperationInfo
    {
        public string OperationType { get; set; } = string.Empty;
        public int OperationId { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime OperationDate { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;

        // Propriétés calculées pour l'affichage
        public string OperationTypeText => OperationType switch
        {
            "Prescription" => "Dispensation de prescription",
            "CareService" => "Service de soins",
            "Sale" => "Vente",
            _ => OperationType
        };

        public string OperationDescription => $"{OperationTypeText} #{OperationId} - {ReferenceNumber}";
    }

    /// <summary>
    /// Information sur un produit en rupture de stock
    /// </summary>
    public class StockShortageItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageAmount => RequestedQuantity - AvailableQuantity;

        public string ShortageText => $"Demandé: {RequestedQuantity:N2}, " +
                                     $"Disponible: {AvailableQuantity:N2}, " +
                                     $"Manquant: {ShortageAmount:N2} {UnitOfMeasure}";
    }
}