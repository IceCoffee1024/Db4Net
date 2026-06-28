using System.Linq.Expressions;
using Db4Net.Metadata;

namespace Db4Net.Query;

internal sealed class ScalarQueryBuilderState<T>
{
    private readonly FilterClauseBuilder _filters;

    public ScalarQueryBuilderState(string table, ScalarProjectionKind projectionKind, string? column = null)
    {
        Model = new ScalarQueryModel
        {
            Table = table,
            ProjectionKind = projectionKind,
            Column = column
        };
        _filters = new FilterClauseBuilder(Model.Filters);
    }

    public ScalarQueryModel Model { get; }

    public void AddFilter(FilterBooleanOperator booleanOperator, string propertyName, Op op, object? value)
    {
        _filters.Add(booleanOperator, () => MapPropertyName(propertyName), op, value);
    }

    public void AddFilter<TValue>(FilterBooleanOperator booleanOperator, Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        _filters.Add(booleanOperator, () => ModelMetadataProvider.GetColumnName(memberSelector), op, value);
    }

    public void AddValueFreeFilter(FilterBooleanOperator booleanOperator, string propertyName, Op op)
    {
        _filters.AddValueFree(booleanOperator, () => MapPropertyName(propertyName), op);
    }

    public void AddValueFreeFilter<TValue>(FilterBooleanOperator booleanOperator, Expression<Func<T, TValue>> memberSelector, Op op)
    {
        _filters.AddValueFree(booleanOperator, () => ModelMetadataProvider.GetColumnName(memberSelector), op);
    }

    public void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder<T>> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        _filters.AddGroup(booleanOperator, group.Filters);
    }

    private static string MapPropertyName(string propertyName)
    {
        return ModelMetadata<T>.GetColumn(propertyName).ColumnName;
    }
}
