using AutoMapper;
using AutoMapper.QueryableExtensions;
using HManagSys.Data.DBContext;
using HManagSys.Data.Repositories.Interfaces;
using HManagSys.Helpers;
using HManagSys.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;

namespace HManagSys.Data.Repositories
{
    /// <summary>
    /// Implémentation du repository générique
    /// Le moteur universel de notre système de gestion de données
    /// </summary>
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : class, IEntity
    {
        protected readonly HospitalManagementContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger<GenericRepository<TEntity>> _logger;

        public GenericRepository(
            HospitalManagementContext context,
            ILogger<GenericRepository<TEntity>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ===== OPÉRATIONS CRUD DE BASE =====

        public virtual IQueryable<TEntity> AsQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public virtual async Task<TEntity?> GetByIdAsync(int id)
        {
            try
            {
                return await _dbSet.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de {EntityName} avec ID {Id}",
                    typeof(TEntity).Name, id);
                throw;
            }
        }

        public virtual async Task<IList<TEntity>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                    query = filter(query);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les entités {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                // Préparer l'entité pour l'insertion
                PrepareEntityForInsert(entity);

                await _dbSet.AddAsync(entity);
                await SaveChangesAsync();

                _logger.LogInformation("Entité {EntityName} créée avec ID {Id}",
                    typeof(TEntity).Name, entity.Id);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<bool> UpdateAsync(TEntity entity, string[]? additionalExcludedProperties = null)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                // Préparer l'entité pour la mise à jour
                PrepareEntityForUpdate(entity);

                // Obtenir l'entité originale du contexte
                var existingEntity = await _dbSet.FindAsync(entity.Id);
                if (existingEntity == null)
                {
                    _logger.LogWarning("Tentative de mise à jour d'une entité {EntityName} inexistante avec ID {Id}",
                        typeof(TEntity).Name, entity.Id);
                    return false;
                }

                // Obtenir les propriétés à exclure
                var defaultExcludedProperties = GetDefaultExcludedProperties();
                if (additionalExcludedProperties != null)
                    defaultExcludedProperties.AddRange(additionalExcludedProperties);

                // Mettre à jour les propriétés de l'entité
                _context.Entry(existingEntity).CurrentValues.SetValues(entity);

                // Exclure les propriétés spécifiées
                ExcludePropertiesFromModification(existingEntity, defaultExcludedProperties);

                var result = await SaveChangesAsync();

                if (result)
                {
                    _logger.LogInformation("Entité {EntityName} mise à jour avec ID {Id}",
                        typeof(TEntity).Name, entity.Id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de {EntityName} avec ID {Id}",
                    typeof(TEntity).Name, entity.Id);
                throw;
            }
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await _dbSet.FindAsync(id);
                if (entity == null)
                {
                    _logger.LogWarning("Tentative de suppression d'une entité {EntityName} inexistante avec ID {Id}",
                        typeof(TEntity).Name, id);
                    return false;
                }

                // Effectuer un soft delete si l'entité a une propriété IsActive
                if (HasProperty(entity, "IsActive"))
                {
                    var property = entity.GetType().GetProperty("IsActive");
                    property?.SetValue(entity, false);
                    PrepareEntityForUpdate(entity);
                    await SaveChangesAsync();
                }
                else
                {
                    // Hard delete si pas de soft delete possible
                    _dbSet.Remove(entity);
                    await SaveChangesAsync();
                }

                _logger.LogInformation("Entité {EntityName} supprimée avec ID {Id}",
                    typeof(TEntity).Name, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de {EntityName} avec ID {Id}",
                    typeof(TEntity).Name, id);
                throw;
            }
        }

        // ===== OPÉRATIONS BATCH =====

