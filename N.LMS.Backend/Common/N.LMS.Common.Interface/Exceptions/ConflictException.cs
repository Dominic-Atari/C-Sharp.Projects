using N.LMS.Common.Interface.Context;

namespace N.LMS.Common.Interface.Exceptions;

public sealed class ConflictException : ExceptionBase
{
    public string EntityName { get; }
    public string FieldName { get; }

    public ConflictException(RequestContext? context, string entityName, string fieldName, string message)
        : base(context, message)
    {
        EntityName = entityName;
        FieldName = fieldName;
    }
}
