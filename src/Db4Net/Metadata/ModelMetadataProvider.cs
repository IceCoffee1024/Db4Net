using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace Db4Net.Metadata;

internal static class ModelMetadataProvider
{
    public static string GetTableName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
    }

    public static string GetColumnName<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);
        return GetColumnName(GetMemberInfo(memberSelector));
    }

    public static ColumnMetadata GetColumnMetadata<T, TValue>(Expression<Func<T, TValue>> memberSelector)
    {
        ArgumentNullException.ThrowIfNull(memberSelector);
        var member = GetMemberInfo(memberSelector);
        return new ColumnMetadata(member.Name, GetColumnName(member));
    }

    public static IReadOnlyList<ColumnMetadata> GetColumnMetadata(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
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

    private static MemberInfo GetMemberInfo<T, TValue>(Expression<Func<T, TValue>> memberSelector)
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

        return memberExpression.Member;
    }

    private static bool IsMappedProperty(PropertyInfo property)
    {
        return property.GetCustomAttribute<NotMappedAttribute>() is null
            && property.GetMethod is not null
            && property.SetMethod is not null
            && property.GetIndexParameters().Length == 0;
    }
}

internal sealed record ColumnMetadata(string PropertyName, string ColumnName);
