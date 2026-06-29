using Db4Net.Query;
using Db4Net.Metadata;

namespace Db4Net.Commands;

internal sealed class InsertCommandModel
{
    public required string Table { get; init; }

    public List<AssignmentClause> Values { get; } = [];

    public ColumnMetadata? ReturnKey { get; init; }
}

internal sealed class UpdateCommandModel
{
    public required string Table { get; init; }

    public List<AssignmentClause> Assignments { get; } = [];

    public List<FilterNode> Filters { get; } = [];

    public bool AllowAllRows { get; set; }
}

internal sealed class DeleteCommandModel
{
    public required string Table { get; init; }

    public List<FilterNode> Filters { get; } = [];

    public bool AllowAllRows { get; set; }
}

internal sealed record AssignmentClause(string Column, object? Value);
