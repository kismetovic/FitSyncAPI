using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Reviews;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Services
{
    public class ReviewService : BaseCRUDService<Review, ReviewResponse, ReviewInsertRequest, ReviewUpdateRequest>, IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IUserActionService _userActionService;
        private readonly FitSyncDbContext _context;

        public ReviewService(
            IReviewRepository repository,
            IMapper mapper,
            IReservationRepository reservationRepository,
            IUserActionService userActionService,
            FitSyncDbContext context)
            : base(repository, mapper)
        {
            _reviewRepository = repository;
            _reservationRepository = reservationRepository;
            _userActionService = userActionService;
            _context = context;
        }

        /// <summary>
        /// A review is only accepted when three things hold: the reservation belongs to the
        /// caller, the training was actually attended (Completed, or Paid and already over),
        /// and that reservation has not been reviewed before.
        /// </summary>
        public async Task<ReviewResponse> CreateForUserAsync(
            int authorUserId,
            ReviewInsertRequest request,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId)
                ?? throw new NotFoundException("Reservation not found.");

            if (reservation.UserId != authorUserId)
                throw new ForbiddenOperationException("You can only review your own reservations.");

            if (!IsAttended(reservation))
            {
                throw new BusinessRuleException(
                    "TRAINING_NOT_ATTENDED",
                    "You can only review a training you have paid for and attended. " +
                    $"This reservation is in status {reservation.Status}.");
            }

            if (await _reviewRepository.ExistsForReservationAsync(reservation.Id, cancellationToken))
                throw new BusinessRuleException("ALREADY_REVIEWED", "You have already reviewed this training session.");

            var review = await _reviewRepository.InsertAsync(new Review
            {
                Rating = request.Rating,
                Comment = request.Comment,
                UserId = authorUserId,
                TrainingId = reservation.TrainingId,
                ReservationId = reservation.Id
            });

            await _userActionService.LogActionAsync(
                authorUserId, UserActionType.ReviewedTraining, reservation.TrainingId,
                reservation.Training?.TrainingTypeId, $"rating={request.Rating}", cancellationToken);

            var saved = await _reviewRepository.GetByIdAsync(review.Id);
            return _mapper.Map<ReviewResponse>(saved ?? review);
        }

        /// <summary>
        /// Base CRUD insert has no authenticated author, so it is not a legal entry point.
        /// The controller calls CreateForUserAsync with the id from the token.
        /// </summary>
        public override Task<ReviewResponse> InsertAsync(ReviewInsertRequest request)
            => throw new BusinessRuleException(
                "AUTHOR_REQUIRED",
                "Reviews must be created through the authenticated endpoint so the author comes from the token.");

        public override async Task<ReviewResponse?> UpdateAsync(int id, ReviewUpdateRequest request)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null) return null;

            // Only the content changes. UserId, TrainingId and ReservationId stay as written.
            review.Rating = request.Rating;
            review.Comment = request.Comment;

            await _reviewRepository.UpdateAsync(review);
            return _mapper.Map<ReviewResponse>(review);
        }

        public async Task<PagedResult<ReviewResponse>> SearchAsync(
            int? trainingId, int? userId, string? searchTerm, PagedRequest paging, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _reviewRepository.SearchAsync(
                trainingId, userId, searchTerm, paging.Skip, paging.Take, cancellationToken);
            return PagedResult<ReviewResponse>.Create(
                _mapper.Map<List<ReviewResponse>>(items), paging.Page, paging.PageSize, total);
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

        public async Task<bool> IsOwnedByAsync(int reviewId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .AnyAsync(r => r.Id == reviewId && r.UserId == userId && !r.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Completed is the normal case. Paid-and-in-the-past is also accepted so a user is
        /// not blocked from reviewing just because staff have not run the completion step yet.
        /// </summary>
        private static bool IsAttended(Reservation reservation)
        {
            if (reservation.Status == ReservationStatus.Completed)
                return true;

            if (reservation.Status != ReservationStatus.Paid)
                return false;

            var end = reservation.ReservationDate.AddMinutes(reservation.Training?.DurationMinutes ?? 0);
            return end <= DateTime.UtcNow;
        }
    }
}
