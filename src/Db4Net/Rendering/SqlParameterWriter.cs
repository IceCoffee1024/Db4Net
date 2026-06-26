using Dapper;

namespace Db4Net.Rendering;

internal sealed class SqlParameterWriter
{
    private int _parameterIndex;

    public DynamicParameters Parameters { get; } = new();

    public string Add(object? value)
    {
        var name = $"p{_parameterIndex++}";
        Parameters.Add(name, value);
        return name;
    }
}
