namespace Db4Net.Query;

internal sealed class QueryModel
{
    public List<string> Columns { get; } = [];

    public string? Table { get; set; }

    public List<FilterClause> Filters { get; } = [];

    public List<OrderClause> Orders { get; } = [];

    public int? Limit { get; set; }

    public int? Offset { get; set; }
}

internal sealed record FilterClause(string BooleanOperator, string Column, Op Operator, object? Value);

internal sealed record OrderClause(string Column, bool Descending);
