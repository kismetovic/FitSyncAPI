namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IBaseRepository<TModel> where TModel : class
    {
        Task<List<TModel>> GetAsync();

        /// <summary>
        /// Server-side paged read. Every list endpoint goes through a paged path so a
        /// client can never make the API materialise a whole table.
        /// </summary>
        Task<(List<TModel> Items, int TotalCount)> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);

        Task<TModel?> GetByIdAsync(int id);
        Task<TModel> InsertAsync(TModel entity);
        Task<TModel> UpdateAsync(TModel entity);
        Task DeleteAsync(TModel entity);
    }
}
