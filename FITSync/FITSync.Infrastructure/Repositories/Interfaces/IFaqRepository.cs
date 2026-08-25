using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IFaqRepository : IBaseRepository<Faq>
    {
        /// <summary>Only the entries a client should see, already ordered.</summary>
        Task<List<Faq>> GetActiveAsync(CancellationToken cancellationToken = default);
    }
}
