namespace AccountingSystem.Domain.Exceptions;

/// <summary>Thrown when an operation would violate an accounting business rule (e.g. unbalanced entry, posting to a closed period).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
