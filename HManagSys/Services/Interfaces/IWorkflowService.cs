using HManagSys.Models.ViewModels.Workflow;

namespace HManagSys.Services.Interfaces;

/// <summary>
/// Service pour gérer les workflows et relations entre modules
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Obtient les actions suivantes possibles pour une entité
    /// </summary>
    /// <param name="entityType">Type d'entité (Patient, CareEpisode, Examination, etc.)</param>
    /// <param name="entityId">ID de l'entité</param>
    /// <returns>Liste des actions possibles</returns>
    Task<List<WorkflowActionViewModel>> GetNextActionsAsync(string entityType, int entityId);

    /// <summary>
    /// Obtient les entités liées à une entité spécifique
    /// </summary>
    /// <param name="entityType">Type d'entité (Patient, CareEpisode, Examination, etc.)</param>
    /// <param name="entityId">ID de l'entité</param>
    /// <returns>Liste des entités liées</returns>
    Task<List<RelatedEntityViewModel>> GetRelatedEntitiesAsync(string entityType, int entityId);
}