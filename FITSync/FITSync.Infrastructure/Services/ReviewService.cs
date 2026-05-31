using AutoMapper;
using FITSync.Contracts.Reviews;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class ReviewService : BaseCRUDService<Review, ReviewResponse, ReviewInsertRequest, ReviewUpdateRequest>, IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IReviewRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
            _reviewRepository = repository;
        }

        public async Task<List<ReviewResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            var entities = await _reviewRepository.GetByTrainingIdAsync(trainingId, cancellationToken);
            return _mapper.Map<List<ReviewResponse>>(entities);
        }

        public async Task<List<ReviewResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _reviewRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<ReviewResponse>>(entities);
        }
    }
}
