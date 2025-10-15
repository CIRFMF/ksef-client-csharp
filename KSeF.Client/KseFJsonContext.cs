using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Token;
using System.Text.Json.Serialization;

namespace KSeF.Client;

[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(ApiExceptionContent))]
[JsonSerializable(typeof(ApiExceptionDetail))]
[JsonSerializable(typeof(InvoicePackage))]
[JsonSerializable(typeof(TokenSubjectDetails))]
[JsonSerializable(typeof(TokenIppPolicy))]
internal partial class KseFJsonContext : JsonSerializerContext
{
}
