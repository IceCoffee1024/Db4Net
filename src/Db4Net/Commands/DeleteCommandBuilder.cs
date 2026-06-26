using System.Data;
using System.Linq.Expressions;
using Db4Net.Metadata;
using Db4Net.Query;
using Db4Net.Rendering;

namespace Db4Net.Commands;

/// <summary>
/// Builds DELETE statements for a mapped CLR model.
/// </summary>
/// <typeparam name="T">The CLR model type used for table and member mapping.</typeparam>
public sealed class DeleteCommandBuilder<T> : CommandBuilderBase
{
    private readonly DeleteCommandModel _model;
    private readonly Db4NetOptions _options;

    internal DeleteCommandBuilder(Db4NetOptions options, IDbConnection? connection, string table)
        : base(connection)
    {
        _options = options;
        _model = new DeleteCommandModel { Table = table };
    }

    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public DeleteCommandBuilder<T> Where(string propertyName, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("AND", MapPropertyName(propertyName), op, value));
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public DeleteCommandBuilder<T> Where(string propertyName, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("AND", MapPropertyName(propertyName), op, null));
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a typed member selector.
    /// </summary>
    public DeleteCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("AND", ModelMetadataProvider.GetColumnName(memberSelector), op, value));
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    public DeleteCommandBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("AND", ModelMetadataProvider.GetColumnName(memberSelector), op, null));
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public DeleteCommandBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("OR", MapPropertyName(propertyName), op, value));
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    public DeleteCommandBuilder<T> OrWhere(string propertyName, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("OR", MapPropertyName(propertyName), op, null));
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a typed member selector.
    /// </summary>
    public DeleteCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        _model.Filters.Add(new FilterClause("OR", ModelMetadataProvider.GetColumnName(memberSelector), op, value));
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    public DeleteCommandBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        EnsureValueFreeOperator(op);
        _model.Filters.Add(new FilterClause("OR", ModelMetadataProvider.GetColumnName(memberSelector), op, null));
        return this;
    }

    /// <summary>
    /// Allows rendering a DELETE statement without a WHERE clause.
    /// </summary>
    public DeleteCommandBuilder<T> AllowAllRows()
    {
        _model.AllowAllRows = true;
        return this;
    }

    /// <inheritdoc />
    public override SqlCommandDefinition ToCommand()
    {
        return new CommandSqlRenderer(_options.Dialect).Render(_model);
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }

    private static void EnsureValueFreeOperator(Op op)
    {
        if (op is not (Op.IsNull or Op.IsNotNull))
        {
            throw new ArgumentException($"Operator {op} requires a value.", nameof(op));
        }
    }

    private static void EnsureValidOperatorValue(Op op, object? value)
    {
        if (op is (Op.IsNull or Op.IsNotNull) && value is not null)
        {
            throw new ArgumentException($"Operator {op} does not accept a value.", nameof(value));
        }
    }
}
