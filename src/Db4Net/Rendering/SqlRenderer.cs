using System.Collections;
using System.Text;
using Dapper;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class SqlRenderer
{
    private readonly ISqlDialect _dialect;
    private readonly DynamicParameters _parameters = new();
    private int _parameterIndex;

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
        sql.Append("SELECT ");
        sql.Append(RenderColumns(model));
        sql.Append(" FROM ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));

        RenderFilters(sql, model);
        RenderOrdering(sql, model);
        RenderPaging(sql, model);

        return new SqlCommandDefinition(sql.ToString(), _parameters);
    }

    private string RenderColumns(QueryModel model)
    {
        if (model.Columns.Count == 0)
        {
            return "*";
        }

        return string.Join(", ", model.Columns.Select(_dialect.QuoteIdentifier));
    }

    private void RenderFilters(StringBuilder sql, QueryModel model)
    {
        for (var index = 0; index < model.Filters.Count; index++)
        {
            var filter = model.Filters[index];
            sql.Append(index == 0 ? " WHERE " : $" {filter.BooleanOperator} ");
            sql.Append(_dialect.QuoteIdentifier(filter.Column));
            sql.Append(' ');
            sql.Append(RenderOperator(filter));
        }
    }

    private string RenderOperator(FilterClause filter)
    {
        return filter.Operator switch
        {
            Op.Eq when filter.Value is null => "IS NULL",
            Op.NotEq when filter.Value is null => "IS NOT NULL",
            Op.Eq => $"= @{AddParameter(filter.Value)}",
            Op.NotEq => $"<> @{AddParameter(filter.Value)}",
            Op.Gt => $"> @{AddParameter(filter.Value)}",
            Op.Gte => $">= @{AddParameter(filter.Value)}",
            Op.Lt => $"< @{AddParameter(filter.Value)}",
            Op.Lte => $"<= @{AddParameter(filter.Value)}",
            Op.Like => $"LIKE @{AddParameter(filter.Value)}",
            Op.In => $"IN ({RenderInParameters(filter.Value)})",
            Op.IsNull => "IS NULL",
            Op.IsNotNull => "IS NOT NULL",
            _ => throw new NotSupportedException($"Operator {filter.Operator} is not supported.")
        };
    }

    private string RenderInParameters(object? value)
    {
        if (value is string || value is not IEnumerable values)
        {
            throw new ArgumentException("Op.In requires a non-string enumerable value.", nameof(value));
        }

        var parameterNames = new List<string>();
        foreach (var item in values)
        {
            parameterNames.Add($"@{AddParameter(item)}");
        }

        if (parameterNames.Count == 0)
        {
            throw new ArgumentException("Op.In requires at least one value.", nameof(value));
        }

        return string.Join(", ", parameterNames);
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

    private void RenderPaging(StringBuilder sql, QueryModel model)
    {
        if (model.Limit is null)
        {
            return;
        }

        string limitParameter;
        string? offsetParameter = null;

        if (model.Offset is null)
        {
            limitParameter = AddParameter(model.Limit.Value);
        }
        else if (_dialect.RenderOffsetBeforeLimit)
        {
            offsetParameter = AddParameter(model.Offset.Value);
            limitParameter = AddParameter(model.Limit.Value);
        }
        else
        {
            limitParameter = AddParameter(model.Limit.Value);
            offsetParameter = AddParameter(model.Offset.Value);
        }

        sql.Append(' ');
        sql.Append(_dialect.RenderPaging(limitParameter, offsetParameter));
    }

    private string AddParameter(object? value)
    {
        var name = $"p{_parameterIndex++}";
        _parameters.Add(name, value);
        return name;
    }
}
