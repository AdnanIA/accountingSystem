using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class JournalEntriesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journalEntryService;

    public JournalEntriesController(IApplicationDbContext db, IJournalEntryService journalEntryService)
    {
        _db = db;
        _journalEntryService = journalEntryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<JournalEntryDto>>> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var query = _db.JournalEntries.Where(e => e.CompanyId == CompanyId);
        if (from.HasValue) query = query.Where(e => e.EntryDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.EntryDate <= to.Value);

        var entries = await query.Include(e => e.Lines).ThenInclude(l => l.Account)
            .OrderByDescending(e => e.EntryDate).ThenByDescending(e => e.EntryNumber)
            .Take(200)
            .ToListAsync(ct);

        return Ok(entries.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JournalEntryDto>> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _db.JournalEntries.Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == CompanyId, ct);
        if (entry is null) return NotFound();
        return Ok(ToDto(entry));
    }

    [HttpPost]
    public async Task<ActionResult<JournalEntryDto>> Create(CreateJournalEntryRequest request, CancellationToken ct)
    {
        var entry = await _journalEntryService.CreateAndPostAsync(CompanyId, request, CurrentUserName, ct);
        await _db.SaveChangesAsync(ct);

        var full = await _db.JournalEntries.Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstAsync(e => e.Id == entry.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToDto(full));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<JournalEntryDto>> Reverse(Guid id, [FromQuery] DateTime? reversalDate, CancellationToken ct)
    {
        var reversal = await _journalEntryService.ReverseAsync(id, reversalDate ?? DateTime.UtcNow.Date, CurrentUserName, ct);
        await _db.SaveChangesAsync(ct);

        var full = await _db.JournalEntries.Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstAsync(e => e.Id == reversal.Id, ct);
        return Ok(ToDto(full));
    }

    private static JournalEntryDto ToDto(Domain.Entities.Accounting.JournalEntry e) => new(
        e.Id, e.EntryNumber, e.EntryDate, e.Status, e.SourceType, e.Memo, e.TotalDebit, e.TotalCredit,
        e.Lines.OrderBy(l => l.LineNumber).Select(l => new JournalEntryLineDto(l.AccountId, l.Account.Code, l.Account.Name, l.Debit, l.Credit, l.Description)).ToList());
}
