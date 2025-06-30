namespace KSeFClient.Core.Exceptions;

/// <summary>
/// Represents a structured error response returned by the API.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// The main exception content with details.
    /// </summary>
    public ApiExceptionContent Exception { get; set; }
}
