namespace Db4Net.Query;

internal sealed class FilterClauseBuilder
{
    public FilterClauseBuilder()
        : this([])
    {
    }

    public FilterClauseBuilder(List<FilterNode> filters)
    {
        ThrowHelper.ThrowIfNull(filters);
        Filters = filters;
    }

    public List<FilterNode> Filters { get; }

    public void Add(FilterBooleanOperator booleanOperator, string column, Op op, object? value)
    {
        EnsureValidOperatorValue(op, value);
        Filters.Add(new FilterClause(booleanOperator, column, op, value));
    }

    public void Add(FilterBooleanOperator booleanOperator, Func<string> columnFactory, Op op, object? value)
    {
        ThrowHelper.ThrowIfNull(columnFactory);
        EnsureValidOperatorValue(op, value);
        Filters.Add(new FilterClause(booleanOperator, columnFactory(), op, value));
    }

    public void AddValueFree(FilterBooleanOperator booleanOperator, string column, Op op)
    {
        EnsureValueFreeOperator(op);
        Filters.Add(new FilterClause(booleanOperator, column, op, null));
    }

    public void AddValueFree(FilterBooleanOperator booleanOperator, Func<string> columnFactory, Op op)
    {
        ThrowHelper.ThrowIfNull(columnFactory);
        EnsureValueFreeOperator(op);
        Filters.Add(new FilterClause(booleanOperator, columnFactory(), op, null));
    }

    public void AddSubquery(FilterBooleanOperator booleanOperator, string column, bool negated, SelectQueryModel subquery)
    {
        ThrowHelper.ThrowIfNull(subquery);
        EnsureSingleColumnSubquery(subquery);
        Filters.Add(new FilterSubqueryClause(booleanOperator, column, negated, subquery));
    }

    public void AddSubquery(FilterBooleanOperator booleanOperator, Func<string> columnFactory, bool negated, SelectQueryModel subquery)
    {
        ThrowHelper.ThrowIfNull(columnFactory);
        AddSubquery(booleanOperator, columnFactory(), negated, subquery);
    }

    public void AddGroup(FilterBooleanOperator booleanOperator, IReadOnlyList<FilterNode> filters)
    {
        ThrowHelper.ThrowIfNull(filters);
        if (filters.Count == 0)
        {
            throw new ArgumentException("Filter group requires at least one filter.", nameof(filters));
        }

        Filters.Add(new FilterGroup(booleanOperator, filters.ToArray()));
    }

    public static void EnsureValueFreeOperator(Op op)
    {
        if (op is not (Op.IsNull or Op.IsNotNull))
        {
            throw new ArgumentException($"Operator {op} requires a value.", nameof(op));
        }
    }

    public static void EnsureValidOperatorValue(Op op, object? value)
    {
        if (op is (Op.IsNull or Op.IsNotNull) && value is not null)
        {
            throw new ArgumentException($"Operator {op} does not accept a value.", nameof(value));
        }
    }

    private static void EnsureSingleColumnSubquery(SelectQueryModel subquery)
    {
        if (subquery.Columns.Count != 1)
        {
            throw new InvalidOperationException("Subquery filter requires exactly one selected column.");
        }
    }
}
