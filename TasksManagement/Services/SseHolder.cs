using System.Collections.Concurrent;
using System.Security.Claims;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TasksManagement.API.Contracts;
using TasksManagement.API.Models;
using TasksManagement.API.Models.ViewModels;

namespace TasksManagement.API.Services;

public class SseHolder : ISseHolder
{
    private readonly ILogger<SseHolder> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly JwtConfiguration _jwtConfiguration;

    private readonly ConcurrentDictionary<string, List<SseClient>> _clients = new();

    public SseHolder(ILogger<SseHolder> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<JwtConfiguration> jwtConfiguration,
        IHostApplicationLifetime applicationLifetime
        )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _jwtConfiguration = jwtConfiguration.Value;

        applicationLifetime.ApplicationStopping.Register(OnShutdown);
    }

    public record SseClient(HttpResponse Response, CancellationTokenSource Cancel);

    public async Task AddAsync(HttpContext context)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        var currentRequestService = scope.ServiceProvider.GetRequiredService<ICurrentRequest>();

        var token = currentRequestService.GetTokenFromQueryParams();
        var cancel = new CancellationTokenSource();
        var client = new SseClient(Response: context.Response, Cancel: cancel);

        var clientId = GetClientId(token);

        if (TryAddSseClient(clientId, client))
        {
            EchoAsync(clientId, client);
            context.RequestAborted.WaitHandle.WaitOne();
            RemoveClient(clientId, client);
            await Task.FromResult(true);
        }
    }

    private bool TryAddSseClient(string clientId, SseClient client)
    {
        if (_clients.ContainsKey(clientId))
        {
            _clients[clientId].Add(client);
            return true;
        }
        else
        {
            var clients = new List<SseClient>() { client };
            if (_clients.TryAdd(clientId, clients))
                return true;
            return false;
        }
    }

    private string GetClientId(StringValues? token)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var principal = authService.GetPrincipalFromToken(token);
        Guard.Against.Null(principal, nameof(principal));

        string? userId = principal!.FindFirstValue(ClaimTypes.NameIdentifier);
        Guard.Against.Null(userId, nameof(userId));
        return userId!;
    }

    public async Task SendMessageAsync(SseMessage message)
    {
        foreach (var c in _clients)
        {
            if (c.Key != message.ClientId)
            {
                continue;
            }
            var messageJson = message.ToJsonWithCamelCase();
            foreach (var sseClient in c.Value)
            {
                await sseClient.Response.WriteAsync($"data: {messageJson}\r\r", sseClient.Cancel.Token);
                await sseClient.Response.Body.FlushAsync(sseClient.Cancel.Token);
            }
        }
    }

    private async void EchoAsync(string clientId, SseClient client)
    {
        try
        {
            var clientIdJson = new SseClientId(clientId).ToJsonWithCamelCase();

            client.Response.Headers.Add("Content-Type", "text/event-stream");
            client.Response.Headers.Add("Cache-Control", "no-cache");
            client.Response.Headers.Add("Connection", "keep-alive");
            // Send ID to client-side after connecting
            await client.Response.WriteAsync($"data: {clientIdJson}\r\r", client.Cancel.Token);
            await client.Response.Body.FlushAsync(client.Cancel.Token);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Exception {ex.Message}");
        }
    }

    private void OnShutdown()
    {
        var tmpClients = new List<KeyValuePair<string, List<SseClient>>>();
        foreach (var c in _clients)
        {
            foreach (var connection in c.Value)
            {
                connection.Cancel.Cancel();
            }
            tmpClients.Add(c);
        }
        foreach (var c in tmpClients)
        {
            _clients.TryRemove(c);
        }
    }

    public void RemoveClient(string id, SseClient sseClient)
    {
        var client = _clients.FirstOrDefault(c => c.Key == id);
        if (string.IsNullOrEmpty(client.Key))
            return;
        var target = client.Value.FirstOrDefault(x => x == sseClient);
        target!.Cancel.Cancel();
        _clients[id].Remove(sseClient);
    }

    private string CreateId()
    {
        return Guid.NewGuid().ToString();
    }
}

public record SseMessage(string ClientId, NotificationVm Notification);

public record SseClientId(string ClientId);
