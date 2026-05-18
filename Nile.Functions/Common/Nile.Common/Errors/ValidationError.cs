namespace Nile.Common.Errors;

public class ValidationError : ErrorBase
{ 
    public required Dictionary<string, string[]> Errors { get; set; }
}