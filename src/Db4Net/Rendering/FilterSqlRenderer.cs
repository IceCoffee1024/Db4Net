using System.Collections;
using System.Text;
using Db4Net.Query;

namespace Db4Net.Rendering;

internal sealed class FilterSqlRenderer
{
    private readonly SqlRenderContext _context;

    public FilterSqlRenderer(SqlRenderContext context)
    {
        _context = context;
    }

    public void Render(StringBuilder sql, IReadOnlyList<FilterNode> filters)
    {
        if (filters.Count == 0)
        {
            return;
        }

        sql.Append(" WHERE ");
        RenderNodes(sql, filters);
    }

    private void RenderNodes(StringBuilder sql, IReadOnlyList<FilterNode> filters)
    {
        for (var index = 0; index < filters.Count; index++)
        {
            var filter = filters[index];
            if (index > 0)
            {
                sql.Append($" {RenderBooleanOperator(filter.BooleanOperator)} ");
            }

            RenderNode(sql, filter);
        }
    }

    private void RenderNode(StringBuilder sql, FilterNode filter)
    {
        switch (filter)
        {
            case FilterClause clause:
                RenderClause(sql, clause);
                break;
            case FilterBetweenClause clause:
                RenderBetweenClause(sql, clause);
                break;
            case FilterSubqueryClause clause:
                RenderSubqueryClause(sql, clause);
                break;
            case FilterGroup group:
                RenderGroup(sql, group);
                break;
            default:
                throw new NotSupportedException($"Filter node {filter.GetType().Name} is not supported.");
        }
    }

    private void RenderClause(StringBuilder sql, FilterClause filter)
    {
        sql.Append(_context.Dialect.QuoteIdentifier(filter.Column));
        sql.Append(' ');
        sql.Append(RenderOperator(filter));
    }

    private void RenderBetweenClause(StringBuilder sql, FilterBetweenClause filter)
    {
        sql.Append(_context.Dialect.QuoteIdentifier(filter.Column));
        sql.Append(" BETWEEN @");
        sql.Append(_context.Parameters.Add(filter.Low));
        sql.Append(" AND @");
        sql.Append(_context.Parameters.Add(filter.High));
    }

    private void RenderSubqueryClause(StringBuilder sql, FilterSubqueryClause filter)
    {
        sql.Append(_context.Dialect.QuoteIdentifier(filter.Column));
        sql.Append(filter.Negated ? " NOT IN (" : " IN (");
        sql.Append(new SelectSqlRenderer(_context.Dialect).RenderSql(filter.Subquery, _context));
        sql.Append(')');
    }

    private void RenderGroup(StringBuilder sql, FilterGroup group)
    {
        if (group.Filters.Count == 0)
        {
            throw new InvalidOperationException("Filter group requires at least one filter.");
        }

        sql.Append('(');
        RenderNodes(sql, group.Filters);
        sql.Append(')');
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
            Op.Eq => $"= @{_context.Parameters.Add(filter.Value)}",
            Op.NotEq => $"<> @{_context.Parameters.Add(filter.Value)}",
            Op.Gt => $"> @{_context.Parameters.Add(filter.Value)}",
            Op.Gte => $">= @{_context.Parameters.Add(filter.Value)}",
            Op.Lt => $"< @{_context.Parameters.Add(filter.Value)}",
            Op.Lte => $"<= @{_context.Parameters.Add(filter.Value)}",
            Op.Like => $"LIKE @{_context.Parameters.Add(filter.Value)}",
            Op.NotLike => $"NOT LIKE @{_context.Parameters.Add(filter.Value)}",
            Op.In => $"IN ({RenderListParameters(filter.Value)})",
            Op.NotIn => $"NOT IN ({RenderListParameters(filter.Value)})",
            Op.IsNull => "IS NULL",
            Op.IsNotNull => "IS NOT NULL",
            _ => throw new NotSupportedException($"Operator {filter.Operator} is not supported.")
        };
    }

    private string RenderListParameters(object? value)
    {
        var parameterNames = new List<string>();
        foreach (var item in (IEnumerable)value!)
        {
            parameterNames.Add($"@{_context.Parameters.Add(item)}");
        }

        return string.Join(", ", parameterNames);
    }
}
