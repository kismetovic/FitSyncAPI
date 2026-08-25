using FITSync.Contracts.Common;
using FITSync.Contracts.Memberships;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Monthly packages: the catalogue, and the packages a user has bought. This is what
    /// backs ReservationType.Monthly - a monthly reservation draws a session from an
    /// active package here.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipsController : BaseCRUDController<MembershipPackageResponse, MembershipPackageInsertRequest, MembershipPackageUpdateRequest>
    {
        private readonly IMembershipService _membershipService;
        private readonly ICaller _caller;

        public MembershipsController(IMembershipService service, ICaller caller) : base(service)
        {
            _membershipService = service;
            _caller = caller;
        }

        [HttpGet("packages")]
        [Authorize]
        public async Task<ActionResult<List<MembershipPackageResponse>>> GetActivePackages(CancellationToken cancellationToken = default)
            => Ok(await _membershipService.GetActivePackagesAsync(cancellationToken));

        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<PagedResult<UserMembershipResponse>>> GetMine(
            [FromQuery] PagedRequest? paging, CancellationToken cancellationToken = default)
            => Ok(await _membershipService.GetMyMembershipsAsync(_caller.RequireUserId(), paging ?? new PagedRequest(), cancellationToken));

        [HttpGet("mine/{membershipId:int}")]
        [Authorize]
        public async Task<ActionResult<UserMembershipResponse>> GetMineById(int membershipId, CancellationToken cancellationToken = default)
        {
            var result = await _membershipService.GetUserMembershipAsync(_caller.RequireUserId(), membershipId, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Buys a package for the authenticated caller. Price, duration and session count
        /// come from the package row, not from the request.
        /// </summary>
        [HttpPost("purchase")]
        [Authorize]
        public async Task<ActionResult<UserMembershipResponse>> Purchase(
            [FromBody] PurchaseMembershipRequest request, CancellationToken cancellationToken = default)
            => Ok(await _membershipService.PurchaseAsync(_caller.RequireUserId(), request, cancellationToken));

        /// <summary>
        /// A client cancelling a package they bought. Packages are cancelled, never
        /// deleted, so the record of what was bought and refunded survives.
        /// </summary>
        [HttpPatch("mine/{membershipId:int}/cancel")]
        [Authorize]
        public async Task<ActionResult<UserMembershipResponse>> Cancel(
            int membershipId, CancellationToken cancellationToken = default)
            => Ok(await _membershipService.CancelAsync(_caller.RequireUserId(), membershipId, cancellationToken));

        [HttpGet]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<List<MembershipPackageResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<MembershipPackageResponse>> GetByIdAsync(int id)
            => await base.GetByIdAsync(id);

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<MembershipPackageResponse>> InsertAsync([FromBody] MembershipPackageInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<MembershipPackageResponse>> UpdateAsync(int id, [FromBody] MembershipPackageUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
