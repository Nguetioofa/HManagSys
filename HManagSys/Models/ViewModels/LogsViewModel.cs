using HManagSys.Models.EfModels;

namespace HManagSys.Models.ViewModels
{
    public class LogsViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Category { get; set; }
        public string? Action { get; set; }
        public HManagSys.Models.Enums.LogLevel? LogLevel { get; set; }
        public int? UserId { get; set; }
        public int? HospitalCenterId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public List<ApplicationLog> Logs { get; set; } = new();

        // Propriétés pour l'interface utilisateur
        public List<string> AvailableCategories { get; set; } = new();
        public List<string> AvailableActions { get; set; } = new();
    }
}
