namespace Db4Net.Query;

internal sealed class SelectCountQueryModel
{
    public string? Table { get; set; }

    public List<FilterNode> Filters { get; } = [];
}
