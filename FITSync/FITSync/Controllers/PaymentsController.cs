using FITSync.Contracts.Payments;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : BaseCRUDController<PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPayPalPaymentService _payPalPaymentService;
        private readonly ICaller _caller;

        public PaymentsController(
            IPaymentService service,
            IPayPalPaymentService payPalPaymentService,
            ICaller caller) : base(service)
        {
            _paymentService = service;
            _payPalPaymentService = payPalPaymentService;
            _caller = caller;
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<PaymentResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_caller.UserId)) return Unauthorized();
            var list = await _paymentService.GetByUserIdAsync(int.Parse(_caller.UserId), cancellationToken);
            return Ok(list);
        }

        [HttpPost("paypal/create-order")]
        public async Task<ActionResult<CreatePayPalOrderResponse>> CreatePayPalOrder([FromBody] CreatePayPalOrderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _payPalPaymentService.CreateOrderAsync(request.Amount, request.Currency, request.ReservationId, cancellationToken);
                return Ok(new CreatePayPalOrderResponse { OrderId = result.OrderId, ApprovalUrl = result.ApprovalUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "PayPal payment service is unavailable.", detail = ex.Message });
            }
        }

        [HttpPost("paypal/capture")]
        public async Task<ActionResult<CapturePayPalOrderResponse>> CapturePayPalOrder([FromBody] CapturePayPalOrderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _payPalPaymentService.CaptureOrderAsync(request.OrderId, cancellationToken);
                return Ok(new CapturePayPalOrderResponse { TransactionId = result.TransactionId, Status = result.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { error = "PayPal payment service is unavailable.", detail = ex.Message });
            }
        }

        [HttpPost("confirm")]
        public async Task<ActionResult<PaymentResponse>> ConfirmPayment([FromBody] PaymentInsertRequest request, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentService.InsertAsync(request);
            return CreatedAtAction(nameof(GetByReservationId), new { reservationId = request.ReservationId }, payment);
        }

        [HttpGet("by-reservation/{reservationId}")]
        public async Task<ActionResult<PaymentResponse>> GetByReservationId(int reservationId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentService.GetByReservationIdAsync(reservationId, cancellationToken);
            if (payment == null)
                return NotFound();
            return Ok(payment);
        }

        [HttpGet("by-transaction/{transactionId}")]
        public async Task<ActionResult<PaymentResponse>> GetByTransactionId(string transactionId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentService.GetByTransactionIdAsync(transactionId, cancellationToken);
            if (payment == null)
                return NotFound();
            return Ok(payment);
        }
    }
}
