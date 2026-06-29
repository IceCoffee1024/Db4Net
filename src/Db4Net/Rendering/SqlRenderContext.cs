using Dapper;
using Db4Net.Dialects;

namespace Db4Net.Rendering;

internal sealed class SqlRenderContext
{
    public SqlRenderContext(ISqlDialect dialect)
    {
        Dialect = dialect;
    }

    public ISqlDialect Dialect { get; }

    public SqlParameterWriter Parameters { get; } = new();

    public DynamicParameters DynamicParameters => Parameters.Parameters;
}
