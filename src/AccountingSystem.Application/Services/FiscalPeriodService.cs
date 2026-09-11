using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IFiscalPeriodService
{
    Task<FiscalPeriod> GetOpenPeriodForDateAsync(Guid companyId, DateTime date, CancellationToken ct = default);
}

public class FiscalPeriodService : IFiscalPeriodService
{
    private readonly IApplicationDbContext _db;

    public FiscalPeriodService(IApplicationDbContext db) => _db = db;

    public async Task<FiscalPeriod> GetOpenPeriodForDateAsync(Guid companyId, DateTime date, CancellationToken ct = default)
    {
        var period = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && date >= p.StartDate && date <= p.EndDate, ct);

        if (period is null)
            throw new DomainException($"No fiscal period is defined for {date:yyyy-MM-dd}.");

        if (period.Status == FiscalPeriodStatus.Closed)
            throw new DomainException($"Fiscal period '{period.Name}' is closed. Choose a date in an open period.");

        return period;
    }
}
