using KSeF.Client.Core.Infrastructure.Rest;

namespace KSeF.Client.Tests.Core.UnitTests;

public class RoutesTests
{
    [Theory]
    [InlineData(Routes.TestData.BlockContext, "/v2/testdata/context/block")]
    [InlineData(Routes.TestData.UnblockContext, "/v2/testdata/context/unblock")]
    [InlineData(Routes.Invoices.Exports, "/v2/invoices/exports")]
    public void Build_WithDefaultVersion_ReturnsOpenApiPath(string route, string expectedPath)
    {
        RouteBuilder routeBuilder = new(defaultVersion: "v2");

        string path = routeBuilder.Build(route);

        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void Build_InvoiceExportByReference_ReturnsOpenApiPath()
    {
        RouteBuilder routeBuilder = new(defaultVersion: "v2");

        string path = routeBuilder.Build(Routes.Invoices.ExportByReference("ref"));

        Assert.Equal("/v2/invoices/exports/ref", path);
    }
}
