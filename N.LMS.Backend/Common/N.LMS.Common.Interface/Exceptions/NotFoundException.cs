using N.LMS.Common.Interface.Context;

namespace N.LMS.Common.Interface.Exceptions;

public sealed class NotFoundException : ExceptionBase
{
    public string EntityName { get; }
    public string FieldName { get; }
    public object FieldValue { get; }

    public NotFoundException(RequestContext? context, string entityName, string fieldName, object fieldValue)
        : base(context, $"{entityName} with {fieldName} '{fieldValue}' was not found.")
    {
        EntityName = entityName;
        FieldName = fieldName;
        FieldValue = fieldValue;
    }
}
