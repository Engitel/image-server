using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ImageServer.Authorization;

public sealed class RequestAuthorizeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SgAuthorizeOptions _options;
    private readonly ILegacyFormsAuthTicketReader _ticketReader;
    private readonly IAuthorizationCheckService _authorizationCheck;
    private readonly ILogger<RequestAuthorizeMiddleware> _logger;
    private readonly Regex? _filter;

    public RequestAuthorizeMiddleware(
        RequestDelegate next,
        IOptions<SgAuthorizeOptions> options,
        ILegacyFormsAuthTicketReader ticketReader,
        IAuthorizationCheckService authorizationCheck,
        ILogger<RequestAuthorizeMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _ticketReader = ticketReader;
        _authorizationCheck = authorizationCheck;
        _logger = logger;

        if (_options.IsFilterConfigured)
        {
            _filter = new Regex(_options.Filter, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        if (_options.IsFilterConfigured && !_options.HasConnectionString)
        {
            _logger.LogError("SGAuthorize Connection is empty; authorization checks will fail for protected URLs.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // No Filter configured → pass through without evaluating the request URL
        if (_filter is null)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var requestUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";


        if (!_filter.IsMatch(requestUrl))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        _logger.LogDebug("OnAuthorizeRequest: {RequestUrl}", requestUrl);
        var ticketIds = _ticketReader.TryRead(context);
        var userId = ticketIds?.UserId ?? 0;
        var userAdminId = ticketIds?.UserAdminId ?? 0;
        var isAuthenticated = ticketIds.HasValue;

        if (isAuthenticated)
        {
            _logger.LogDebug("UserId: {UserId}, UserAdminId: {UserAdminId}", userId, userAdminId);
        }
        else if (_options.ValidateAnonymousUsers)
        {
            _logger.LogDebug("Anonymous user validation enabled.");
        }
        else
        {
            await RedirectToLoginAsync(context).ConfigureAwait(false);
            return;
        }

        if (userAdminId != 0)
        {
            _logger.LogDebug("User is admin, skipping authorization check.");
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!_options.HasConnectionString)
        {
            _logger.LogError("Unable to connect to database");
            throw new InvalidOperationException("Unable to connect to database");
        }

        var filePath = context.Request.Path.Value ?? string.Empty;
        var rawUrl = GetRawUrl(context);

        var authorized = await _authorizationCheck
            .IsAuthorizedAsync(requestUrl, userId, filePath, rawUrl, context.RequestAborted)
            .ConfigureAwait(false);

        if (!authorized)
        {
            await RedirectUnauthorizedAsync(context, userId).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private async Task RedirectUnauthorizedAsync(HttpContext context, int userId)
    {
        if (userId > 0)
        {
            var target403 = string.IsNullOrWhiteSpace(_options.RedirectTo403)
                ? _options.RedirectTo
                : _options.RedirectTo403;
            await RedirectAsync(context, BuildRedirectUrl(target403, GetRawUrl(context))).ConfigureAwait(false);
            return;
        }

        if (_options.RedirectToStatusCode == 302)
        {
            await RedirectAsync(context, BuildRedirectUrl(_options.RedirectTo, GetRawUrl(context))).ConfigureAwait(false);
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = _options.RedirectToStatusCode;
    }

    private async Task RedirectToLoginAsync(HttpContext context)
    {
        var target = BuildRedirectUrl(_options.RedirectTo, GetRawUrl(context));
        _logger.LogDebug("Redirecting to: {Target}", target);
        await RedirectAsync(context, target).ConfigureAwait(false);
    }

    private string BuildRedirectUrl(string baseUrl, string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return rawUrl;
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return baseUrl + separator + _options.RedirectToQS + "=" + Uri.EscapeDataString(rawUrl);
    }

    private static async Task RedirectAsync(HttpContext context, string target)
    {
        context.Response.Redirect(target);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static string GetRawUrl(HttpContext context)
    {
        var path = context.Request.PathBase.Add(context.Request.Path);
        return path + context.Request.QueryString;
    }
}

