using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Memberships;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    /// <summary>
    /// Gives ReservationType.Monthly its business meaning: a package with a validity
    /// period, a session budget and its own price, which monthly reservations draw down.
    /// </summary>
    public class MembershipService : BaseCRUDService<MembershipPackage, MembershipPackageResponse, MembershipPackageInsertRequest, MembershipPackageUpdateRequest>, IMembershipService
    {
        private readonly IMembershipPackageRepository _packageRepository;
        private readonly IUserMembershipRepository _userMembershipRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationDispatcher _dispatcher;

        public MembershipService(
            IMembershipPackageRepository repository,
            IMapper mapper,
            IUserMembershipRepository userMembershipRepository,
            IUserRepository userRepository,
            INotificationDispatcher dispatcher)
            : base(repository, mapper)
        {
            _packageRepository = repository;
            _userMembershipRepository = userMembershipRepository;
            _userRepository = userRepository;
            _dispatcher = dispatcher;
        }

        public async Task<List<MembershipPackageResponse>> GetActivePackagesAsync(CancellationToken cancellationToken = default)
        {
            var packages = await _packageRepository.GetActiveAsync(cancellationToken);
            return _mapper.Map<List<MembershipPackageResponse>>(packages);
        }

        public async Task<PagedResult<UserMembershipResponse>> GetMyMembershipsAsync(
            int userId, PagedRequest paging, CancellationToken cancellationToken = default)
        {
            // Keep the list honest: anything past its end date is reported as expired.
            await _userMembershipRepository.ExpireOutdatedAsync(DateTime.UtcNow, cancellationToken);

            var all = await _userMembershipRepository.GetByUserIdAsync(userId, cancellationToken);
            var page = all.Skip(paging.Skip).Take(paging.Take).ToList();

            return PagedResult<UserMembershipResponse>.Create(
                page.Select(ToResponse).ToList(), paging.Page, paging.PageSize, all.Count);
        }

        /// <summary>
        /// Buying a package. Price, duration and session count are read from the package
        /// row, never taken from the request.
        ///
        /// The package is created <b>unpaid</b>. It used to be created Active on the spot,
        /// so a single tap handed out a usable package and no Payment row ever existed —
        /// which is why bought packages never appeared under "my payments". Paying for it
        /// goes through the same PayPal and cash endpoints a booking uses.
        /// </summary>
        public async Task<UserMembershipResponse> PurchaseAsync(
            int userId, PurchaseMembershipRequest request, CancellationToken cancellationToken = default)
        {
            var package = await _packageRepository.GetByIdAsync(request.MembershipPackageId)
                ?? throw new NotFoundException("Membership package not found.");

            if (!package.IsActive)
                throw new BusinessRuleException("PACKAGE_INACTIVE", "This membership package is no longer on sale.");

            var start = (request.StartDate ?? DateTime.UtcNow).Date;
            if (start < DateTime.UtcNow.Date)
                throw new BusinessRuleException("START_IN_PAST", "A membership cannot start in the past.");

            var end = start.AddDays(package.DurationDays);
            await EnsureNoOverlappingPackageAsync(userId, package, start, end, cancellationToken);

            var membership = await _userMembershipRepository.InsertAsync(new UserMembership
            {
                UserId = userId,
                MembershipPackageId = package.Id,
                StartDate = start,
                EndDate = end,
                SessionsTotal = package.SessionCount,
                SessionsUsed = 0,
                Status = MembershipStatus.PendingPayment,
                PricePaid = package.Price
            });

            var saved = await _userMembershipRepository.GetByIdAsync(membership.Id) ?? membership;
            saved.MembershipPackage ??= package;

            var user = await _userRepository.GetByIdAsync(userId);
            await _dispatcher.DispatchAsync(
                userId, NotificationTemplates.MembershipPurchased(saved), user?.Email, true, cancellationToken);

            return ToResponse(saved);
        }

        /// <summary>
        /// Refuses a purchase that would overlap a package the client already holds.
        ///
        /// Nothing used to look at what the client owned, so the same package could be
        /// bought over and over - which is exactly what happened: sixteen packages in
        /// twenty-four seconds. Two packages are in conflict when their validity periods
        /// overlap <b>and</b> their coverage overlaps, so a general package rules out
        /// everything else while two packages tied to different training types can sit
        /// side by side.
        ///
        /// A package that is exhausted or expired does not block: that is a renewal, not
        /// a duplicate. One still waiting to be paid does block, otherwise tapping "buy"
        /// twice would leave two unpaid rows behind.
        /// </summary>
        private async Task EnsureNoOverlappingPackageAsync(
            int userId,
            MembershipPackage package,
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var existing = await _userMembershipRepository.GetByUserIdAsync(userId, cancellationToken);

            var clash = existing.FirstOrDefault(m =>
                StillCounts(m, today) && PeriodsOverlap(m, start, end) && CoverageOverlaps(m, package));

            if (clash == null) return;

            var clashName = clash.MembershipPackage?.Name ?? "an existing package";
            throw new BusinessRuleException(
                "MEMBERSHIP_OVERLAP",
                $"You already have \"{clashName}\", valid until {clash.EndDate:dd.MM.yyyy}, which covers " +
                "the same trainings. Use it up, let it expire or cancel it before buying this one.");
        }

        /// <summary>A package that could still be spent, or is waiting to be paid for.</summary>
        private static bool StillCounts(UserMembership membership, DateTime today)
        {
            if (membership.Status == MembershipStatus.PendingPayment) return true;

            return membership.Status == MembershipStatus.Active
                   && membership.EndDate.Date >= today
                   && membership.SessionsRemaining > 0;
        }

        private static bool PeriodsOverlap(UserMembership membership, DateTime start, DateTime end)
            => start <= membership.EndDate.Date && end >= membership.StartDate.Date;

        /// <summary>
        /// A package with no training type covers everything, so it overlaps anything.
        /// Two type-specific packages only clash when it is the same type.
        /// </summary>
        private static bool CoverageOverlaps(UserMembership membership, MembershipPackage package)
        {
            var existingType = membership.MembershipPackage?.TrainingTypeId;
            return existingType == null || package.TrainingTypeId == null || existingType == package.TrainingTypeId;
        }

        /// <summary>
        /// Cancelling a package the caller owns.
        ///
        /// A package that has already been drawn down cannot be cancelled: reservations
        /// point at it and were priced at zero because it covered them, so removing it
        /// would leave those bookings unexplained. An unused package can always go,
        /// whether it was paid for or is still waiting for payment.
        /// </summary>
        public async Task<UserMembershipResponse> CancelAsync(
            int callerUserId, int membershipId, CancellationToken cancellationToken = default)
        {
            var membership = await _userMembershipRepository.GetByIdAsync(membershipId)
                ?? throw new NotFoundException("Membership not found.");

            // Ownership lives here so no route can reach someone else's package.
            if (membership.UserId != callerUserId)
                throw new NotFoundException("Membership not found.");

            if (membership.Status == MembershipStatus.Cancelled)
                throw new BusinessRuleException("ALREADY_CANCELLED", "This package is already cancelled.");

            if (membership.SessionsUsed > 0)
                throw new BusinessRuleException(
                    "MEMBERSHIP_IN_USE",
                    "A package with sessions already spent cannot be cancelled. " +
                    "Cancel the reservations that used it first.");

            membership.Status = MembershipStatus.Cancelled;
            await _userMembershipRepository.UpdateAsync(membership);

            var saved = await _userMembershipRepository.GetByIdAsync(membership.Id) ?? membership;
            return ToResponse(saved);
        }

        public async Task<UserMembershipResponse?> GetUserMembershipAsync(int userId, int membershipId, CancellationToken cancellationToken = default)
        {
            var membership = await _userMembershipRepository.GetByIdAsync(membershipId);
            if (membership == null || membership.UserId != userId) return null;
            return ToResponse(membership);
        }

        private UserMembershipResponse ToResponse(UserMembership membership)
        {
            var response = _mapper.Map<UserMembershipResponse>(membership);
            response.SessionsRemaining = membership.SessionsRemaining;
            response.IsUsable = membership.IsUsableAt(DateTime.UtcNow);
            response.MembershipPackageName = membership.MembershipPackage?.Name;
            response.TrainingTypeId = membership.MembershipPackage?.TrainingTypeId;
            return response;
        }
    }
}
