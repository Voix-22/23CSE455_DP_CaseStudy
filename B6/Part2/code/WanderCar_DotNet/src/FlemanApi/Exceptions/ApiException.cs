using System.Net;

namespace FlemanApi.Exceptions;

// Mirrors com.fleman.exception.ApiException — carries the HTTP status the
// global exception middleware should respond with.
public class ApiException : Exception
{
    public HttpStatusCode Status { get; }

    public ApiException(string message, HttpStatusCode status) : base(message)
    {
        Status = status;
    }
}
