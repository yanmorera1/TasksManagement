namespace TasksManagement.API.Endpoints.Test;

public record TestRequest(Guid To, string Message);

public class TestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/test", async ([FromBody] TestRequest request, ISender sender) =>
        {
            var command = request.Adapt<TestCommand>();
            var result = await sender.Send(command);
            return Results.Ok();
        })
        .WithName("Test")
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Test")
        .WithDescription("Test");
    }
}
