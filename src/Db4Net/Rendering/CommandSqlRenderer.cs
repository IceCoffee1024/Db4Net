using System.Collections;
using System.Text;
using Dapper;
using Db4Net.Commands;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class CommandSqlRenderer
{
    private readonly ISqlDialect _dialect;
    private readonly DynamicParameters _parameters = new();
    private int _parameterIndex;

    public CommandSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public SqlCommandDefinition Render(InsertCommandModel model)
    {
        if (model.Values.Count == 0)
        {
            throw new InvalidOperationException("INSERT requires at least one value.");
        }

        var columns = string.Join(", ", model.Values.Select(value => _dialect.QuoteIdentifier(value.Column)));
        var parameters = string.Join(", ", model.Values.Select(value => $"@{AddParameter(value.Value)}"));
        var sql = $"INSERT INTO {_dialect.QuoteIdentifier(model.Table)} ({columns}) VALUES ({parameters})";

        return new SqlCommandDefinition(sql, _parameters);
    }

    public SqlCommandDefinition Render(UpdateCommandModel model)
    {
        if (model.Assignments.Count == 0)
        {
            throw new InvalidOperationException("UPDATE requires at least one SET assignment.");
        }

        if (!model.AllowAllRows && model.Filters.Count == 0)
        {
            throw new InvalidOperationException("UPDATE requires a WHERE clause. Call AllowAllRows() to update every row explicitly.");
        }

        var sql = new StringBuilder();
        sql.Append("UPDATE ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));
        sql.Append(" SET ");
        sql.Append(string.Join(", ", model.Assignments.Select(assignment => $"{_dialect.QuoteIdentifier(assignment.Column)} = @{AddParameter(assignment.Value)}")));

        RenderFilters(sql, model.Filters);

        return new SqlCommandDefinition(sql.ToString(), _parameters);
    }

    public SqlCommandDefinition Render(DeleteCommandModel model)
    {
        if (!model.AllowAllRows && model.Filters.Count == 0)
        {
            throw new InvalidOperationException("DELETE requires a WHERE clause. Call AllowAllRows() to delete every row explicitly.");
        }

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));

        RenderFilters(sql, model.Filters);

        return new SqlCommandDefinition(sql.ToString(), _parameters);
    }

    private void RenderFilters(StringBuilder sql, IReadOnlyList<FilterClause> filters)
    {
        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
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

    private string AddParameter(object? value)
    {
        var name = $"p{_parameterIndex++}";
        _parameters.Add(name, value);
        return name;
    }
}
