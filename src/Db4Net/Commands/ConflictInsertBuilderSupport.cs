using System.Linq.Expressions;
using Db4Net.Metadata;

namespace Db4Net.Commands;

internal static class ConflictInsertBuilderSupport<T>
{
    public static ColumnMetadata[] ResolveConflictColumns(IReadOnlyList<ColumnMetadata> explicitColumns)
    {
        var conflictColumns = explicitColumns.Count > 0
            ? explicitColumns.ToArray()
            : ModelMetadata<T>.RequireKeyColumns().ToArray();

        foreach (var column in conflictColumns)
        {
            if (column.IsDatabaseGenerated)
            {
                throw new ArgumentException($"Member '{typeof(T).Name}.{column.PropertyName}' is database-generated and cannot be used as a conflict target.");
            }
        }

        return conflictColumns;
    }

    public static ColumnMetadata[] ResolveUpdateColumns(
        IReadOnlyList<ColumnMetadata> explicitUpdateColumns,
        IReadOnlyList<ColumnMetadata> conflictColumns)
    {
        var updateColumns = explicitUpdateColumns.Count > 0
            ? explicitUpdateColumns.ToArray()
            : ModelMetadata<T>.InsertColumns
                .Where(column => !column.IsKey && !ContainsColumn(conflictColumns, column))
                .ToArray();

        if (updateColumns.Length == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' does not have any columns available for conflict updates.");
        }

        foreach (var column in updateColumns)
        {
            if (column.IsDatabaseGenerated)
            {
                throw new ArgumentException($"Member '{typeof(T).Name}.{column.PropertyName}' is database-generated and cannot be used as a conflict update column.");
            }

            if (ContainsColumn(conflictColumns, column))
            {
                throw new ArgumentException($"Member '{typeof(T).Name}.{column.PropertyName}' is part of the conflict target and cannot be used as a conflict update column.");
            }
        }

        return updateColumns;
    }

    public static ColumnMetadata[] GetColumns(params Expression<Func<T, object?>>[] memberSelectors)
    {
        if (memberSelectors.Length == 0)
        {
            throw new ArgumentException("At least one member selector is required.", nameof(memberSelectors));
        }

        return memberSelectors
            .Select(ModelMetadataProvider.GetColumnMetadata)
            .ToArray();
    }

    public static List<AssignmentClause> GetInsertValues(T entity)
    {
        ThrowHelper.ThrowIfNull(entity);
        return ModelMetadata<T>.InsertColumns
            .Select(column => new AssignmentClause(column.ColumnName, column.GetValue(entity)))
            .ToList();
    }

    private static bool ContainsColumn(IEnumerable<ColumnMetadata> columns, ColumnMetadata candidate)
    {
        return columns.Any(column => column.PropertyName == candidate.PropertyName);
    }
}
