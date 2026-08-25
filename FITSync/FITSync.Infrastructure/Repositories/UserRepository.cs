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

        protected override IQueryable<User> BaseQuery()
        {
            return _dbSet
                .Include(u => u.Roles!)
                .ThenInclude(ur => ur.Role);
        }

        public override async Task<List<User>> GetAsync()
        {
            return await BaseQuery().ToListAsync();
        }

        public override async Task<User?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return await BaseQuery().FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await BaseQuery().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        /// <summary>
        /// Filtering happens in SQL. The previous admin search pulled every user into memory
        /// and filtered with LINQ-to-objects.
        /// </summary>
        public async Task<(List<User> Items, int TotalCount)> SearchAsync(
            string? name,
            string? role,
            bool? enabled,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(u =>
                    (u.Name != null && u.Name.Contains(name)) ||
                    (u.Surname != null && u.Surname.Contains(name)) ||
                    (u.UserName != null && u.UserName.Contains(name)) ||
                    (u.Email != null && u.Email.Contains(name)));
            }

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Roles!.Any(ur => ur.Role != null && ur.Role.Name == role));

            if (enabled.HasValue)
                query = query.Where(u => u.Enabled == enabled.Value);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(u => u.UserName)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
            => await _dbSet.CountAsync(cancellationToken);
    }
}
