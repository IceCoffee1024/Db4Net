using System.Linq.Expressions;
using Db4Net.Metadata;

namespace Db4Net.Query;

/// <summary>
/// Builds a parenthesized filter group using typed member selectors for column mapping.
/// </summary>
/// <typeparam name="T">The CLR model type used for member mapping.</typeparam>
public sealed class FilterGroupBuilder<T> : FilterGroupBuilder
{
    /// <summary>
    /// Adds an AND filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> Where(string propertyName, Op op, object? value)
    {
        base.Where(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> Where(string propertyName, Op op)
    {
        base.Where(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        base.Where(ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> Where<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        base.Where(ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> WhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> OrWhere(string propertyName, Op op, object? value)
    {
        base.OrWhere(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a CLR property name from <typeparamref name="T"/>.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> OrWhere(string propertyName, Op op)
    {
        base.OrWhere(ModelMetadata<T>.GetColumn(propertyName).ColumnName, op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op, object? value)
    {
        base.OrWhere(ModelMetadataProvider.GetColumnName(memberSelector), op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a typed member selector.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.DeletedAt</c>.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> OrWhere<TValue>(Expression<Func<T, TValue>> memberSelector, Op op)
    {
        base.OrWhere(ModelMetadataProvider.GetColumnName(memberSelector), op);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> OrWhereGroup(Action<FilterGroupBuilder<T>> configure)
    {
        AddGroup(FilterBooleanOperator.Or, configure);
        return this;
    }

    private void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        base.AddGroup(booleanOperator, group.Filters);
    }
}
