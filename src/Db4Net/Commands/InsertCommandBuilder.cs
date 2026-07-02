using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds INSERT statements for a mapped CLR model.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class InsertCommandBuilder<T> : CommandBuilderBase
{
    private readonly InsertCommandModel _model;
    private readonly Db4NetOptions _options;

    internal InsertCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table, Db4NetExecutionOptions? executionOptions = null)
        : base(connection, executionOptions)
    {
        _options = options;
        _model = new InsertCommandModel { Table = table };
    }

    /// <summary>
    /// Adds an INSERT value using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The mapped CLR property name to insert.</param>
    /// <param name="value">The value to pass as a Dapper parameter.</param>
    /// <returns>The current command builder.</returns>
    public InsertCommandBuilder<T> Value(string propertyName, object? value)
    {
        _model.Values.Add(new AssignmentClause(MapPropertyName(propertyName), value));
        return this;
    }

    /// <summary>
    /// Adds an INSERT value using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="value">The value to pass as a Dapper parameter.</param>
    /// <returns>The current command builder.</returns>
    public InsertCommandBuilder<T> Value<TValue>(Expression<Func<T, TValue>> memberSelector, object? value)
    {
        _model.Values.Add(new AssignmentClause(ModelMetadataProvider.GetColumnName(memberSelector), value));
        return this;
    }

    /// <summary>
    /// Adds INSERT values for all mapped properties from an entity instance.
    /// </summary>
    /// <param name="entity">The entity instance to read values from.</param>
    /// <returns>The current command builder.</returns>
    public InsertCommandBuilder<T> Values(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);

        foreach (var column in ModelMetadata<T>.InsertColumns)
        {
            _model.Values.Add(new AssignmentClause(column.ColumnName, column.GetValue(entity)));
        }

        return this;
    }

    /// <summary>
    /// Selects the mapped key column returned by this single-row INSERT command.
    /// </summary>
    /// <param name="keySelector">A simple key member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <returns>A scalar insert command builder for executing or inspecting the returned key command.</returns>
    public InsertReturnKeyCommandBuilder<T> ReturnKey(Expression<Func<T, object?>> keySelector)
    {
        return new InsertReturnKeyCommandBuilder<T>(
            _options,
            Connection,
            CreateReturnKeyModel(ResolveReturnKey(keySelector)),
            ExecutionOptions);
    }

    /// <summary>
    /// Executes this single-row INSERT command and returns the model's only mapped key.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The key value returned by the database.</returns>
    public TResult ExecuteReturnKey<TResult>(Db4NetExecutionOptions? options = null)
    {
        return ExecuteScalar<TResult>(ToReturnKeyCommand(ResolveDefaultReturnKey()), options);
    }

    /// <summary>
    /// Executes this single-row INSERT command and returns the selected mapped key.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="keySelector">A simple key member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <returns>The key value returned by the database.</returns>
    public TResult ExecuteReturnKey<TResult>(Expression<Func<T, object?>> keySelector, Db4NetExecutionOptions? options = null)
    {
        return ExecuteScalar<TResult>(ToReturnKeyCommand(ResolveReturnKey(keySelector)), options);
    }

    /// <summary>
    /// Asynchronously executes this single-row INSERT command and returns the model's only mapped key.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The key value returned by the database.</returns>
    public Task<TResult> ExecuteReturnKeyAsync<TResult>(
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteScalarAsync<TResult>(ToReturnKeyCommand(ResolveDefaultReturnKey()), options, cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes this single-row INSERT command and returns the selected mapped key.
    /// </summary>
    /// <typeparam name="TResult">The scalar key result type returned by the database.</typeparam>
    /// <param name="keySelector">A simple key member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="options">Optional Dapper execution settings such as transaction, timeout, or command type.</param>
    /// <param name="cancellationToken">The cancellation token passed to Dapper.</param>
    /// <returns>The key value returned by the database.</returns>
    public Task<TResult> ExecuteReturnKeyAsync<TResult>(
        Expression<Func<T, object?>> keySelector,
        Db4NetExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteScalarAsync<TResult>(ToReturnKeyCommand(ResolveReturnKey(keySelector)), options, cancellationToken);
    }

    /// <inheritdoc />
    public override RenderedSqlCommand ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    internal static ColumnMetadata ResolveDefaultReturnKey()
    {
        if (ModelMetadata<T>.KeyColumns.Count == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' does not have a key. Add [Key] or an Id/{typeof(T).Name}Id property.");
        }

        if (ModelMetadata<T>.KeyColumns.Count > 1)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' has multiple keys. Specify the key selector to return.");
        }

        return ModelMetadata<T>.KeyColumns[0];
    }

    internal static ColumnMetadata ResolveReturnKey(Expression<Func<T, object?>> keySelector)
    {
        var column = ModelMetadataProvider.GetColumnMetadata(keySelector);
        if (!column.IsKey)
        {
            throw new ArgumentException($"Member '{typeof(T).Name}.{column.PropertyName}' is not a key. ReturnKey requires a mapped key column.", nameof(keySelector));
        }

        return column;
    }

    private RenderedSqlCommand ToReturnKeyCommand(ColumnMetadata returnKey)
    {
        return new CommandSqlRenderer(_options.Dialect).Render(CreateReturnKeyModel(returnKey));
    }

    private InsertCommandModel CreateReturnKeyModel(ColumnMetadata returnKey)
    {
        var model = new InsertCommandModel
        {
            Table = _model.Table,
            ReturnKey = returnKey,
        };
        model.Values.AddRange(_model.Values);
        return model;
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }
}
