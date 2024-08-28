namespace TasksManagement.API.Endpoints.Auth.Register;

public record RegisterRequest(
    string Names,
    string LastNames,
    string Email,
    string Password,
    Guid RoleId);
public record RegisterResponse(
    string AccessToken,
    long ExpiresIn,
    DateTime ExpirationDate,
    string RefreshToken);

public class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async ([FromBody] RegisterRequest request, ISender sender) =>
        {
            var command = request.Adapt<RegisterCommand>();
            var result = await sender.Send(command);
            var response = result.Adapt<RegisterResponse>();
            return Results.Ok(response);
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Register")
        .WithDescription("Register");
    }
}
