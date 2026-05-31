using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public virtual async Task<List<TModel>> GetAsync()
        {
            return await _dbSet.ToListAsync();
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
            _context.Entry(entity).State = EntityState.Modified;
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
