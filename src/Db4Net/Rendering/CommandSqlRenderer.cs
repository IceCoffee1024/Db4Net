using System.Text;
using Db4Net.Commands;
using Db4Net.Dialects;

namespace Db4Net.Rendering;

internal sealed class CommandSqlRenderer
{
    private readonly ISqlDialect _dialect;

    public CommandSqlRenderer(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    public RenderedSqlCommand Render(InsertCommandModel model)
    {
        if (model.Values.Count == 0)
        {
            throw new InvalidOperationException("INSERT requires at least one value.");
        }

        var parameterWriter = new SqlParameterWriter();
        var columns = string.Join(", ", model.Values.Select(value => _dialect.QuoteIdentifier(value.Column)));
        var parameters = string.Join(", ", model.Values.Select(value => $"@{parameterWriter.Add(value.Value)}"));
        var sql = $"INSERT INTO {_dialect.QuoteIdentifier(model.Table)} ({columns}) VALUES ({parameters})";

        return new RenderedSqlCommand(sql, parameterWriter.Parameters);
    }

    public RenderedSqlCommand Render(UpdateCommandModel model)
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
        var parameters = new SqlParameterWriter();
        var filters = new FilterSqlRenderer(_dialect, parameters);

        sql.Append("UPDATE ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));
        sql.Append(" SET ");
        sql.Append(string.Join(", ", model.Assignments.Select(assignment => $"{_dialect.QuoteIdentifier(assignment.Column)} = @{parameters.Add(assignment.Value)}")));

        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), parameters.Parameters);
    }

    public RenderedSqlCommand Render(DeleteCommandModel model)
    {
        if (!model.AllowAllRows && model.Filters.Count == 0)
        {
            throw new InvalidOperationException("DELETE requires a WHERE clause. Call AllowAllRows() to delete every row explicitly.");
        }

        var sql = new StringBuilder();
        var parameters = new SqlParameterWriter();
        var filters = new FilterSqlRenderer(_dialect, parameters);

        sql.Append("DELETE FROM ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));

        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), parameters.Parameters);
    }
}
