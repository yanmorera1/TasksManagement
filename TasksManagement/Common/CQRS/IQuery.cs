using MediatR;

namespace TasksManagement.API.Common.CQRS;

public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull
{

}
