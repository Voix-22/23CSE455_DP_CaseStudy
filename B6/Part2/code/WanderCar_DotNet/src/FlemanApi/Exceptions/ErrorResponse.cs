namespace FlemanApi.Exceptions;

// Mirrors com.fleman.exception.ErrorResponse — every error the API returns
// has this exact shape: { message, status, timestamp }.
public class ErrorResponse
{
    public string Message { get; }
    public int Status { get; }
    public DateTime Timestamp { get; } = DateTime.Now;

    public ErrorResponse(string message, int status)
    {
        Message = message;
        Status = status;
    }
}
