using DomainIUnitOfWork = MonkoraEdge.Core.Auth.Domain.Services.Interface.IUnitOfWork;
using DotNetIUnitOfWork = MonkoraEdge.Core.DotNet.Infrastructure.Interfaces.IUnitOfWork;

namespace MonkoraEdge.Core.Auth.Infrastructure.Services;

public sealed class DomainUnitOfWorkAdapter : DomainIUnitOfWork
{
    private readonly DotNetIUnitOfWork _inner;

    public DomainUnitOfWorkAdapter(DotNetIUnitOfWork inner)
    {
        _inner = inner;
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _inner.BeginTransactionAsync(cancellationToken);

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        => _inner.CommitTransactionAsync(cancellationToken);

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        => _inner.RollbackTransactionAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _inner.SaveChangesAsync(cancellationToken);
}