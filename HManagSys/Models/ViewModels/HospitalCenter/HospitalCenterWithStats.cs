namespace HManagSys.Models.ViewModels.HospitalCenter
{
    /// <summary>
    /// Centre hospitalier avec statistiques de base
    /// </summary>
    public class HospitalCenterWithStats
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Statistiques
        public int ActiveUsers { get; set; }
        public int ProductsInStock { get; set; }
        public int TotalSales { get; set; }
        public int ActiveCareEpisodes { get; set; }
    }
}
