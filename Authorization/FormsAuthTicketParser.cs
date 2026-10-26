namespace ImageServer.Authorization;

/// <summary>
/// Parses Spin&amp;Go FormsAuthenticationTicket.UserData tokens the same way as the .NET 4.7 module.
/// </summary>
public static class FormsAuthTicketParser
{
    public static TicketIds ParseUserData(string? userData, char separator, int userToken, int adminToken)
    {
        if (string.IsNullOrEmpty(userData) || userData.IndexOf(separator) < 0)
        {
            return default;
        }

        var parts = userData.Split(separator);
        var userId = ReadToken(parts, userToken);
        var adminId = ReadToken(parts, adminToken);
        return new TicketIds(userId, adminId);
    }

    private static int ReadToken(string[] parts, int oneBasedIndex)
    {
        if (oneBasedIndex <= 0 || oneBasedIndex > parts.Length)
        {
            return 0;
        }

        return int.TryParse(parts[oneBasedIndex - 1], out var value) ? value : 0;
    }
}
