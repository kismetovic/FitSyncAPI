using FITSync.Contracts.Support;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface ISupportContactService
    {
        Task<SupportContactResponse> GetAsync(CancellationToken cancellationToken = default);

        Task<SupportContactResponse> UpdateAsync(
            SupportContactUpdateRequest request, CancellationToken cancellationToken = default);
    }
}
