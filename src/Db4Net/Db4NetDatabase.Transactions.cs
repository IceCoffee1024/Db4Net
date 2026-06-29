using System.Data;

namespace Db4Net;

public sealed partial class Db4NetDatabase
{
    /// <summary>
    /// Begins a transaction on the bound connection and returns a Db4Net transaction facade.
    /// </summary>
    /// <returns>A Db4Net transaction facade. Disposing it without committing rolls back the transaction.</returns>
    public Db4NetTransaction BeginTransaction()
    {
        return new Db4NetTransaction(this, RequireConnection().BeginTransaction());
    }

    /// <summary>
    /// Begins a transaction on the bound connection with the specified isolation level and returns a Db4Net transaction facade.
    /// </summary>
    /// <param name="isolationLevel">The transaction isolation level.</param>
    /// <returns>A Db4Net transaction facade. Disposing it without committing rolls back the transaction.</returns>
    public Db4NetTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        return new Db4NetTransaction(this, RequireConnection().BeginTransaction(isolationLevel));
    }

    /// <summary>
    /// Executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <param name="work">The work to execute inside the transaction.</param>
    public void ExecuteInTransaction(Action<Db4NetTransaction> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        work(transaction);
        transaction.Commit();
    }

    /// <summary>
    /// Executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the delegate.</typeparam>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>The result returned by the delegate.</returns>
    public TResult ExecuteInTransaction<TResult>(Func<Db4NetTransaction, TResult> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        var result = work(transaction);
        transaction.Commit();
        return result;
    }

    /// <summary>
    /// Asynchronously executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>A task that completes when the work has finished and the transaction has committed.</returns>
    public async Task ExecuteInTransactionAsync(Func<Db4NetTransaction, Task> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        await work(transaction).ConfigureAwait(false);
        transaction.Commit();
    }

    /// <summary>
    /// Asynchronously executes work inside a Db4Net-owned transaction and commits when the delegate succeeds.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the delegate.</typeparam>
    /// <param name="work">The work to execute inside the transaction.</param>
    /// <returns>A task containing the result returned by the delegate.</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Db4NetTransaction, Task<TResult>> work)
    {
        ThrowHelper.ThrowIfNull(work);

        using var transaction = BeginTransaction();
        var result = await work(transaction).ConfigureAwait(false);
        transaction.Commit();
        return result;
    }
}
