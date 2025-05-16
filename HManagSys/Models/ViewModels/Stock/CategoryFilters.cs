using System.ComponentModel.DataAnnotations;

namespace HManagSys.Models.ViewModels.Stock
{
    /// <summary>
    /// Filtres pour la liste des catégories
    /// </summary>
    //public class ProductCategoryFilters
    //{
    //    [Display(Name = "Recherche")]
    //    public string? SearchTerm { get; set; }

    //    [Display(Name = "Statut")]
    //    public bool? IsActive { get; set; }

    //    public string GetQueryString()
    //    {
    //        var queryParams = new List<string>();

    //        if (!string.IsNullOrEmpty(SearchTerm))
    //            queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm)}");

    //        if (IsActive.HasValue)
    //            queryParams.Add($"isActive={IsActive.Value.ToString().ToLower()}");

    //        return queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
    //    }
    //}
}
