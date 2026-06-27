namespace Db4Net.Query;

internal sealed class SelectQueryModel
{
    public List<SelectColumn> Columns { get; } = [];

    public string? Table { get; set; }

    public List<FilterNode> Filters { get; } = [];

    public List<OrderClause> Orders { get; } = [];

    public int? Limit { get; set; }

    public int? Offset { get; set; }
}

internal sealed record SelectColumn(string Column, string? Alias = null);

internal abstract record FilterNode(FilterBooleanOperator BooleanOperator);

internal sealed record FilterClause(FilterBooleanOperator BooleanOperator, string Column, Op Operator, object? Value)
    : FilterNode(BooleanOperator);

internal sealed record FilterGroup(FilterBooleanOperator BooleanOperator, IReadOnlyList<FilterNode> Filters)
    : FilterNode(BooleanOperator);

internal enum FilterBooleanOperator
{
    And,
    Or
}

internal sealed record OrderClause(string Column, bool Descending);
