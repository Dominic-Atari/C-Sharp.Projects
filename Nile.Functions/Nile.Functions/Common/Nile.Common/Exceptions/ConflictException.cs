namespace Nile.Common.Exceptions;

public class ConflictException : ExceptionBase
{
    public ConflictException()
    {
        
    }
    public ConflictException(string message) : base(message)
    {
        
    }
    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}