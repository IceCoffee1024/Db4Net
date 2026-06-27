using System.Collections;
using System.Text;
using Db4Net.Dialects;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class FilterSqlRenderer
{
    private readonly ISqlDialect _dialect;
    private readonly SqlParameterWriter _parameters;

    public FilterSqlRenderer(ISqlDialect dialect, SqlParameterWriter parameters)
    {
        _dialect = dialect;
        _parameters = parameters;
    }

    public void Render(StringBuilder sql, IReadOnlyList<FilterClause> filters)
    {
        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
            sql.Append(index == 0 ? " WHERE " : $" {RenderBooleanOperator(filter.BooleanOperator)} ");
            sql.Append(_dialect.QuoteIdentifier(filter.Column));
            sql.Append(' ');
            sql.Append(RenderOperator(filter));
        }
    }

    private static string RenderBooleanOperator(FilterBooleanOperator booleanOperator)
    {
        return booleanOperator switch
        {
            FilterBooleanOperator.And => "AND",
            FilterBooleanOperator.Or => "OR",
            _ => throw new NotSupportedException($"Boolean operator {booleanOperator} is not supported.")
        };
    }

    private string RenderOperator(FilterClause filter)
    {
        return filter.Operator switch
        {
            Op.Eq when filter.Value is null => "IS NULL",
            Op.NotEq when filter.Value is null => "IS NOT NULL",
            Op.Eq => $"= @{_parameters.Add(filter.Value)}",
            Op.NotEq => $"<> @{_parameters.Add(filter.Value)}",
            Op.Gt => $"> @{_parameters.Add(filter.Value)}",
            Op.Gte => $">= @{_parameters.Add(filter.Value)}",
            Op.Lt => $"< @{_parameters.Add(filter.Value)}",
            Op.Lte => $"<= @{_parameters.Add(filter.Value)}",
            Op.Like => $"LIKE @{_parameters.Add(filter.Value)}",
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
            parameterNames.Add($"@{_parameters.Add(item)}");
        }

        if (parameterNames.Count == 0)
        {
            throw new ArgumentException("Op.In requires at least one value.", nameof(value));
        }

        return string.Join(", ", parameterNames);
    }
}
