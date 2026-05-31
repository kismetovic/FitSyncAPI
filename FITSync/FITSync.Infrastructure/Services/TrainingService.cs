using AutoMapper;
using FITSync.Contracts.Trainings;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class TrainingService : BaseCRUDService<Training, TrainingResponse, TrainingInsertRequest, TrainingUpdateRequest>, ITrainingService
    {
        private readonly ITrainingRepository _trainingRepository;
        private readonly IReviewRepository _reviewRepository;

        public TrainingService(ITrainingRepository repository, IMapper mapper, IReviewRepository reviewRepository)
            : base(repository, mapper)
        {
            _trainingRepository = repository;
            _reviewRepository = reviewRepository;
        }

        public override async Task<List<TrainingResponse>> GetAsync()
        {
            var entities = await _trainingRepository.GetAsync();
            var list = _mapper.Map<List<TrainingResponse>>(entities);
            await EnrichWithReviewStatsAsync(list);
            return list;
        }

        public override async Task<TrainingResponse?> GetByIdAsync(int id)
        {
            var entity = await _trainingRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var response = _mapper.Map<TrainingResponse>(entity);
            await EnrichWithReviewStatsAsync(new List<TrainingResponse> { response });
            return response;
        }

        public async Task<List<TrainingResponse>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default)
        {
            var entities = await _trainingRepository.GetByTrainingTypeIdAsync(trainingTypeId, cancellationToken);
            var list = _mapper.Map<List<TrainingResponse>>(entities);
            await EnrichWithReviewStatsAsync(list);
            return list;
        }

        public async Task<List<TrainingResponse>> SearchAsync(TrainingSearchRequest request, CancellationToken cancellationToken = default)
        {
            var entities = await _trainingRepository.SearchAsync(
                request.Name, request.MinPrice, request.MaxPrice, request.TrainingTypeId, request.Difficulty, cancellationToken);
            var list = _mapper.Map<List<TrainingResponse>>(entities);
            await EnrichWithReviewStatsAsync(list);
            return list;
        }

        public async Task<List<TrainingResponse>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new List<TrainingResponse>();
            var list = new List<TrainingResponse>();
            foreach (var id in idList)
            {
                var r = await GetByIdAsync(id);
                if (r != null) list.Add(r);
            }
            return list;
        }

        private async Task EnrichWithReviewStatsAsync(List<TrainingResponse> list)
        {
            if (list.Count == 0) return;
            var ids = list.Select(t => t.Id).ToList();
            var stats = await _reviewRepository.GetStatsByTrainingIdsAsync(ids);
            foreach (var response in list)
            {
                if (stats.TryGetValue(response.Id, out var s))
                {
                    response.AverageRating = s.AverageRating;
                    response.ReviewCount = s.ReviewCount;
                }
            }
        }
    }
}
