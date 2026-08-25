using FITSync.Contracts.Faqs;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IFaqService : IBaseCRUDService<FaqResponse, FaqInsertRequest, FaqUpdateRequest>
    {
        Task<List<FaqResponse>> GetActiveAsync(CancellationToken cancellationToken = default);
    }
}
