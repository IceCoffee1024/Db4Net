using Db4Net.Commands;
using Db4Net.Metadata;

namespace Db4Net.Dialects;

internal interface ISqlDialect
{
    bool RenderOffsetBeforeLimit { get; }

    bool RequiresOrderByForPaging { get; }

    string QuoteIdentifier(string identifier);

    string RenderPaging(string limitParameterName, string? offsetParameterName);

    string RenderInsert(
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        string? returnKeyColumnName = null,
        string? returnKeyParameterName = null,
        bool returnKeyIsIdentity = false);

    string RenderConflictInsert(
        ConflictInsertBehavior behavior,
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<ColumnMetadata> conflictColumns,
        IReadOnlyList<ColumnMetadata> updateColumns);
}
