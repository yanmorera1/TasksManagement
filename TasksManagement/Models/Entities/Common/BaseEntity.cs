namespace TasksManagement.API.Models.Entities.Common;

public abstract class BaseEntity<T>
{
    public T Id { get; set; }
}
