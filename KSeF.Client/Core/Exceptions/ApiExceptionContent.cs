namespace KSeFClient.Core.Exceptions;

/// <summary>
/// Contains detailed exception metadata including code, description, and timestamp.
/// </summary>
public class ApiExceptionContent
{
    /// <summary>
    /// List of detailed exceptions describing individual issues.
    /// </summary>
    public List<ApiExceptionDetail> ExceptionDetailList { get; set; }

    /// <summary>
    /// Unique code representing the service instance that generated the error.
    /// </summary>
    public string ServiceCode { get; set; }

    /// <summary>
    /// Timestamp when the exception occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    public string ServiceName { get; set; }
    public string ReferenceNumber { get; set; }

    public string ServiceCtx { get; set; }
}
