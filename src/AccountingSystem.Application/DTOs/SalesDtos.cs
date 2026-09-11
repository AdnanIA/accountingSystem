using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Application.DTOs;

public record InvoiceLineRequest(Guid? ItemId, string Description, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, Guid? TaxCodeId, Guid? RevenueAccountId);

public record CreateInvoiceRequest(Guid CustomerId, DateTime InvoiceDate, DateTime DueDate, string? Memo, string? Terms, List<InvoiceLineRequest> Lines, string CurrencyCode = "USD", decimal ExchangeRateToBase = 1m);

public record InvoiceLineDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxAmount, decimal LineTotal);

public record InvoiceDto(
    Guid Id, string InvoiceNumber, Guid CustomerId, string CustomerName, DateTime InvoiceDate, DateTime DueDate,
    InvoiceStatus Status, decimal SubTotal, decimal TaxTotal, decimal Total, decimal AmountPaid, decimal Balance,
    List<InvoiceLineDto> Lines);

public record ApplyPaymentRequest(Guid InvoiceId, decimal Amount);

public record RecordCustomerPaymentRequest(
    Guid CustomerId, DateTime PaymentDate, decimal Amount, PaymentMethod Method, string? ReferenceNumber,
    Guid BankAccountId, List<ApplyPaymentRequest> Applications, string? Memo);

public record CustomerPaymentDto(Guid Id, string PaymentNumber, Guid CustomerId, DateTime PaymentDate, decimal Amount, decimal UnappliedAmount);

public record CreateCreditMemoRequest(Guid CustomerId, DateTime Date, decimal Amount, string? Reason);
