namespace Db4Net.Dialects;

internal abstract class AnsiSqlDialectBase : ISqlDialect
{
    public abstract bool RenderOffsetBeforeLimit { get; }

    public virtual bool RequiresOrderByForPaging => false;

    public string QuoteIdentifier(string identifier)
    {
        return SqlIdentifier.QuoteParts(identifier, part => $"\"{part}\"");
    }

    public abstract string RenderPaging(string limitParameterName, string? offsetParameterName);

    public abstract string RenderInsert(
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        string? returnKeyColumnName = null,
        string? returnKeyParameterName = null,
        bool returnKeyIsIdentity = false);

    public abstract string RenderConflictInsert(
        Commands.ConflictInsertBehavior behavior,
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<Metadata.ColumnMetadata> conflictColumns,
        IReadOnlyList<Metadata.ColumnMetadata> updateColumns);
}
