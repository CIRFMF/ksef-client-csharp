﻿using System;
using KSeF.Client.Core.Models.Certificates;

namespace KSeFClient.Api.Builders.Certificates;


public interface ISendCertificateEnrollmentRequestBuilder
{
    ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCertificateName
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCsr
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom); // opcjonalna metoda
    SendCertificateEnrollmentRequest Build();
}


internal class SendCertificateEnrollmentRequestBuilderImpl :
    ISendCertificateEnrollmentRequestBuilder,
    ISendCertificateEnrollmentRequestBuilderWithCertificateName,
    ISendCertificateEnrollmentRequestBuilderWithCsr
{
    private string _certificateName;
    private string _csr;
    private DateTimeOffset? _validFrom;

    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        new SendCertificateEnrollmentRequestBuilderImpl();

    public ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName)
    {
        if (string.IsNullOrWhiteSpace(certificateName))
            throw new ArgumentException("CertificateName cannot be null or empty.");

        _certificateName = certificateName;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr)
    {
        if (string.IsNullOrWhiteSpace(csr))
            throw new ArgumentException("CSR cannot be null or empty.");

        _csr = csr;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom)
    {
        _validFrom = validFrom;
        return this;
    }

    public SendCertificateEnrollmentRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_certificateName))
            throw new InvalidOperationException("CertificateName is required.");
        if (string.IsNullOrWhiteSpace(_csr))
            throw new InvalidOperationException("CSR is required.");

        return new SendCertificateEnrollmentRequest
        {
            CertificateName = _certificateName,
            Csr = _csr,
            ValidFrom = _validFrom
        };
    }
}

public static class SendCertificateEnrollmentRequestBuilder
{
    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        SendCertificateEnrollmentRequestBuilderImpl.Create();
}
