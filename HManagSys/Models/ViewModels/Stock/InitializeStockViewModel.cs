namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// ViewModel pour l'initialisation du stock (SuperAdmin)
    /// </summary>
    public class InitializeStockViewModel
    {
        public int HospitalCenterId { get; set; }
        public string HospitalCenterName { get; set; } = string.Empty;
        public List<ProductStockInitViewModel> Products { get; set; } = new();
        public bool AllowBulkUpdate { get; set; } = true;
    }
}
