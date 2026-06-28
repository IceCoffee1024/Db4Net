using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class SelectCountSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public SelectCountSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(SelectCountQueryModel model)
    {
        var table = model.Table ?? throw new InvalidOperationException("A table must be specified before rendering SQL.");
        if (string.IsNullOrWhiteSpace(table))
        {
            throw new InvalidOperationException("A table must be specified before rendering SQL.");
        }

        var sql = new StringBuilder();
        var parameters = new SqlParameterWriter();
        var filters = new FilterSqlRenderer(_dialect, parameters);

        sql.Append("SELECT COUNT(*) FROM ");
        sql.Append(_dialect.QuoteIdentifier(table));
        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), parameters.Parameters);
    }
}
