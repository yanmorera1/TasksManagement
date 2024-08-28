using TasksManagement.API.Services;

namespace TasksManagement.API.Contracts;

public interface ISseHolder
{
    /// <summary>
    /// Añade un cliente a la lista de clientes.
    /// </summary>
    Task AddAsync(HttpContext context);
    /// <summary>
    /// Envia un mensaje a los clientes conectados.
    /// </summary>
    Task SendMessageAsync(SseMessage message);
}
