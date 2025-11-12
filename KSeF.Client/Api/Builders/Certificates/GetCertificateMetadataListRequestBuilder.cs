using KSeF.Client.Core.Models.Certificates;

namespace KSeF.Client.Api.Builders.Certificates;

public interface IGetCertificateListRequestBuilder
{
    IGetCertificateListRequestBuilder WithCertificateSerialNumber(string serialNumber);
    IGetCertificateListRequestBuilder WithName(string name);
    IGetCertificateListRequestBuilder WithStatus(CertificateStatusEnum status);
    IGetCertificateListRequestBuilder WithExpiresAfter(DateTimeOffset expiresAfter);
    IGetCertificateListRequestBuilder WithCertificateType(CertificateType certificateType);
    CertificateMetadataListRequest Build();
}

internal class GetCertificateListRequestBuilderImpl : IGetCertificateListRequestBuilder
{
    private readonly CertificateMetadataListRequest _request = new();

    public static IGetCertificateListRequestBuilder Create() => new GetCertificateListRequestBuilderImpl();

    public IGetCertificateListRequestBuilder WithCertificateSerialNumber(string serialNumber)
    {
        _request.CertificateSerialNumber = serialNumber;
        return this;
    }

    public IGetCertificateListRequestBuilder WithName(string name)
    {
        _request.Name = name;
        return this;
    }

    public IGetCertificateListRequestBuilder WithStatus(CertificateStatusEnum status)
    {
        _request.Status = status;
        return this;
    }

    public IGetCertificateListRequestBuilder WithExpiresAfter(DateTimeOffset expiresAfter)
    {
        _request.ExpiresAfter = expiresAfter;
        return this;
    }

    public IGetCertificateListRequestBuilder WithCertificateType(CertificateType certificateType)
    {
        _request.Type = certificateType;
        return this;
    }

    public CertificateMetadataListRequest Build()
    {
        return _request;
    }
}

public static class GetCertificateMetadataListRequestBuilder
{
    public static IGetCertificateListRequestBuilder Create() =>
        GetCertificateListRequestBuilderImpl.Create();
}