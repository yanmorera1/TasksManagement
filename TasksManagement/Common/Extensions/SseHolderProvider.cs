using TasksManagement.API.Common.Middlewares;

namespace TasksManagement.API.Common.Extensions;
public static class SseHolderMapper
{
    public static IApplicationBuilder MapSseHolder(this IApplicationBuilder app, PathString path)
    {
        return app.Map(path, (app) => app.UseMiddleware<SseMiddleware>());
    }
}
