using AutoMapper;
using FITSync.Contracts.Faqs;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class FaqService : BaseCRUDService<Faq, FaqResponse, FaqInsertRequest, FaqUpdateRequest>, IFaqService
    {
        private readonly IFaqRepository _faqRepository;
        private readonly IMapper _mapper;

        public FaqService(IFaqRepository repository, IMapper mapper) : base(repository, mapper)
        {
            _faqRepository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// What the mobile help screen asks for: active entries only, in display order.
        /// Administrators use the inherited GetAsync, which also returns retired ones.
        /// </summary>
        public async Task<List<FaqResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            var entries = await _faqRepository.GetActiveAsync(cancellationToken);
            return _mapper.Map<List<FaqResponse>>(entries);
        }
    }
}
