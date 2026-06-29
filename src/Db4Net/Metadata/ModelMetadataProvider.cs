using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Db4Net.Metadata;

internal static class ModelMetadataProvider
{
    public static void EnsureMappedModelType<T>()
    {
        EnsureMappedModelType(typeof(T));
    }

    public static void EnsureMappedModelType(Type type)
    {
        ThrowHelper.ThrowIfNull(type);
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            throw new ArgumentException($"Type '{GetDisplayName(type)}' does not have any mapped columns.");
        }

        if (IsSequenceType(type))
        {
            throw new ArgumentException($"Type '{GetDisplayName(type)}' is a sequence type and cannot be used as a mapped entity type.");
        }
    }

    public static bool IsSequenceType(Type type)
    {
        return type != typeof(string)
            && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    public static string GetDisplayName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericName = type.Name.Substring(0, type.Name.IndexOf('`'));
        return $"{genericName}<{string.Join(", ", type.GetGenericArguments().Select(GetDisplayName))}>";
    }

    public static string GetTableName(Type type)
    {
        return BuildTableName(type);
    }

    public static string GetColumnName<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ThrowHelper.ThrowIfNull(memberSelector);
        var member = GetMemberInfo(memberSelector);
        return ModelMetadata<T>.GetColumn(member.Name).ColumnName;
    }

    public static ColumnMetadata GetColumnMetadata<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ThrowHelper.ThrowIfNull(memberSelector);
        var member = GetMemberInfo(memberSelector);
        return ModelMetadata<T>.GetColumn(member.Name);
    }

    public static IReadOnlyList<ColumnMetadata> GetColumnMetadata(Type type)
    {
        ThrowHelper.ThrowIfNull(type);
        return BuildColumnMetadata(type);
    }

    internal static string BuildTableName(Type type)
    {
        EnsureMappedModelType(type);
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
    }

    internal static ColumnMetadata[] BuildColumnMetadata(Type type)
    {
        EnsureMappedModelType(type);

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var explicitKeyProperties = new HashSet<PropertyInfo>(
            properties.Where(property => property.GetCustomAttribute<KeyAttribute>() is not null));

        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsMappedProperty)
            .Select(property => new ColumnMetadata(
                property.Name,
                GetColumnName(property),
                property,
                IsKeyProperty(type, property, explicitKeyProperties),
                GetDatabaseGeneratedOption(property)))
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

    private static bool IsKeyProperty(Type type, PropertyInfo property, ISet<PropertyInfo> explicitKeyProperties)
    {
        if (explicitKeyProperties.Count > 0)
        {
            return explicitKeyProperties.Contains(property);
        }

        return property.Name == "Id"
            || property.Name == $"{type.Name}Id";
    }

    private static DatabaseGeneratedOption? GetDatabaseGeneratedOption(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
        return attribute?.DatabaseGeneratedOption;
    }
}

internal static class ModelMetadata<T>
{
    public static readonly string TableName = ModelMetadataProvider.BuildTableName(typeof(T));

    public static readonly IReadOnlyList<ColumnMetadata> Columns = ModelMetadataProvider.BuildColumnMetadata(typeof(T));

    public static readonly IReadOnlyList<ColumnMetadata> KeyColumns = Columns.Where(column => column.IsKey).ToArray();

    public static readonly IReadOnlyList<ColumnMetadata> NonKeyColumns = Columns.Where(column => !column.IsKey).ToArray();

    public static readonly IReadOnlyList<ColumnMetadata> UpdateColumns = Columns.Where(column => !column.IsKey && !column.IsDatabaseGenerated).ToArray();

    public static readonly IReadOnlyList<ColumnMetadata> InsertColumns = Columns.Where(column => !column.IsDatabaseGenerated).ToArray();

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

    public static IReadOnlyList<ColumnMetadata> RequireKeyColumns()
    {
        if (KeyColumns.Count == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' does not have a key. Add [Key] or an Id/{typeof(T).Name}Id property.");
        }

        if (KeyColumns.Count > 1)
        {
            throw new InvalidOperationException($"Composite keys are not supported for type '{typeof(T).Name}'. Use explicit Where clauses instead.");
        }

        return KeyColumns;
    }

    public static IReadOnlyList<ColumnMetadata> RequireConflictColumns()
    {
        if (KeyColumns.Count == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(T).Name}' does not have a key. Add [Key] or an Id/{typeof(T).Name}Id property.");
        }

        return KeyColumns;
    }
}

internal sealed record ColumnMetadata(string PropertyName, string ColumnName, PropertyInfo Property, bool IsKey, DatabaseGeneratedOption? DatabaseGeneratedOption)
{
    public bool IsDatabaseGenerated => DatabaseGeneratedOption is System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity or System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Computed;

    public bool IsDatabaseGeneratedIdentity => DatabaseGeneratedOption == System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity;

    public object? GetValue(object entity)
    {
        return Property.GetValue(entity);
    }
}
