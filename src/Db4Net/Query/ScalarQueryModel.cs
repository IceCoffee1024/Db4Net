namespace Db4Net.Query;

internal enum ScalarProjectionKind
{
    CountAll,
    Exists,
    CountDistinct,
    Max,
    Min
}

internal sealed class ScalarQueryModel
{
    public string? Table { get; set; }

    public ScalarProjectionKind ProjectionKind { get; set; }

    public string? Column { get; set; }

    public List<FilterNode> Filters { get; } = [];
}
