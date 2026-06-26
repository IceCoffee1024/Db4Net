using Db4Net.Query;

namespace Db4Net.Commands;

internal sealed class InsertCommandModel
{
    public required string Table { get; init; }

    public List<AssignmentClause> Values { get; } = [];
}

internal sealed class UpdateCommandModel
{
    public required string Table { get; init; }

    public List<AssignmentClause> Assignments { get; } = [];

    public List<FilterClause> Filters { get; } = [];

    public bool AllowAllRows { get; set; }
}

internal sealed class DeleteCommandModel
{
    public required string Table { get; init; }

    public List<FilterClause> Filters { get; } = [];

    public bool AllowAllRows { get; set; }
}

internal sealed record AssignmentClause(string Column, object? Value);
