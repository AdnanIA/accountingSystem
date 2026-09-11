using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class VendorPaymentsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IVendorPaymentService _paymentService;

    public VendorPaymentsController(IApplicationDbContext db, IVendorPaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VendorPaymentDto>>> GetAll([FromQuery] Guid? vendorId, CancellationToken ct)
    {
        var query = _db.VendorPayments.Where(p => p.CompanyId == CompanyId);
        if (vendorId.HasValue) query = query.Where(p => p.VendorId == vendorId.Value);

        var payments = await query.OrderByDescending(p => p.PaymentDate).Take(200)
            .Select(p => new VendorPaymentDto(p.Id, p.PaymentNumber, p.VendorId, p.PaymentDate, p.Amount, p.UnappliedAmount))
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpPost]
    public async Task<ActionResult<VendorPaymentDto>> Create(RecordVendorPaymentRequest request, CancellationToken ct)
    {
        var payment = await _paymentService.RecordAndApplyAsync(CompanyId, request, CurrentUserName, ct);
        var dto = new VendorPaymentDto(payment.Id, payment.PaymentNumber, payment.VendorId, payment.PaymentDate, payment.Amount, payment.UnappliedAmount);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
