namespace Db4Net.Dialects;

internal interface ISqlDialect
{
    bool RenderOffsetBeforeLimit { get; }

    string QuoteIdentifier(string identifier);

    string RenderPaging(string limitParameterName, string? offsetParameterName);
}
