using HManagSys.Models.Interfaces;
using System.Linq.Expressions;

namespace HManagSys.Data.Repositories.Interfaces;

/// <summary>
/// Repository générique - Le modèle universel pour toutes nos opérations de données
/// Comme un couteau suisse numérique avec tous les outils de base
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : class, IEntity
{
    // ===== OPÉRATIONS CRUD DE BASE =====

    /// <summary>
    /// Récupère une entité par son ID
    /// Comme chercher un dossier spécifique dans les archives
    /// </summary>
    Task<TEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Récupère toutes les entités avec filtre optionnel
    /// Comme parcourir tous les dossiers d'une catégorie
    /// </summary>
    Task<IList<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null);

    /// <summary>
    /// Ajoute une nouvelle entité
    /// Comme créer un nouveau dossier dans les archives
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Met à jour une entité existante avec contrôle des propriétés
    /// Comme modifier un dossier en préservant certaines informations originales
    /// </summary>
    Task<bool> UpdateAsync(TEntity entity, string[]? additionalExcludedProperties = null);

    /// <summary>
    /// Supprime une entité (soft delete recommandé)
    /// Comme archiver un dossier plutôt que le détruire
    /// </summary>
    Task<bool> DeleteAsync(int id);

    // ===== OPÉRATIONS BATCH =====

    /// <summary>
    /// Ajoute plusieurs entités en une fois
    /// Comme traiter un lot de nouveaux dossiers ensemble
    /// </summary>
    Task<bool> AddRangeAsync(IList<TEntity> entities);

    /// <summary>
    /// Met à jour plusieurs entités
    /// Comme modifier plusieurs dossiers avec les mêmes critères
    /// </summary>
    Task<bool> UpdateRangeAsync(IList<TEntity> entities);

    /// <summary>
    /// Supprime plusieurs entités par leurs IDs
    /// </summary>
    Task<bool> DeleteRangeAsync(IEnumerable<int> ids);

    /// <summary>
    /// Récupère plusieurs entités par leurs IDs
    /// </summary>
    Task<IList<TEntity>> GetByIdsAsync(IList<int> ids);

    // ===== OPÉRATIONS DE REQUÊTE AVANCÉES =====

    /// <summary>
    /// Vérifie l'existence d'au moins une entité selon un critère
    /// Comme vérifier s'il existe au moins un dossier répondant aux critères
    /// </summary>
    Task<bool> AnyAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null);

    /// <summary>
    /// Récupère une seule entité selon un critère
    /// </summary>
    Task<TEntity?> GetSingleAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> filter);

    /// <summary>
    /// Compte le nombre d'entités selon un critère
    /// </summary>
    Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null);

    /// <summary>
    /// Calcule une somme sur les entités
    /// </summary>
    Task<TResult> SumAsync<TResult>(
                Func<IQueryable<TEntity>, IQueryable<TEntity>> filter,
                Func<TEntity, TResult> selector) where TResult : struct;
    
    // ===== OPÉRATIONS AVEC PROJECTION =====

        /// <summary>
        /// Récupère et projette une entité vers un autre type
        /// Comme extraire seulement certaines informations d'un dossier
        /// </summary>
      //  Task<TResult?> GetByIdAsAsync<TResult>(int id) where TResult : class;

    /// <summary>
    /// Récupère et projette toutes les entités
    /// </summary>
    //Task<IList<TResult>> GetAllAsAsync<TResult>(
    //    Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null) where TResult : class;

    // ===== PAGINATION =====

    /// <summary>
    /// Récupère les entités de manière paginée
    /// Comme feuilleter un gros registre page par page
    /// </summary>
    Task<(IList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageIndex = 1,
        int pageSize = 20,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null);

    /// <summary>
    /// Récupère et projette les entités de manière paginée
    /// </summary>
    //Task<(IList<TResult> Items, int TotalCount)> GetPagedAsAsync<TResult>(
    //    int pageIndex = 1,
    //    int pageSize = 20,
    //    Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null) where TResult : class;

    // ===== REQUÊTES PERSONNALISÉES =====

    /// <summary>
    /// Exécute une requête personnalisée retournant un seul résultat
    /// </summary>
    Task<TResult?> QuerySingleAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder)
        where TResult : class;

    /// <summary>
    /// Exécute une requête personnalisée retournant une liste
    /// </summary>
    Task<IList<TResult>> QueryListAsync<TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder)
        where TResult : class;

    /// <summary>
    /// Exécute du SQL brut
    /// Comme accéder directement aux archives sans passer par le système
    /// </summary>
    Task<IList<TEntity>> FromSqlRawAsync(string sql, params object[] parameters);

    // ===== TRANSACTIONS =====

    /// <summary>
    /// Exécute une opération dans une transaction
    /// Comme s'assurer que plusieurs modifications se font ensemble ou pas du tout
    /// </summary>
    Task<TResult> TransactionAsync<TResult>(Func<Task<TResult>> operation);

    // ===== UTILITAIRES =====

    /// <summary>
    /// Sauvegarde les changements en cours
    /// </summary>
    Task<bool> SaveChangesAsync();

    /// <summary>
    /// Détache une entité du contexte pour éviter les conflits
    /// </summary>
    void DetachEntity(TEntity entity);
}
