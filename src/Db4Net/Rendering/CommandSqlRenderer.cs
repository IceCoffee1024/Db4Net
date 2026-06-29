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
        var insertColumnNames = model.Values.Select(value => value.Column).ToArray();
        var parameterNames = model.Values.Select(value => parameterWriter.Add(value.Value)).ToArray();
        var returnKeyParameterName = model.ReturnKey is null
            ? null
            : model.Values
                .Select((value, index) => new { value.Column, ParameterName = parameterNames[index] })
                .FirstOrDefault(value => value.Column == model.ReturnKey.ColumnName)
                ?.ParameterName;
        var sql = _dialect.RenderInsert(
            model.Table,
            insertColumnNames,
            parameterNames,
            model.ReturnKey?.ColumnName,
            returnKeyParameterName,
            model.ReturnKey?.IsDatabaseGeneratedIdentity ?? false);

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
        var context = new SqlRenderContext(_dialect);
        var parameters = context.Parameters;
        var filters = new FilterSqlRenderer(context);

        sql.Append("UPDATE ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));
        sql.Append(" SET ");
        sql.Append(string.Join(", ", model.Assignments.Select(assignment => $"{_dialect.QuoteIdentifier(assignment.Column)} = @{parameters.Add(assignment.Value)}")));

        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), context.DynamicParameters);
    }

    public RenderedSqlCommand Render(DeleteCommandModel model)
    {
        if (!model.AllowAllRows && model.Filters.Count == 0)
        {
            throw new InvalidOperationException("DELETE requires a WHERE clause. Call AllowAllRows() to delete every row explicitly.");
        }

        var sql = new StringBuilder();
        var context = new SqlRenderContext(_dialect);
        var parameters = context.Parameters;
        var filters = new FilterSqlRenderer(context);

        sql.Append("DELETE FROM ");
        sql.Append(_dialect.QuoteIdentifier(model.Table));

        filters.Render(sql, model.Filters);

        return new RenderedSqlCommand(sql.ToString(), context.DynamicParameters);
    }
}
