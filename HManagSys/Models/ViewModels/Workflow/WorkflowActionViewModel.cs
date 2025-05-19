namespace HManagSys.Models.ViewModels.Workflow;

/// <summary>
/// ViewModel pour une action de workflow
/// </summary>
public class WorkflowActionViewModel
{
    public string ActionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public Dictionary<string, string> RouteValues { get; set; } = new Dictionary<string, string>();
    public string IconClass { get; set; } = string.Empty;
    public string ButtonClass { get; set; } = "btn-outline-primary";
    public int Priority { get; set; } = 0;
    public bool IsRecommended { get; set; } = false;

    // Propriétés calculées pour faciliter l'affichage
    public string RouteUrl => $"/{ControllerName}/{ActionName}";
    public string RecommendedClass => IsRecommended ? "recommended-action" : "";
}

/// <summary>
/// ViewModel pour une entité liée dans un workflow
/// </summary>
public class RelatedEntityViewModel
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public DateTime RelationshipDate { get; set; }
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = "badge-primary";

    // Propriétés calculées pour faciliter l'affichage
    public string DetailUrl => $"/{ControllerName}/{ActionName}/{EntityId}";
    public string FormattedDate => RelationshipDate.ToString("dd/MM/yyyy HH:mm");
}