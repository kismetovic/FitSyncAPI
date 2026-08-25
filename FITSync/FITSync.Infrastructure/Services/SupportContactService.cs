using AutoMapper;
using FITSync.Contracts.Support;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class SupportContactService : ISupportContactService
    {
        private readonly ISupportContactRepository _repository;
        private readonly IMapper _mapper;

        public SupportContactService(ISupportContactRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<SupportContactResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            var contact = await _repository.GetSingletonAsync(cancellationToken);
            return _mapper.Map<SupportContactResponse>(contact);
        }

        public async Task<SupportContactResponse> UpdateAsync(
            SupportContactUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var contact = await _repository.GetSingletonAsync(cancellationToken);

            contact.Email = request.Email.Trim();
            contact.PhoneNumber = request.PhoneNumber.Trim();
            contact.WorkingHours = request.WorkingHours.Trim();
            contact.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

            await _repository.UpdateAsync(contact);
            return _mapper.Map<SupportContactResponse>(contact);
        }
    }
}
