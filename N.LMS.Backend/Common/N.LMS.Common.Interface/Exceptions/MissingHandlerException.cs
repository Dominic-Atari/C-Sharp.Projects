namespace N.LMS.Common.Interface.Exceptions;

public sealed class MissingHandlerException : Exception
{
    public Type ServiceType { get; }
    public Type RequestType { get; }

    public MissingHandlerException(object service, Type requestType)
        : base($"{service.GetType().Name} has no handler for request {requestType.Name}.")
    {
        ServiceType = service.GetType();
        RequestType = requestType;
    }
}
