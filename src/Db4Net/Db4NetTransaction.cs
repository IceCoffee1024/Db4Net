using System.Data;

namespace Db4Net;

/// <summary>
/// Provides a Db4Net facade bound to a transaction owned by Db4Net.
/// </summary>
public sealed class Db4NetTransaction : IDisposable
{
    private readonly Db4NetDatabase _database;
    private readonly IDbTransaction _transaction;
    private bool _completed;
    private bool _disposed;

    internal Db4NetTransaction(Db4NetDatabase database, IDbTransaction transaction)
    {
        _transaction = transaction;
        _database = database.WithExecutionOptions(new Db4NetExecutionOptions
        {
            Transaction = transaction,
            ValidateBeforeExecute = ThrowIfNotActive
        });
    }

    /// <summary>
    /// Gets the Db4Net facade that applies this transaction to terminal methods.
    /// </summary>
    public Db4NetDatabase Database
    {
        get
        {
            ThrowIfNotActive();
            return _database;
        }
    }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public void Commit()
    {
        ThrowIfDisposed();
        ThrowIfCompleted();
        _transaction.Commit();
        _completed = true;
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public void Rollback()
    {
        ThrowIfDisposed();
        ThrowIfCompleted();
        _transaction.Rollback();
        _completed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (!_completed)
            {
                _transaction.Rollback();
            }
        }
        finally
        {
            _transaction.Dispose();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(Db4NetTransaction));
        }
    }

    private void ThrowIfNotActive()
    {
        ThrowIfDisposed();
        ThrowIfCompleted();
    }

    private void ThrowIfCompleted()
    {
        if (_completed)
        {
            throw new InvalidOperationException("The transaction has already been completed.");
        }
    }
}
