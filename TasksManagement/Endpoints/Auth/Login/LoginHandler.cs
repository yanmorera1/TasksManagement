using TasksManagement.API.Contracts;
using TasksManagement.API.Services;

namespace TasksManagement.API.Endpoints.Auth.Login;

public record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
public record LoginResult(string AccessToken,
    long ExpiresIn,
    DateTime ExpirationDate,
    string RefreshToken);

internal class LoginCommandHandler(IAuthService authService) : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var authRequest = new AuthRequest(command.Email, command.Password);
        var authResponse = await authService.Login(authRequest);
        return authResponse.Adapt<LoginResult>();
    }
}
