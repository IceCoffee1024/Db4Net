namespace Db4Net.Query;

internal sealed class SelectQueryModel
{
    public List<SelectColumn> Columns { get; } = [];

    public string? Table { get; set; }

    public List<FilterClause> Filters { get; } = [];

    public List<OrderClause> Orders { get; } = [];

    public int? Limit { get; set; }

    public int? Offset { get; set; }
}

internal sealed record SelectColumn(string Column, string? Alias = null);

internal sealed record FilterClause(FilterBooleanOperator BooleanOperator, string Column, Op Operator, object? Value);

internal enum FilterBooleanOperator
{
    And,
    Or
}

internal sealed record OrderClause(string Column, bool Descending);
