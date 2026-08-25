using FITSync.Contracts.Common;
using FITSync.Contracts.Payments;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// The client can start a PayPal order for its own reservation and ask the server to
    /// capture it. It cannot state an amount, and it cannot declare a payment successful.
    /// Listing all payments and confirming cash are administrator operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : BaseCRUDController<PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>
    {
        private readonly IPaymentService _paymentService;
        private readonly ICaller _caller;

        public PaymentsController(IPaymentService service, ICaller caller) : base(service)
        {
            _paymentService = service;
            _caller = caller;
        }

        // ------------------------------------------------------------------
        // Client-facing
        // ------------------------------------------------------------------

        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<List<PaymentResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            var list = await _paymentService.GetByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Starts a PayPal checkout. The request names only the reservation; the amount is
        /// read from that reservation on the server.
        /// </summary>
        [HttpPost("paypal/create-order")]
        [Authorize]
        public async Task<ActionResult<CreatePayPalOrderResponse>> CreatePayPalOrder(
            [FromBody] CreatePayPalOrderRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.CreatePayPalOrderAsync(_caller.RequireUserId(), request.ReservationId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Captures the approved order. The server verifies status, amount, currency and
        /// reservation reference with PayPal before recording anything, then marks the
        /// reservation paid in the same transaction. Safe to call twice.
        /// </summary>
        [HttpPost("paypal/capture")]
        [Authorize]
        public async Task<ActionResult<CapturePayPalOrderResponse>> CapturePayPalOrder(
            [FromBody] CapturePayPalOrderRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.CapturePayPalOrderAsync(
                _caller.RequireUserId(), request.OrderId, request.ReservationId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Records that the client intends to pay on arrival. The reservation stays unpaid
        /// until an administrator confirms the cash was collected.
        /// </summary>
        [HttpPost("cash/select")]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> SelectCash(
            [FromBody] SelectCashPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.SelectCashPaymentAsync(_caller.RequireUserId(), request.ReservationId, cancellationToken);
            return Ok(result);
        }

        // ------------------------------------------------------------------
        // Paying for a bought package. Deliberately the same two ways a booking is
        // paid for, with the same server-side verification: the client names the
        // package, never the amount.
        // ------------------------------------------------------------------

        [HttpPost("membership/paypal/create-order")]
        [Authorize]
        public async Task<ActionResult<CreatePayPalOrderResponse>> CreateMembershipPayPalOrder(
            [FromBody] CreateMembershipPayPalOrderRequest request, CancellationToken cancellationToken = default)
            => Ok(await _paymentService.CreateMembershipPayPalOrderAsync(
                _caller.RequireUserId(), request.MembershipId, cancellationToken));

        [HttpPost("membership/paypal/capture")]
        [Authorize]
        public async Task<ActionResult<CapturePayPalOrderResponse>> CaptureMembershipPayPalOrder(
            [FromBody] CaptureMembershipPayPalOrderRequest request, CancellationToken cancellationToken = default)
            => Ok(await _paymentService.CaptureMembershipPayPalOrderAsync(
                _caller.RequireUserId(), request.OrderId, request.MembershipId, cancellationToken));

        [HttpPost("membership/cash/select")]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> SelectMembershipCash(
            [FromBody] CreateMembershipPayPalOrderRequest request, CancellationToken cancellationToken = default)
            => Ok(await _paymentService.SelectMembershipCashPaymentAsync(
                _caller.RequireUserId(), request.MembershipId, cancellationToken));

        /// <summary>Only staff can say that cash was actually taken.</summary>
        [HttpPost("membership/cash/confirm")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PaymentResponse>> ConfirmMembershipCash(
            [FromBody] ConfirmMembershipCashPaymentRequest request, CancellationToken cancellationToken = default)
            => Ok(await _paymentService.ConfirmMembershipCashPaymentAsync(
                _caller.RequireUserId(), request.MembershipId, request.Note, cancellationToken));

        [HttpGet("by-reservation/{reservationId:int}")]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> GetByReservationId(int reservationId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentService.GetByReservationIdAsync(reservationId, cancellationToken);
            if (payment == null) return NotFound();

            if (!_caller.IsAdministrator && !await _paymentService.IsOwnedByAsync(payment.Id, _caller.RequireUserId(), cancellationToken))
                return Forbid();

            return Ok(payment);
        }

        // ------------------------------------------------------------------
        // Administrative
        // ------------------------------------------------------------------

        /// <summary>Confirms cash received at the desk. This is what marks the reservation paid.</summary>
        [HttpPost("cash/confirm")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PaymentResponse>> ConfirmCash(
            [FromBody] ConfirmCashPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.ConfirmCashPaymentAsync(
                _caller.RequireUserId(), request.ReservationId, request.Note, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<List<PaymentResponse>>> GetAsync()
        {
            var result = await _paymentService.SearchAsync(new PaymentSearchRequest(), default);
            return Ok(result.Items);
        }

        /// <summary>Captured revenue and per-provider counts for the admin summary cards.</summary>
        [HttpGet("summary")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PaymentSummaryResponse>> GetSummary(CancellationToken cancellationToken = default)
            => Ok(await _paymentService.GetSummaryAsync(cancellationToken));

        [HttpGet("search")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PagedResult<PaymentResponse>>> Search(
            [FromQuery] PaymentSearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _paymentService.SearchAsync(request ?? new PaymentSearchRequest(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<PaymentResponse>> GetByIdAsync(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null) return NotFound();

            if (!_caller.IsAdministrator && !await _paymentService.IsOwnedByAsync(id, _caller.RequireUserId()))
                return Forbid();

            return Ok(payment);
        }

        [HttpGet("by-transaction/{transactionId}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PaymentResponse>> GetByTransactionId(string transactionId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentService.GetByTransactionIdAsync(transactionId, cancellationToken);
            return payment == null ? NotFound() : Ok(payment);
        }

        /// <summary>Back-office correction only. Clients pay through the PayPal or cash flows.</summary>
        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<PaymentResponse>> InsertAsync([FromBody] PaymentInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<PaymentResponse>> UpdateAsync(int id, [FromBody] PaymentUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
