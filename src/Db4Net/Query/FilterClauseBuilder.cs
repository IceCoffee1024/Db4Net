namespace Db4Net.Query;

internal sealed class FilterClauseBuilder
{
    public FilterClauseBuilder()
        : this([])
    {
    }

    public FilterClauseBuilder(List<FilterNode> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
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
        ArgumentNullException.ThrowIfNull(columnFactory);
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
        ArgumentNullException.ThrowIfNull(columnFactory);
        EnsureValueFreeOperator(op);
        Filters.Add(new FilterClause(booleanOperator, columnFactory(), op, null));
    }

    public void AddGroup(FilterBooleanOperator booleanOperator, IReadOnlyList<FilterNode> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
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
}
