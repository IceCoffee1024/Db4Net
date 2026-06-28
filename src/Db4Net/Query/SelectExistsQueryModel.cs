namespace Db4Net.Query;

internal sealed class SelectExistsQueryModel
{
    public string? Table { get; set; }

    public List<FilterNode> Filters { get; } = [];
}
