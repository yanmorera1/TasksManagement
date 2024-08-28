using System.Security.Claims;
using TasksManagement.API.Contracts;

namespace TasksManagement.API.Services;

public class CurrentRequest(IHttpContextAccessor httpContextAccessor) : ICurrentRequest
{
    public string? Id => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? GetTokenFromQueryParams()
    {
        return httpContextAccessor.HttpContext?.Request?.Query.FirstOrDefault(x => x.Key == "token").Value;
    }
}
