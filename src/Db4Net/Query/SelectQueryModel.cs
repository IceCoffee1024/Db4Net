namespace Db4Net.Query;

internal sealed class SelectQueryModel
{
    public List<SelectColumn> Columns { get; } = [];

    public string? Table { get; set; }

    public List<FilterNode> Filters { get; } = [];

    public List<OrderClause> Orders { get; } = [];

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public SelectQueryModel Clone()
    {
        var clone = new SelectQueryModel
        {
            Table = Table,
            Limit = Limit,
            Offset = Offset
        };

        clone.Columns.AddRange(Columns);
        clone.Filters.AddRange(Filters.Select(CloneFilter));
        clone.Orders.AddRange(Orders);
        return clone;
    }

    private static FilterNode CloneFilter(FilterNode filter)
    {
        return filter switch
        {
            FilterClause clause => clause,
            FilterSubqueryClause clause => clause with { Subquery = clause.Subquery.Clone() },
            FilterGroup group => group with { Filters = group.Filters.Select(CloneFilter).ToArray() },
            _ => throw new NotSupportedException($"Filter node {filter.GetType().Name} is not supported.")
        };
    }
}

internal sealed record SelectColumn(string Column, string? Alias = null);

internal abstract record FilterNode(FilterBooleanOperator BooleanOperator);

internal sealed record FilterClause(FilterBooleanOperator BooleanOperator, string Column, Op Operator, object? Value)
    : FilterNode(BooleanOperator);

internal sealed record FilterSubqueryClause(FilterBooleanOperator BooleanOperator, string Column, bool Negated, SelectQueryModel Subquery)
    : FilterNode(BooleanOperator);

internal sealed record FilterGroup(FilterBooleanOperator BooleanOperator, IReadOnlyList<FilterNode> Filters)
    : FilterNode(BooleanOperator);

internal enum FilterBooleanOperator
{
    And,
    Or
}

internal sealed record OrderClause(string Column, bool Descending);
