using System.Security.Claims;
using TasksManagement.API.Services;

namespace TasksManagement.API.Contracts;

public interface IAuthService
{
    Task<AuthResponse> Login(AuthRequest request);
    Task<AuthResponse> Register(RegisterRequest request);
    Task<AuthResponse> Refresh(RefreshRequest request);
    ClaimsPrincipal? GetPrincipalFromToken(string? token, bool validateLifeTime = true);
}
