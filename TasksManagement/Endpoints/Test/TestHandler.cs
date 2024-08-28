using TasksManagement.API.Contracts;
using TasksManagement.API.Models.ViewModels;

namespace TasksManagement.API.Endpoints.Test;
public record TestCommand(Guid To, string Message) : ICommand<TestResult>;
public record TestResult(bool IsSuccess);
internal class TestCommandHandler(ISseHolder sseHolder)
    : ICommandHandler<TestCommand, TestResult>
{
    public async Task<TestResult> Handle(TestCommand command, CancellationToken cancellationToken)
    {
        var message = new Services.SseMessage(command.To.ToString(), new NotificationVm(command.Message));
        await sseHolder.SendMessageAsync(message);
        return new TestResult(true);
    }
}
