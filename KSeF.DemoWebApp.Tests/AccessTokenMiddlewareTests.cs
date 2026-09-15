using KSeF.DemoWebApp.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace KSeF.DemoWebApp.Tests;

/// <summary>
/// Testy jednostkowe AccessTokenMiddleware (ujednolicenie przekazywania tokenu z nagłówka Authorization do query).
/// </summary>
public sealed class AccessTokenMiddlewareTests
{
	private static async Task<HttpContext> InvokeAsync(Action<HttpContext> arrange)
	{
		DefaultHttpContext context = new();
		arrange(context);

		AccessTokenMiddleware middleware = new(_ => Task.CompletedTask);
		await middleware.InvokeAsync(context).ConfigureAwait(false);

		return context;
	}

	[Fact]
	public async Task InvokeAsync_WithBearerHeader_InjectsAccessTokenAndTokenIntoQuery()
	{
		HttpContext context = await InvokeAsync(ctx =>
			ctx.Request.Headers.Authorization = "Bearer abc123");

		Assert.Equal("abc123", context.Request.Query["accessToken"]);
		Assert.Equal("abc123", context.Request.Query["token"]);
		Assert.Equal("abc123", context.Request.Headers.Authorization.ToString());
	}

	[Fact]
	public async Task InvokeAsync_WithoutBearerPrefix_UsesRawHeaderValueAsToken()
	{
		HttpContext context = await InvokeAsync(ctx =>
			ctx.Request.Headers.Authorization = "abc123");

		Assert.Equal("abc123", context.Request.Query["accessToken"]);
		Assert.Equal("abc123", context.Request.Query["token"]);
	}

	[Fact]
	public async Task InvokeAsync_WithExplicitAccessTokenInQuery_DoesNotOverrideIt()
	{
		HttpContext context = await InvokeAsync(ctx =>
		{
			ctx.Request.Headers.Authorization = "Bearer fromHeader";
			ctx.Request.QueryString = QueryString.Create("accessToken", "fromQuery");
		});

		Assert.Equal("fromQuery", context.Request.Query["accessToken"]);
		// Parametr "token" nie został podany jawnie, więc przyjmuje wartość z nagłówka.
		Assert.Equal("fromHeader", context.Request.Query["token"]);
	}

	[Fact]
	public async Task InvokeAsync_PreservesOtherQueryParameters()
	{
		HttpContext context = await InvokeAsync(ctx =>
		{
			ctx.Request.Headers.Authorization = "Bearer abc123";
			ctx.Request.QueryString = QueryString.Create("pageSize", "10");
		});

		Assert.Equal("10", context.Request.Query["pageSize"]);
		Assert.Equal("abc123", context.Request.Query["accessToken"]);
	}

	[Fact]
	public async Task InvokeAsync_WithoutAuthorizationHeader_LeavesQueryUnchanged()
	{
		HttpContext context = await InvokeAsync(ctx =>
			ctx.Request.QueryString = QueryString.Create("accessToken", "already-set"));

		Assert.Equal("already-set", context.Request.Query["accessToken"]);
		Assert.False(context.Request.Query.ContainsKey("token"));
	}
}
