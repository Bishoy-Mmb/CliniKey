using CliniKey.SharedKernel.Primitives;
using MediatR;

namespace CliniKey.Application.Abstractions.Messaging;

public interface IBaseCommand;

public interface ICommand : IBaseCommand, IRequest<Result>
{
}

public interface ICommand<TResponse> : IBaseCommand, IRequest<Result<TResponse>>
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
