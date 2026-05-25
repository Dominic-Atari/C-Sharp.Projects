using N.LMS.Common.Interface.Context;

namespace N.LMS.Common.Interface.Exceptions;

public abstract class ExceptionBase : Exception
{
    public RequestContext? Context { get; }

    protected ExceptionBase(RequestContext? context, string message) : base(message)
    {
        Context = context;
    }

    protected ExceptionBase(RequestContext? context, string message, Exception inner) : base(message, inner)
    {
        Context = context;
    }
}
