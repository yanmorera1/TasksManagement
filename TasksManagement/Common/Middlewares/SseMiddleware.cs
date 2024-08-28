using TasksManagement.API.Contracts;

namespace TasksManagement.API.Common.Middlewares;
public class SseMiddleware
{
    private readonly RequestDelegate next;
    private readonly ISseHolder sse;
    public SseMiddleware(RequestDelegate next,
        ISseHolder sse)
    {
        this.next = next;
        this.sse = sse;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        await sse.AddAsync(context);
    }
}
