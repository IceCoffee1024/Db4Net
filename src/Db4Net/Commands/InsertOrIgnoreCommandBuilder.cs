using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds INSERT commands that ignore rows when a conflict target already exists.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertOrIgnoreCommandBuilder<T> : CommandBuilderBase
{
    private readonly T _entity;
    private readonly Db4NetOptions _options;
    private readonly string _table;
    private ColumnMetadata[] _conflictColumns = [];

    internal InsertOrIgnoreCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, T entity, Db4NetExecutionOptions? executionOptions = null)
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
    public InsertOrIgnoreCommandBuilder<T> OnConflict(params Expression<Func<T, object?>>[] memberSelectors)
    {
        _conflictColumns = ConflictInsertBuilderSupport<T>.GetColumns(memberSelectors);
        return this;
    }

    /// <inheritdoc />
    public override RenderedSqlCommand ToCommand()
    {
        return new ConflictInsertSqlRenderer(_options.Dialect).Render(
            _table,
            ConflictInsertBehavior.Ignore,
            ConflictInsertBuilderSupport<T>.GetInsertValues(_entity),
            ConflictInsertBuilderSupport<T>.ResolveConflictColumns(_conflictColumns),
            []);
    }
}
