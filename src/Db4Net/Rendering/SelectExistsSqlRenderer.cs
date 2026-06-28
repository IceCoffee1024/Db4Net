using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class SelectExistsSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public SelectExistsSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(SelectExistsQueryModel model)
    {
        var table = model.Table ?? throw new InvalidOperationException("A table must be specified before rendering SQL.");
        if (string.IsNullOrWhiteSpace(table))
        {
            throw new InvalidOperationException("A table must be specified before rendering SQL.");
        }

        var sql = new StringBuilder();
        var parameters = new SqlParameterWriter();
        var filters = new FilterSqlRenderer(_dialect, parameters);

        sql.Append("SELECT CASE WHEN EXISTS (SELECT 1 FROM ");
        sql.Append(_dialect.QuoteIdentifier(table));
        filters.Render(sql, model.Filters);
        sql.Append(") THEN 1 ELSE 0 END");

        return new RenderedSqlCommand(sql.ToString(), parameters.Parameters);
    }
}
