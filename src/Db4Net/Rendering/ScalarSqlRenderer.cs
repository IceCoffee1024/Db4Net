using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class ScalarSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public ScalarSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(ScalarQueryModel model)
    {
        var table = RequireIdentifier(model.Table, "A table must be specified before rendering SQL.");
        var sql = new StringBuilder();
        var context = new SqlRenderContext(_dialect);
        var filters = new FilterSqlRenderer(context);

        if (model.ProjectionKind == ScalarProjectionKind.Exists)
        {
            sql.Append("SELECT CASE WHEN EXISTS (SELECT 1 FROM ");
            sql.Append(_dialect.QuoteIdentifier(table));
            filters.Render(sql, model.Filters);
            sql.Append(") THEN 1 ELSE 0 END");
            return new RenderedSqlCommand(sql.ToString(), context.DynamicParameters);
        }

        sql.Append("SELECT ");
        RenderProjection(sql, model);
        sql.Append(" FROM ");
        sql.Append(_dialect.QuoteIdentifier(table));
        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), context.DynamicParameters);
    }

    private void RenderProjection(StringBuilder sql, ScalarQueryModel model)
    {
        switch (model.ProjectionKind)
        {
            case ScalarProjectionKind.CountAll:
                sql.Append("COUNT(*)");
                break;
            case ScalarProjectionKind.CountDistinct:
                sql.Append("COUNT(DISTINCT ");
                sql.Append(RenderColumn(model.Column));
                sql.Append(')');
                break;
            case ScalarProjectionKind.Max:
                sql.Append("MAX(");
                sql.Append(RenderColumn(model.Column));
                sql.Append(')');
                break;
            case ScalarProjectionKind.Min:
                sql.Append("MIN(");
                sql.Append(RenderColumn(model.Column));
                sql.Append(')');
                break;
            case ScalarProjectionKind.Sum:
                sql.Append("SUM(");
                sql.Append(RenderColumn(model.Column));
                sql.Append(')');
                break;
            case ScalarProjectionKind.Average:
                sql.Append("AVG(");
                sql.Append(RenderColumn(model.Column));
                sql.Append(')');
                break;
            default:
                throw new NotSupportedException($"Scalar projection {model.ProjectionKind} is not supported.");
        }
    }

    private string RenderColumn(string? column)
    {
        return _dialect.QuoteIdentifier(RequireIdentifier(column, "A column must be specified before rendering scalar SQL."));
    }

    private static string RequireIdentifier(string? identifier, string message)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException(message);
        }

        return identifier!;
    }
}