        public virtual async Task<bool> AddRangeAsync(IList<TEntity> entities)
        {
            try
            {
                if (entities == null || !entities.Any())
                    return true;

                foreach (var entity in entities)
                {
                    PrepareEntityForInsert(entity);
                }

                await _dbSet.AddRangeAsync(entities);
                var result = await SaveChangesAsync();

                _logger.LogInformation("Ajout de {Count} entités {EntityName}",
                    entities.Count, typeof(TEntity).Name);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout en lot de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<bool> UpdateRangeAsync(IList<TEntity> entities)
        {
            try
            {
                if (entities == null || !entities.Any())
                    return true;

                foreach (var entity in entities)
                {
                    await UpdateAsync(entity);
                }

                _logger.LogInformation("Mise à jour de {Count} entités {EntityName}",
                    entities.Count, typeof(TEntity).Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour en lot de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<bool> DeleteRangeAsync(IEnumerable<int> ids)
        {
            try
            {
                var entities = await _dbSet.Where(e => ids.Contains(e.Id)).ToListAsync();

                foreach (var entity in entities)
                {
                    await DeleteAsync(entity.Id);
                }

                _logger.LogInformation("Suppression de {Count} entités {EntityName}",
                    entities.Count, typeof(TEntity).Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression en lot de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<IList<TEntity>> GetByIdsAsync(IList<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    return new List<TEntity>();

                return await _dbSet.Where(e => ids.Contains(e.Id)).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération par IDs de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        // ===== OPÉRATIONS DE REQUÊTE AVANCÉES =====

        public virtual async Task<bool> AnyAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                    query = filter(query);

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification d'existence de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<TEntity?> GetSingleAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> filter)
        {
            try
            {
                var query = filter(_dbSet);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération unique de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                    query = filter(query);

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<TResult> SumAsync<TResult>(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> filter,
            Func<TEntity, TResult> selector) where TResult : struct
        {
            try
            {
                if (filter == null)
                    throw new ArgumentNullException(nameof(filter));
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));

                //IQueryable<TEntity> query = _dbSet;

                //if (filter != null)
                //    query = filter(query);
                var sum = filter(_dbSet)
                                .AsEnumerable()
                                .Sum(entity => Convert.ToDecimal(selector(entity)));
                //var sum = await filter(_dbSet).SumAsync(entity => Convert.ToDecimal(selector(entity)));
                return (TResult)Convert.ChangeType(sum, typeof(TResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de somme de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        // ===== OPÉRATIONS AVEC PROJECTION =====

        //public virtual async Task<TResult?> GetByIdAsAsync<TResult>(int id) where TResult : class
        //{
        //    try
        //    {
        //        var entity = await GetByIdAsync(id);
        //        if (entity == null) return null;

        //        return _mapper.Map<TResult>(entity);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erreur lors de la projection de {EntityName} vers {ResultType}",
        //            typeof(TEntity).Name, typeof(TResult).Name);
        //        throw;
        //    }
        //}

        //public virtual async Task<IList<TResult>> GetAllAsAsync<TResult>(
        //    Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null) where TResult : class
        //{
        //    try
        //    {
        //        IQueryable<TEntity> query = _dbSet;

        //        if (filter != null)
        //            query = filter(query);

        //        return await query.ProjectTo<TResult>(_mapper.ConfigurationProvider).ToListAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erreur lors de la projection de {EntityName} vers {ResultType}",
        //            typeof(TEntity).Name, typeof(TResult).Name);
        //        throw;
        //    }
        //}

        // ===== PAGINATION =====

        public virtual async Task<(IList<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageIndex = 1,
            int pageSize = 20,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
        {
            try
            {
                IQueryable<TEntity> query = _dbSet;

                if (filter != null)
                    query = filter(query);

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la pagination de {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        //public virtual async Task<(IList<TResult> Items, int TotalCount)> GetPagedAsAsync<TResult>(
        //    int pageIndex = 1,
        //    int pageSize = 20,
        //    Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null) where TResult : class
        //{
        //    try
        //    {
        //        IQueryable<TEntity> query = _dbSet;

        //        if (filter != null)
        //            query = filter(query);

        //        var totalCount = await query.CountAsync();
        //        var items = await query
        //            .Skip((pageIndex - 1) * pageSize)
        //            .Take(pageSize)
        //            .ProjectTo<TResult>(_mapper.ConfigurationProvider)
        //            .ToListAsync();

        //        return (items, totalCount);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Erreur lors de la pagination avec projection de {EntityName}",
        //            typeof(TEntity).Name);
        //        throw;
        //    }
        //}

        // ===== REQUÊTES PERSONNALISÉES =====

        public virtual async Task<TResult?> QuerySingleAsync<TResult>(
            Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder)
            where TResult : class
        {
            try
            {
                var query = queryBuilder(_dbSet);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution d'une requête personnalisée sur {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<List<TResult>> QueryListAsync<TResult>(
            Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder)
            where TResult : class
        {
            try
            {
                var query = queryBuilder(_dbSet);

                var tt = query.ToQueryString();
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution d'une requête personnalisée sur {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual async Task<IList<TEntity>> FromSqlRawAsync(string sql, params object[] parameters)
        {
            try
            {
                return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution de SQL brut sur {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        // ===== TRANSACTIONS =====

        public virtual async Task<TResult> TransactionAsync<TResult>(Func<Task<TResult>> operation)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ===== UTILITAIRES =====

        public virtual async Task<bool> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde des changements pour {EntityName}",
                    typeof(TEntity).Name);
                throw;
            }
        }

        public virtual void DetachEntity(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }

        // ===== MÉTHODES PROTÉGÉES POUR PERSONNALISATION =====

        /// <summary>
        /// Prépare une entité avant insertion
        /// Méthode virtuelle permettant la personnalisation dans les repositories hérités
        /// </summary>
        protected virtual void PrepareEntityForInsert(TEntity entity)
        {
            var cameroonTime = TimeZoneHelper.GetCameroonTime();
            entity.CreatedAt = cameroonTime;
            entity.ModifiedAt = null;
            entity.ModifiedBy = null;
        }

        /// <summary>
        /// Prépare une entité avant mise à jour
        /// Méthode virtuelle permettant la personnalisation dans les repositories hérités
        /// </summary>
        protected virtual void PrepareEntityForUpdate(TEntity entity)
        {
            entity.ModifiedAt = TimeZoneHelper.GetCameroonTime();
        }

        /// <summary>
        /// Obtient la liste des propriétés à exclure par défaut lors des mises à jour
        /// Méthode virtuelle permettant la personnalisation dans les repositories hérités
        /// </summary>
        protected virtual List<string> GetDefaultExcludedProperties()
        {
            return new List<string>
            {
                nameof(IEntity.CreatedBy),
                nameof(IEntity.CreatedAt),
                nameof(IEntity.Id) // On ne doit jamais modifier l'ID
            };
        }

        /// <summary>
        /// Exclut certaines propriétés de la modification
        /// Méthode virtuelle permettant la personnalisation dans les repositories hérités
        /// </summary>
        protected virtual void ExcludePropertiesFromModification(TEntity entity, List<string> propertiesToExclude)
        {
            foreach (var property in propertiesToExclude)
            {
                if (_context.Entry(entity).Property(property).Metadata != null)
                {
                    _context.Entry(entity).Property(property).IsModified = false;
                }
            }
        }

        /// <summary>
        /// Vérifie si une entité a une propriété spécifique
        /// </summary>
        private static bool HasProperty(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }
    }
}