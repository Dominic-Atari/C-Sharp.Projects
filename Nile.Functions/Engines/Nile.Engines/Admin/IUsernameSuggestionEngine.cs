namespace Nile.Engines.Admin;

public interface IUsernameSuggestionEngine
{
    Task<string[]> GenerateUniqueSuggestions();
}