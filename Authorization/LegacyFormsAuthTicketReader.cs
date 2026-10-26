using AspNetCore.LegacyAuthCookieCompat;
using Microsoft.Extensions.Options;

namespace ImageServer.Authorization;

public sealed class LegacyFormsAuthTicketReader : ILegacyFormsAuthTicketReader
{
    private readonly SgAuthorizeOptions _options;
    private readonly ILogger<LegacyFormsAuthTicketReader> _logger;
    private readonly LegacyFormsAuthenticationTicketEncryptor? _encryptor;

    public LegacyFormsAuthTicketReader(
        IOptions<SgAuthorizeOptions> options,
        ILogger<LegacyFormsAuthTicketReader> logger)
    {
        _options = options.Value;
        _logger = logger;
        _encryptor = CreateEncryptor(_options, logger);
    }

    public TicketIds? TryRead(HttpContext context)
    {
        if (_encryptor is null)
        {
            return null;
        }

        if (!context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieValue)
            || string.IsNullOrWhiteSpace(cookieValue))
        {
            return null;
        }

        try
        {
            var ticket = _encryptor.DecryptCookie(cookieValue);
            if (ticket is null || ticket.Expired)
            {
                return null;
            }

            return FormsAuthTicketParser.ParseUserData(
                ticket.UserData,
                _options.TicketSeparatorChar,
                _options.TicketToken,
                _options.TicketAdminToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt legacy Forms Authentication cookie '{CookieName}'.", _options.CookieName);
            return null;
        }
    }

    private static LegacyFormsAuthenticationTicketEncryptor? CreateEncryptor(
        SgAuthorizeOptions options,
        ILogger logger)
    {
        if (!options.HasMachineKeys)
        {
            logger.LogWarning(
                "SGAuthorize ValidationKey/DecryptionKey are not configured; Forms Auth cookies cannot be decrypted.");
            return null;
        }

        try
        {
            var decryptionKey = HexUtils.HexToBinary(options.DecryptionKey);
            var validationKey = HexUtils.HexToBinary(options.ValidationKey);
            var sha = ParseShaVersion(options.ShaVersion);
            var compatibility = ParseCompatibilityMode(options.CompatibilityMode);
            return new LegacyFormsAuthenticationTicketEncryptor(decryptionKey, validationKey, sha, compatibility);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to initialize legacy Forms Authentication decryptor.");
            return null;
        }
    }

    private static ShaVersion ParseShaVersion(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "SHA256" or "HMACSHA256" => ShaVersion.Sha256,
            "SHA512" or "HMACSHA512" => ShaVersion.Sha512,
            _ => ShaVersion.Sha1
        };

    private static CompatibilityMode ParseCompatibilityMode(string? value) =>
        value?.Trim().Equals("Framework20SP2", StringComparison.OrdinalIgnoreCase) == true
            ? CompatibilityMode.Framework20SP2
            : CompatibilityMode.Framework45;
}
