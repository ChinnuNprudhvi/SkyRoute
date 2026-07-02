namespace SkyRoute.Domain.Exceptions;

public class SearchExpiredException : Exception
{
    public SearchExpiredException(string message) : base(message)
    {
    }
}
