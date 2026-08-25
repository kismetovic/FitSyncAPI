namespace FITSync.Infrastructure.Exceptions;

/// <summary>
/// A violated business rule, as opposed to an unexpected fault. The API maps these to
/// 4xx responses with a stable machine-readable code, so the Flutter apps can react to
/// the code instead of parsing English prose.
/// </summary>
public class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string code, string message) : base(message)
    {
        Code = code;
    }

    public BusinessRuleException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}

/// <summary>The caller is authenticated but is not allowed to touch this particular record.</summary>
public class ForbiddenOperationException : Exception
{
    public string Code { get; }

    public ForbiddenOperationException(string message, string code = "FORBIDDEN") : base(message)
    {
        Code = code;
    }
}

/// <summary>The addressed record does not exist (or is soft-deleted).</summary>
public class NotFoundException : Exception
{
    public string Code { get; }

    public NotFoundException(string message, string code = "NOT_FOUND") : base(message)
    {
        Code = code;
    }
}
