using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds INSERT commands that update mapped columns when a conflict target already exists.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertOrUpdateCommandBuilder<T> : CommandBuilderBase
{
    private readonly T _entity;
    private readonly Db4NetOptions _options;
    private readonly string _table;
    private ColumnMetadata[] _conflictColumns = [];
    private ColumnMetadata[] _updateColumns = [];

    internal InsertOrUpdateCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, T entity, Db4NetExecutionOptions? executionOptions = null)
        : base(connection, executionOptions)
    {
        _options = options;
        _table = table;
        _entity = entity;
    }

    /// <summary>
    /// Overrides the conflict target using mapped CLR property selectors.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the conflict target columns.</param>
    /// <returns>The current command builder.</returns>
    public InsertOrUpdateCommandBuilder<T> OnConflict(params Expression<Func<T, object?>>[] memberSelectors)
    {
        _conflictColumns = ConflictInsertBuilderSupport<T>.GetColumns(memberSelectors);
        return this;
    }

    /// <summary>
    /// Overrides the columns updated when the conflict target already exists.
    /// </summary>
    /// <param name="memberSelectors">Simple member selectors for the update columns.</param>
    /// <returns>The current command builder.</returns>
    public InsertOrUpdateCommandBuilder<T> Update(params Expression<Func<T, object?>>[] memberSelectors)
    {
        _updateColumns = ConflictInsertBuilderSupport<T>.GetColumns(memberSelectors);
        return this;
    }

    /// <inheritdoc />
    public override RenderedSqlCommand ToCommand()
    {
        var conflictColumns = ConflictInsertBuilderSupport<T>.ResolveConflictColumns(_conflictColumns);
        return new ConflictInsertSqlRenderer(_options.Dialect).Render(
            _table,
            ConflictInsertBehavior.Update,
            ConflictInsertBuilderSupport<T>.GetInsertValues(_entity),
            conflictColumns,
            ConflictInsertBuilderSupport<T>.ResolveUpdateColumns(_updateColumns, conflictColumns));
    }
}
