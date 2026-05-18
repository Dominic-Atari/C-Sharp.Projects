namespace Nile.Common.Exceptions;

public abstract class ExceptionBase : Exception
{
    public string? PublicMessage { get; set; }

    protected ExceptionBase()
    {
    }
    public ExceptionBase(string message) : base(message)
    {
    }
    public ExceptionBase(string message, Exception innerException) : base(message, innerException)
    {
    }
}