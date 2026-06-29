namespace Db4Net.Dialects;

internal interface ISqlDialect
{
    bool RenderOffsetBeforeLimit { get; }

    string QuoteIdentifier(string identifier);

    string RenderPaging(string limitParameterName, string? offsetParameterName);

    string RenderInsert(
        string table,
        IReadOnlyList<string> insertColumnNames,
        IReadOnlyList<string> parameterNames,
        string? returnKeyColumnName = null,
        string? returnKeyParameterName = null,
        bool returnKeyIsIdentity = false);
}
