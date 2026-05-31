using FITSync.Domain.Models;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(FitSyncDbContext context) : base(context)
        {
        }

        public override async Task<List<User>> GetAsync()
        {
            return await _dbSet
                .Include(u => u.Roles!)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public override async Task<User?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Roles!)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Roles!)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.Roles!)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }
}
