namespace HManagSys.Models.Interfaces;

/// <summary>
/// Interface de base pour toutes les entités avec audit automatique
/// Chaque entité hérite de cet "ADN" commun qui garantit la traçabilité
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Identifiant unique de l'entité - la clé d'identité universelle
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// ID de l'utilisateur qui a créé cette entité - "qui l'a fait naître ?"
    /// </summary>
    int CreatedBy { get; set; }

    /// <summary>
    /// Date de création en fuseau camerounais (UTC+1)
    /// Comme un acte de naissance numérique
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// ID de l'utilisateur qui a modifié l'entité en dernier
    /// "Qui a touché à cela en dernier ?"
    /// </summary>
    int? ModifiedBy { get; set; }

    /// <summary>
    /// Date de dernière modification en fuseau camerounais (UTC+1)
    /// Comme un journal de modifications
    /// </summary>
    DateTime? ModifiedAt { get; set; }
}
