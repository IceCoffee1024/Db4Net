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

    internal InsertCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table)
        : base(connection)
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

    /// <inheritdoc />
    public override RenderedSqlCommand ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }
}
