using ImageServer.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ImageServer.Tests;

public class RequestAuthorizeMiddlewareTests
{
    [Fact]
    public async Task Invoke_PassesThroughWithoutEvaluating_WhenFilterEmpty()
    {
        var nextCalled = false;
        var auth = Substitute.For<IAuthorizationCheckService>();
        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        var middleware = CreateMiddleware(
            opts => opts.Filter = "",
            tickets,
            auth,
            async _ => { nextCalled = true; await Task.CompletedTask; });

        var context = CreateContext("https://images.example.com/protected/secret.jpg");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        tickets.DidNotReceiveWithAnyArgs().TryRead(default!);
        await auth.DidNotReceiveWithAnyArgs().IsAuthorizedAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task Invoke_SkipsAuthorization_WhenUrlDoesNotMatchFilter()
    {
        var nextCalled = false;
        var auth = Substitute.For<IAuthorizationCheckService>();
        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        var middleware = CreateMiddleware(
            opts => opts.Filter = @"https://images\.example\.com/protected/.*",
            tickets,
            auth,
            async _ => { nextCalled = true; await Task.CompletedTask; });

        var context = CreateContext("https://images.example.com/public/photo.jpg");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        await auth.DidNotReceiveWithAnyArgs().IsAuthorizedAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task Invoke_RedirectsToLogin_WhenAnonymousAndValidationDisabled()
    {
        var auth = Substitute.For<IAuthorizationCheckService>();
        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        tickets.TryRead(Arg.Any<HttpContext>()).Returns((TicketIds?)null);

        var middleware = CreateMiddleware(
            opts =>
            {
                opts.Filter = @"https://images\.example\.com/protected/.*";
                opts.RedirectTo = "https://login.example.com/";
                opts.RedirectToQS = "to";
                opts.ValidateAnonymousUsers = false;
            },
            tickets,
            auth,
            _ => throw new InvalidOperationException("next should not run"));

        var context = CreateContext("https://images.example.com/protected/secret.jpg");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.StartsWith("https://login.example.com/?to=", context.Response.Headers.Location.ToString());
        await auth.DidNotReceiveWithAnyArgs().IsAuthorizedAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task Invoke_AllowsAdminWithoutDbCheck()
    {
        var nextCalled = false;
        var auth = Substitute.For<IAuthorizationCheckService>();
        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        tickets.TryRead(Arg.Any<HttpContext>()).Returns(new TicketIds(UserId: 10, UserAdminId: 99));

        var middleware = CreateMiddleware(
            opts =>
            {
                opts.Filter = @"https://images\.example\.com/protected/.*";
                opts.Connection = "Server=.;Database=test;Trusted_Connection=True;";
            },
            tickets,
            auth,
            async _ => { nextCalled = true; await Task.CompletedTask; });

        var context = CreateContext("https://images.example.com/protected/secret.jpg");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        await auth.DidNotReceiveWithAnyArgs().IsAuthorizedAsync(default!, default, default!, default!, default);
    }

    [Fact]
    public async Task Invoke_CallsAuthorizationService_ForAuthenticatedNonAdmin()
    {
        var nextCalled = false;
        var auth = Substitute.For<IAuthorizationCheckService>();
        auth.IsAuthorizedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        tickets.TryRead(Arg.Any<HttpContext>()).Returns(new TicketIds(UserId: 55, UserAdminId: 0));

        var middleware = CreateMiddleware(
            opts =>
            {
                opts.Filter = @"https://images\.example\.com/protected/.*";
                opts.Connection = "Server=.;Database=test;Trusted_Connection=True;";
            },
            tickets,
            auth,
            async _ => { nextCalled = true; await Task.CompletedTask; });

        var context = CreateContext("https://images.example.com/protected/secret.jpg?w=100");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        await auth.Received(1).IsAuthorizedAsync(
            Arg.Is<string>(u => u.Contains("/protected/secret.jpg")),
            55,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_RedirectsTo403_WhenAuthenticatedUserDenied()
    {
        var auth = Substitute.For<IAuthorizationCheckService>();
        auth.IsAuthorizedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var tickets = Substitute.For<ILegacyFormsAuthTicketReader>();
        tickets.TryRead(Arg.Any<HttpContext>()).Returns(new TicketIds(UserId: 55, UserAdminId: 0));

        var middleware = CreateMiddleware(
            opts =>
            {
                opts.Filter = @"https://images\.example\.com/protected/.*";
                opts.Connection = "Server=.;Database=test;Trusted_Connection=True;";
                opts.RedirectTo403 = "https://app.example.com/forbidden";
                opts.RedirectToQS = "to";
            },
            tickets,
            auth,
            _ => throw new InvalidOperationException("next should not run"));

        var context = CreateContext("https://images.example.com/protected/secret.jpg");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.StartsWith("https://app.example.com/forbidden?to=", context.Response.Headers.Location.ToString());
    }

    private static RequestAuthorizeMiddleware CreateMiddleware(
        Action<SgAuthorizeOptions> configure,
        ILegacyFormsAuthTicketReader tickets,
        IAuthorizationCheckService auth,
        RequestDelegate next)
    {
        var options = new SgAuthorizeOptions();
        configure(options);
        return new RequestAuthorizeMiddleware(
            next,
            Options.Create(options),
            tickets,
            auth,
            NullLogger<RequestAuthorizeMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string absoluteUrl)
    {
        var uri = new Uri(absoluteUrl);
        var context = new DefaultHttpContext();
        context.Request.Scheme = uri.Scheme;
        // Avoid default :443/:80 in Host so filter regexes match public URLs
        context.Request.Host = uri.IsDefaultPort
            ? new HostString(uri.Host)
            : new HostString(uri.Host, uri.Port);
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        context.Response.Body = new MemoryStream();
        return context;
    }
}

