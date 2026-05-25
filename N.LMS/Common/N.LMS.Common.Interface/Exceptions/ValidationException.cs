using N.LMS.Common.Interface.Context;

namespace N.LMS.Common.Interface.Exceptions;

public sealed class ValidationException : ExceptionBase
{
    public string? FieldName { get; }

    public ValidationException(RequestContext? context, string message, string? fieldName = null)
        : base(context, message)
    {
        FieldName = fieldName;
    }
}
