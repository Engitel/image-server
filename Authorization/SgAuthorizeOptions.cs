namespace ImageServer.Authorization;

/// <summary>
/// Configuration for request authorization. Property names mirror the SGAuthorize* keys
/// from the .NET Framework HttpModule (without the SGAuthorize prefix), plus machine-key
/// settings required to decrypt legacy Forms Authentication cookies.
/// </summary>
public class SgAuthorizeOptions
{
    public const string Section = "SGAuthorize";

    /// <summary>SGAuthorizeConnection — SQL Server connection string.</summary>
    public string Connection { get; set; } = string.Empty;

    /// <summary>SGAuthorizeProcedure — stored procedure that returns 1/0 (default: sg_AuthorizeRequest).</summary>
    public string Procedure { get; set; } = "sg_AuthorizeRequest";

    /// <summary>SGAuthorizeRedirectTo — login / unauthenticated redirect URL.</summary>
    public string RedirectTo { get; set; } = string.Empty;

    /// <summary>SGAuthorizeRedirectToStatusCode — status when RedirectTo is not a 302 redirect (default: 302).</summary>
    public int RedirectToStatusCode { get; set; } = 302;

    /// <summary>SGAuthorizeRedirectTo403 — redirect when an authenticated user is denied.</summary>
    public string RedirectTo403 { get; set; } = string.Empty;

    /// <summary>SGAuthorizeRedirectToQS — query-string key for the return URL (default: to).</summary>
    public string RedirectToQS { get; set; } = "to";

    /// <summary>SGAuthorizeFilter — regex matched against the full request URL; empty disables checks.</summary>
    public string Filter { get; set; } = string.Empty;

    /// <summary>SGAuthorizeConnectionAttempts — DB retry count (default: 5).</summary>
    public int ConnectionAttempts { get; set; } = 5;

    /// <summary>SGAuthorizeConnectionAttemptsSleep — milliseconds between retries (default: 500).</summary>
    public int ConnectionAttemptsSleep { get; set; } = 500;

    /// <summary>SGAuthorizeTicketToken — 1-based UserData token index for user id (default: 9).</summary>
    public int TicketToken { get; set; } = 9;

    /// <summary>SGAuthorizeTicketAdminToken — 1-based UserData token index for admin id (default: 1).</summary>
    public int TicketAdminToken { get; set; } = 1;

    /// <summary>SGAuthorizeTicketTokenSeparator — UserData separator (default: |).</summary>
    public string TicketTokenSeparator { get; set; } = "|";

    /// <summary>SGAuthorizeValidateAnonymousUsers — run DB check for anonymous users (default: false).</summary>
    public bool ValidateAnonymousUsers { get; set; }

    /// <summary>SGAuthorizeCacheSeconds — authorization result cache TTL; 0 disables (default: 30).</summary>
    public int CacheSeconds { get; set; } = 30;

    /// <summary>Forms authentication cookie name (typically .ASPXAUTH).</summary>
    public string CookieName { get; set; } = ".ASPXAUTH";

    /// <summary>machineKey validationKey (hex) from the legacy ASP.NET site.</summary>
    public string ValidationKey { get; set; } = string.Empty;

    /// <summary>machineKey decryptionKey (hex) from the legacy ASP.NET site.</summary>
    public string DecryptionKey { get; set; } = string.Empty;

    /// <summary>machineKey validation algorithm: Sha1, Sha256, or Sha512 (default: Sha1).</summary>
    public string ShaVersion { get; set; } = "Sha1";

    /// <summary>machineKey compatibility: Framework20SP2 or Framework45 (default: Framework45).</summary>
    public string CompatibilityMode { get; set; } = "Framework45";

    public char TicketSeparatorChar =>
        string.IsNullOrEmpty(TicketTokenSeparator) ? '|' : TicketTokenSeparator[0];

    public bool IsFilterConfigured => !string.IsNullOrWhiteSpace(Filter);

    /// <summary>
    /// Authorization is active only when a URL filter is configured.
    /// With an empty/missing Filter the middleware is not registered and requests are not evaluated.
    /// </summary>
    public bool IsEnabled => IsFilterConfigured;

    public bool HasConnectionString => !string.IsNullOrWhiteSpace(Connection);

    public bool HasMachineKeys =>
        !string.IsNullOrWhiteSpace(ValidationKey) && !string.IsNullOrWhiteSpace(DecryptionKey);
}
