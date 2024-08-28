namespace TasksManagement.API.Contracts;

public interface ICurrentRequest
{
    /// <summary>
    /// Id del usuario, puede no funcionar para todos los servicios, ejemplo singleton.
    /// </summary>
    string? Id { get; }
    string? GetTokenFromQueryParams();
}
