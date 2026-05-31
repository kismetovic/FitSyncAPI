using AutoMapper;
using FITSync.Contracts.Payments;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class PaymentService : BaseCRUDService<Payment, PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>, IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
            _paymentRepository = repository;
        }

        public async Task<PaymentResponse?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            var entity = await _paymentRepository.GetByReservationIdAsync(reservationId, cancellationToken);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<PaymentResponse?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            var entity = await _paymentRepository.GetByTransactionIdAsync(transactionId, cancellationToken);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<List<PaymentResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _paymentRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<PaymentResponse>>(entities);
        }
    }
}
