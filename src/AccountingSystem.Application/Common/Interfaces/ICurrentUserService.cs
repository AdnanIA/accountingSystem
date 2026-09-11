namespace AccountingSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    Guid? CompanyId { get; }
    bool IsInRole(string role);
}
