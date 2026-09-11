using AccountingSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INumberSequenceService, NumberSequenceService>();
        services.AddScoped<IFiscalPeriodService, FiscalPeriodService>();
        services.AddScoped<ISystemAccountResolver, SystemAccountResolver>();
        services.AddScoped<IJournalEntryService, JournalEntryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IBillService, BillService>();
        services.AddScoped<ICustomerPaymentService, CustomerPaymentService>();
        services.AddScoped<IVendorPaymentService, VendorPaymentService>();
        services.AddScoped<IBankReconciliationService, BankReconciliationService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IPeriodClosingService, PeriodClosingService>();
        services.AddScoped<IReportingService, ReportingService>();

        return services;
    }
}
