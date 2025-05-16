namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Résumé d'une catégorie pour l'affichage en liste
    /// </summary>
    public class CategorySummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int ActiveProductCount { get; set; }

        // Propriétés calculées pour l'affichage
        public string StatusBadge => IsActive ? "bg-success" : "bg-secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");
        public string DescriptionPreview => Description?.Length > 100
            ? Description.Substring(0, 100) + "..."
            : Description ?? "";
    }
}
