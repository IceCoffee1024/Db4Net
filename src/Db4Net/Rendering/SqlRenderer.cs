using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class SqlRenderer
{
    private readonly ISqlDialect _dialect;

    public SqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public SqlCommandDefinition Render(QueryModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Table))
        {
            throw new InvalidOperationException("A table must be specified before rendering SQL.");
        }

        var sql = new StringBuilder();
        var parameters = new SqlParameterWriter();
        var filters = new FilterSqlRenderer(_dialect, parameters);

        sql.Append("SELECT ");
        sql.Append(RenderColumns(model));
        sql.Append(" FROM ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));

        filters.Render(sql, model.Filters);
        RenderOrdering(sql, model);
        RenderPaging(sql, model, parameters);

        return new SqlCommandDefinition(sql.ToString(), parameters.Parameters);
    }

    private string RenderColumns(QueryModel model)
    {
        if (model.Columns.Count == 0)
        {
            return "*";
        }

        return string.Join(", ", model.Columns.Select(RenderColumn));
    }

    private string RenderColumn(SelectColumn column)
    {
        var renderedColumn = _dialect.QuoteIdentifier(column.Column);
        if (column.Alias is null || column.Alias == column.Column)
        {
            return renderedColumn;
        }

        return $"{renderedColumn} AS {_dialect.QuoteIdentifier(column.Alias)}";
    }

    private void RenderOrdering(StringBuilder sql, QueryModel model)
    {
        if (model.Orders.Count == 0)
        {
            return;
        }

        var orders = model.Orders.Select(order =>
        {
            var direction = order.Descending ? " DESC" : "";
            return $"{_dialect.QuoteIdentifier(order.Column)}{direction}";
        });

        sql.Append(" ORDER BY ");
        sql.Append(string.Join(", ", orders));
    }

    private void RenderPaging(StringBuilder sql, QueryModel model, SqlParameterWriter parameters)
    {
        if (model.Limit is null)
        {
            return;
        }

        string limitParameter;
        string? offsetParameter = null;

        if (model.Offset is null && _dialect.RenderOffsetBeforeLimit)
        {
            offsetParameter = parameters.Add(0);
            limitParameter = parameters.Add(model.Limit.Value);
        }
        else if (model.Offset is null)
        {
            limitParameter = parameters.Add(model.Limit.Value);
        }
        else if (_dialect.RenderOffsetBeforeLimit)
        {
            offsetParameter = parameters.Add(model.Offset.Value);
            limitParameter = parameters.Add(model.Limit.Value);
        }
        else
        {
            limitParameter = parameters.Add(model.Limit.Value);
            offsetParameter = parameters.Add(model.Offset.Value);
        }

        sql.Append(' ');
        sql.Append(_dialect.RenderPaging(limitParameter, offsetParameter));
    }
}
