namespace ImageServer.Authorization;

public interface ILegacyFormsAuthTicketReader
{
    /// <summary>
    /// Decrypts the legacy Forms Auth cookie and returns ticket ids, or null if missing/invalid.
    /// </summary>
    TicketIds? TryRead(HttpContext context);
}
