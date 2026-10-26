namespace ImageServer.Authorization;

public interface IAuthorizationCheckService
{
    Task<bool> IsAuthorizedAsync(string url, int userId, string filePath, string rawUrl, CancellationToken cancellationToken);
}
