namespace Nile.Common.Exceptions;

public class ForbiddenException : ExceptionBase
{
    public ForbiddenException()
    {
        
    }
    public ForbiddenException(string message) : base(message)
    {
        
    }
    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}