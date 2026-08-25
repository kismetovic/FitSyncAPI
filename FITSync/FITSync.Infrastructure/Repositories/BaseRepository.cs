using FITSync.Domain.Models;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class BaseRepository<TModel> : IBaseRepository<TModel> where TModel : class
    {
        protected readonly FitSyncDbContext _context;
        protected readonly DbSet<TModel> _dbSet;

        public BaseRepository(FitSyncDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TModel>();
        }

        /// <summary>
        /// Base query used by both the list and the paged reads, so a derived repository
        /// only has to declare its includes and soft-delete filter once.
        /// </summary>
        protected virtual IQueryable<TModel> BaseQuery()
        {
            var query = _dbSet.AsQueryable();
            if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TModel)))
                query = query.Where(e => !((ISoftDeletable)e).IsDeleted);
            return query;
        }

        public virtual async Task<List<TModel>> GetAsync()
        {
            return await BaseQuery().ToListAsync();
        }

        public virtual async Task<(List<TModel> Items, int TotalCount)> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();
            var total = await query.CountAsync(cancellationToken);
            var items = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
            return (items, total);
        }

        public virtual async Task<TModel?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TModel> InsertAsync(TModel entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TModel> UpdateAsync(TModel entity)
        {
            // Only force the Modified state when the entity is not already tracked;
            // re-stamping a tracked graph would also try to re-insert its navigations.
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(TModel entity)
        {
            if (entity is ISoftDeletable soft)
            {
                soft.IsDeleted = true;
                await UpdateAsync(entity);
                return;
            }
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
