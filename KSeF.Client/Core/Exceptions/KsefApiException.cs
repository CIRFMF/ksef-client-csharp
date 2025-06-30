using System.Net;


namespace KSeFClient.Core.Exceptions;

/// <summary>
/// Represents a structured API exception containing error details returned from the API.
/// </summary>
public class KsefApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the optional service code from the API error payload.
    /// </summary>
    public string ServiceCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KsefApiException"/> class.
    /// </summary>
    /// <param name="message">The detailed exception message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="serviceCode">Optional service code from the API.</param>
    public KsefApiException(string message, HttpStatusCode statusCode, string serviceCode = null)
        : base(message)
    {
        StatusCode = statusCode;
        ServiceCode = serviceCode;
    }
}
