using TasksManagement.API.Contracts;

namespace TasksManagement.API.Endpoints.Auth.Register;

public record RegisterCommand(
    string Names,
    string LastNames,
    string Email,
    string Password,
    Guid RoleId) : ICommand<RegisterResult>;
public record RegisterResult(
    string AccessToken,
    long ExpiresIn,
    DateTime ExpirationDate,
    string RefreshToken);

internal class RegisterCommandHandler(IAuthService authService)
    : ICommandHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var registerRequest = command.Adapt<Services.RegisterRequest>();
        var registerResult = await authService.Register(registerRequest);
        return registerResult.Adapt<RegisterResult>();
    }
}
