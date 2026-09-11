using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class CustomerPaymentsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICustomerPaymentService _paymentService;

    public CustomerPaymentsController(IApplicationDbContext db, ICustomerPaymentService paymentService)
    {
        _db = db;
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerPaymentDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = _db.CustomerPayments.Where(p => p.CompanyId == CompanyId);
        if (customerId.HasValue) query = query.Where(p => p.CustomerId == customerId.Value);

        var payments = await query.OrderByDescending(p => p.PaymentDate).Take(200)
            .Select(p => new CustomerPaymentDto(p.Id, p.PaymentNumber, p.CustomerId, p.PaymentDate, p.Amount, p.UnappliedAmount))
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerPaymentDto>> Create(RecordCustomerPaymentRequest request, CancellationToken ct)
    {
        var payment = await _paymentService.RecordAndApplyAsync(CompanyId, request, CurrentUserName, ct);
        var dto = new CustomerPaymentDto(payment.Id, payment.PaymentNumber, payment.CustomerId, payment.PaymentDate, payment.Amount, payment.UnappliedAmount);
        return StatusCode(StatusCodes.Status201Created, dto);
    }
}
