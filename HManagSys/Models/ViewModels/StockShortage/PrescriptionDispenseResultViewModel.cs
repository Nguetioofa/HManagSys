using HManagSys.Models.ViewModels.Patients;
using HManagSys.Models.ViewModels.Stock;

namespace HManagSys.Models.ViewModels
{
    /// <summary>
    /// ViewModel pour afficher le résultat d'une dispensation avec mouvements de stock
    /// </summary>
    public class PrescriptionDispenseResultViewModel
    {
        public PrescriptionViewModel Prescription { get; set; } = null!;
        public StockMovementTrackingViewModel? StockMovements { get; set; }
    }

    /// <summary>
    /// ViewModel pour afficher les ruptures de stock d'une prescription
    /// </summary>
    public class StockShortageViewModel
    {
        public int PrescriptionId { get; set; }
        public List<StockShortageItem> ShortageItems { get; set; } = new List<StockShortageItem>();
    }

    /// <summary>
    /// ViewModel pour afficher les ruptures de stock d'un service de soin
    /// </summary>
    public class CareServiceStockShortageViewModel
    {
        public CreateCareServiceViewModel CareServiceModel { get; set; } = null!;
        public List<StockShortageItem> ShortageItems { get; set; } = new List<StockShortageItem>();
    }

    /// <summary>
    /// ViewModel pour afficher le résultat d'un service de soin avec mouvements de stock
    /// </summary>
    public class CareServiceResultViewModel
    {
        public CareServiceViewModel CareService { get; set; } = null!;
        public StockMovementTrackingViewModel? StockMovements { get; set; }
    }
}