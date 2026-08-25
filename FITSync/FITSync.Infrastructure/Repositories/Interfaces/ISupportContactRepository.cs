using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface ISupportContactRepository : IBaseRepository<SupportContact>
    {
        /// <summary>The single contact row, creating it on first use.</summary>
        Task<SupportContact> GetSingletonAsync(CancellationToken cancellationToken = default);
    }
}
