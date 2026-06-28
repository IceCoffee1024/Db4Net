using Db4Net.Metadata;

namespace Db4Net.Commands;

internal static class ManyCommandBuilderSupport<T>
{
    public static T[] Materialize(IEnumerable<T> entities)
    {
        ThrowHelper.ThrowIfNull(entities);

        var materialized = entities.ToArray();
        foreach (var entity in materialized)
        {
            ThrowHelper.ThrowIfNull(entity, nameof(entities));
        }

        return materialized;
    }

    public static void EnsureNonDefaultKeyValues(IEnumerable<T> entities)
    {
        var keyColumn = RequireSingleKeyColumn();

        foreach (var entity in entities)
        {
            var value = keyColumn.GetValue(entity!);
            if (value is null || value.Equals(GetDefaultValue(keyColumn.Property.PropertyType)))
            {
                throw new InvalidOperationException($"Key '{typeof(T).Name}.{keyColumn.PropertyName}' has the default key value.");
            }
        }
    }

    public static ColumnMetadata RequireSingleKeyColumn()
    {
        return ModelMetadata<T>.RequireKeyColumns()[0];
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
