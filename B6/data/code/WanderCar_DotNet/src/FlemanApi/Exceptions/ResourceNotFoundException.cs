using System.Net;

namespace FlemanApi.Exceptions;

public class ResourceNotFoundException : ApiException
{
    public ResourceNotFoundException(string message) : base(message, HttpStatusCode.NotFound) { }
}
