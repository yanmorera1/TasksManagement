namespace TasksManagement.API.Models.Entities.Common;

public abstract class BaseAuditableEntity : BaseEntity<Guid>
{
    public DateTimeOffset Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public string? LastModifiedBy { get; set; }
}