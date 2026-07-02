using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class SelectSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public SelectSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(SelectQueryModel model)
    {
        var context = new SqlRenderContext(_dialect);
        var sql = RenderSql(model, context);

        return new RenderedSqlCommand(sql, context.DynamicParameters);
    }

    internal string RenderSql(SelectQueryModel model, SqlRenderContext context)
    {
        var table = model.Table ?? throw new InvalidOperationException("A table must be specified before rendering SQL.");
        if (string.IsNullOrWhiteSpace(table))
        {
            throw new InvalidOperationException("A table must be specified before rendering SQL.");
        }

        var sql = new StringBuilder();
        var filters = new FilterSqlRenderer(context);

        ValidatePaging(model);

        sql.Append("SELECT ");
        sql.Append(RenderColumns(model));
        sql.Append(" FROM ");
        sql.Append(context.Dialect.QuoteIdentifier(table));

        filters.Render(sql, model.Filters);
        RenderOrdering(sql, model);
        RenderPaging(sql, model, context.Parameters);

        return sql.ToString();
    }

    private void ValidatePaging(SelectQueryModel model)
    {
        if (model.Offset is not null && model.Limit is null)
        {
            throw new InvalidOperationException("Offset requires Limit before rendering SELECT SQL.");
        }

        if (_dialect.RequiresOrderByForPaging && model.Limit is not null && model.Orders.Count == 0)
        {
            throw new InvalidOperationException("SQL Server SELECT paging requires ORDER BY when Limit or Offset is used.");
        }
    }

    private string RenderColumns(SelectQueryModel model)
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

    private void RenderOrdering(StringBuilder sql, SelectQueryModel model)
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

    private void RenderPaging(StringBuilder sql, SelectQueryModel model, SqlParameterWriter parameters)
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
