using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface INumberSequenceService
{
    Task<string> GetNextNumberAsync(Guid companyId, string documentType, string prefix, CancellationToken ct = default);
}

public class NumberSequenceService : INumberSequenceService
{
    private readonly IApplicationDbContext _db;

    public NumberSequenceService(IApplicationDbContext db) => _db = db;

    public async Task<string> GetNextNumberAsync(Guid companyId, string documentType, string prefix, CancellationToken ct = default)
    {
        var sequence = await _db.NumberSequences
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.DocumentType == documentType, ct);

        if (sequence is null)
        {
            sequence = new NumberSequence { CompanyId = companyId, DocumentType = documentType, Prefix = prefix, NextNumber = 1 };
            _db.NumberSequences.Add(sequence);
        }

        var formatted = sequence.Format();
        sequence.NextNumber++;
        return formatted;
    }
}
