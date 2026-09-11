using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Application.DTOs;

public record BillLineRequest(Guid? ItemId, string Description, decimal Quantity, decimal UnitCost, Guid? TaxCodeId, Guid? ExpenseAccountId);

public record CreateBillRequest(Guid VendorId, string? VendorInvoiceNumber, DateTime BillDate, DateTime DueDate, string? Memo, List<BillLineRequest> Lines, string CurrencyCode = "USD", decimal ExchangeRateToBase = 1m);

public record BillLineDto(Guid Id, string Description, decimal Quantity, decimal UnitCost, decimal TaxAmount, decimal LineTotal);

public record BillDto(
    Guid Id, string BillNumber, Guid VendorId, string VendorName, DateTime BillDate, DateTime DueDate,
    BillStatus Status, decimal SubTotal, decimal TaxTotal, decimal Total, decimal AmountPaid, decimal Balance,
    List<BillLineDto> Lines);

public record ApplyBillPaymentRequest(Guid BillId, decimal Amount);

public record RecordVendorPaymentRequest(
    Guid VendorId, DateTime PaymentDate, decimal Amount, PaymentMethod Method, string? ReferenceNumber,
    Guid BankAccountId, List<ApplyBillPaymentRequest> Applications, string? Memo);

public record VendorPaymentDto(Guid Id, string PaymentNumber, Guid VendorId, DateTime PaymentDate, decimal Amount, decimal UnappliedAmount);

public record CreateVendorCreditRequest(Guid VendorId, DateTime Date, decimal Amount, string? Reason);
