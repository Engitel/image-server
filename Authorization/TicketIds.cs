namespace ImageServer.Authorization;

public readonly record struct TicketIds(int UserId, int UserAdminId)
{
    public bool IsAdmin => UserAdminId != 0;

    public bool IsAuthenticated => UserId != 0 || UserAdminId != 0;
}
