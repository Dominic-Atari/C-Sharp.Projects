namespace Nile.Common.Exceptions;

public class ExternalTimeoutException : ExceptionBase
{
    public ExternalTimeoutException()
    {
        
    }
    public ExternalTimeoutException(string message) : base(message)
    {
        
    }
    public ExternalTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}