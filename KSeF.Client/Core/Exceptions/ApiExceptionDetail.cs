namespace KSeFClient.Core.Exceptions;

/// <summary>
/// Represents a single exception detail within an API error response.
/// </summary>
public class ApiExceptionDetail
{
    /// <summary>
    /// Numeric code representing the type of exception.
    /// </summary>
    public int ExceptionCode { get; set; }

    /// <summary>
    /// Human-readable description of the exception.
    /// </summary>
    public string ExceptionDescription { get; set; }

    /// <summary>
    /// Optional list of additional context messages.
    /// </summary>
    public List<string> Details { get; set; }
}
