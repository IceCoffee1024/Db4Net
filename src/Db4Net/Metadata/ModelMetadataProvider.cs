using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Db4Net.Metadata;

internal static class ModelMetadataProvider
{
    public static string GetTableName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return BuildTableName(type);
    }

    public static string GetColumnName<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);
        var member = GetMemberInfo(memberSelector);
        return ModelMetadata<T>.GetColumn(member.Name).ColumnName;
    }

    public static ColumnMetadata GetColumnMetadata<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);
        var member = GetMemberInfo(memberSelector);
        return ModelMetadata<T>.GetColumn(member.Name);
    }

    public static IReadOnlyList<ColumnMetadata> GetColumnMetadata(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return BuildColumnMetadata(type);
    }

    internal static string BuildTableName(Type type)
    {
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
    }

    internal static ColumnMetadata[] BuildColumnMetadata(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsMappedProperty)
            .Select(property => new ColumnMetadata(property.Name, GetColumnName(property)))
            .ToArray();
    }

    private static string GetColumnName(MemberInfo member)
    {
        return member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
    }

    private static PropertyInfo GetMemberInfo<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        Expression expression = memberSelector.Body;

        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            expression = unary.Operand;
        }

        if (expression is not MemberExpression memberExpression)
        {
            throw new ArgumentException("Only simple member selectors are supported, for example u => u.Id.", nameof(memberSelector));
        }

        if (memberExpression.Expression is not ParameterExpression)
        {
            throw new ArgumentException("Only direct member selectors are supported, for example u => u.Id.", nameof(memberSelector));
        }

        if (memberExpression.Member is not PropertyInfo property)
        {
            throw new ArgumentException("Only property selectors are supported, for example u => u.Id.", nameof(memberSelector));
        }

        return property;
    }

    private static bool IsMappedProperty(PropertyInfo property)
    {
        return property.GetCustomAttribute<NotMappedAttribute>() is null
            && property.GetMethod is not null
            && property.SetMethod is not null
            && property.GetIndexParameters().Length == 0;
    }
}

internal static class ModelMetadata<T>
{
    public static readonly string TableName = ModelMetadataProvider.BuildTableName(typeof(T));

    public static readonly IReadOnlyList<ColumnMetadata> Columns = ModelMetadataProvider.BuildColumnMetadata(typeof(T));

    private static readonly Dictionary<string, ColumnMetadata> ColumnsByPropertyName =
        Columns.ToDictionary(column => column.PropertyName, StringComparer.Ordinal);

    public static ColumnMetadata GetColumn(string propertyName)
    {
        if (ColumnsByPropertyName.TryGetValue(propertyName, out var column))
        {
            return column;
        }

        throw new ArgumentException($"Member '{typeof(T).Name}.{propertyName}' is not a mapped column.");
    }
}

internal sealed record ColumnMetadata(string PropertyName, string ColumnName);
