using AspNetCore.LegacyAuthCookieCompat;
using ImageServer.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ImageServer.Tests;

public class LegacyFormsAuthTicketReaderTests
{
    // Deterministic test keys (not production secrets)
    private const string ValidationKey =
        "30101052676849B0B494466B7A99656346328E8964748448E422D7344467A45777D972414947271744423422851D6742C9A09A65212C276C7F839157501291C6";
    private const string DecryptionKey =
        "AC7387D7E54B156377D81930CF237888854B5B5B515CF2D6356541255E696144";

    [Fact]
    public void TryRead_ReturnsNull_WhenCookieMissing()
    {
        var reader = CreateReader();
        var context = new DefaultHttpContext();

        Assert.Null(reader.TryRead(context));
    }

    [Fact]
    public void TryRead_ParsesEncryptedCookie()
    {
        var userData = "99|a|b|c|d|e|f|g|1234";
        var cookieValue = EncryptTicket(userData);

        var reader = CreateReader();
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $".ASPXAUTH={cookieValue}";

        var ids = reader.TryRead(context);

        Assert.NotNull(ids);
        Assert.Equal(1234, ids.Value.UserId);
        Assert.Equal(99, ids.Value.UserAdminId);
    }

    private static LegacyFormsAuthTicketReader CreateReader()
    {
        var options = new SgAuthorizeOptions
        {
            CookieName = ".ASPXAUTH",
            ValidationKey = ValidationKey,
            DecryptionKey = DecryptionKey,
            ShaVersion = "Sha1",
            CompatibilityMode = "Framework45",
            TicketToken = 9,
            TicketAdminToken = 1,
            TicketTokenSeparator = "|"
        };

        return new LegacyFormsAuthTicketReader(
            Options.Create(options),
            NullLogger<LegacyFormsAuthTicketReader>.Instance);
    }

    private static string EncryptTicket(string userData)
    {
        var issue = DateTime.Now;
        var ticket = new FormsAuthenticationTicket(
            2,
            "testuser",
            issue,
            issue.AddHours(1),
            false,
            userData,
            "/");

        var encryptor = new LegacyFormsAuthenticationTicketEncryptor(
            HexUtils.HexToBinary(DecryptionKey),
            HexUtils.HexToBinary(ValidationKey),
            ShaVersion.Sha1,
            CompatibilityMode.Framework45);

        return encryptor.Encrypt(ticket);
    }
}
