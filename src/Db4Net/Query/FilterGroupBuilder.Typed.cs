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
    /// Adds an AND <c>IN</c> filter using a CLR property name from <typeparamref name="T"/> and a single-column SELECT subquery.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> WhereIn(string propertyName, SelectQueryBuilder subquery)
    {
        base.WhereIn(ModelMetadata<T>.GetColumn(propertyName).ColumnName, subquery);
        return this;
    }

    /// <summary>
    /// Adds an AND <c>IN</c> filter using a typed member selector and a single-column SELECT subquery.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> WhereIn<TValue>(Expression<Func<T, TValue>> memberSelector, SelectQueryBuilder subquery)
    {
        base.WhereIn(ModelMetadataProvider.GetColumnName(memberSelector), subquery);
        return this;
    }

    /// <summary>
    /// Adds an AND <c>NOT IN</c> filter using a CLR property name from <typeparamref name="T"/> and a single-column SELECT subquery.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> WhereNotIn(string propertyName, SelectQueryBuilder subquery)
    {
        base.WhereNotIn(ModelMetadata<T>.GetColumn(propertyName).ColumnName, subquery);
        return this;
    }

    /// <summary>
    /// Adds an AND <c>NOT IN</c> filter using a typed member selector and a single-column SELECT subquery.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> WhereNotIn<TValue>(Expression<Func<T, TValue>> memberSelector, SelectQueryBuilder subquery)
    {
        base.WhereNotIn(ModelMetadataProvider.GetColumnName(memberSelector), subquery);
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
    /// Adds an OR <c>IN</c> filter using a CLR property name from <typeparamref name="T"/> and a single-column SELECT subquery.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> OrWhereIn(string propertyName, SelectQueryBuilder subquery)
    {
        base.OrWhereIn(ModelMetadata<T>.GetColumn(propertyName).ColumnName, subquery);
        return this;
    }

    /// <summary>
    /// Adds an OR <c>IN</c> filter using a typed member selector and a single-column SELECT subquery.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> OrWhereIn<TValue>(Expression<Func<T, TValue>> memberSelector, SelectQueryBuilder subquery)
    {
        base.OrWhereIn(ModelMetadataProvider.GetColumnName(memberSelector), subquery);
        return this;
    }

    /// <summary>
    /// Adds an OR <c>NOT IN</c> filter using a CLR property name from <typeparamref name="T"/> and a single-column SELECT subquery.
    /// </summary>
    /// <param name="propertyName">The CLR property name to filter by.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public new FilterGroupBuilder<T> OrWhereNotIn(string propertyName, SelectQueryBuilder subquery)
    {
        base.OrWhereNotIn(ModelMetadata<T>.GetColumn(propertyName).ColumnName, subquery);
        return this;
    }

    /// <summary>
    /// Adds an OR <c>NOT IN</c> filter using a typed member selector and a single-column SELECT subquery.
    /// </summary>
    /// <typeparam name="TValue">The selected member value type.</typeparam>
    /// <param name="memberSelector">A simple member selector, for example <c>u =&gt; u.Id</c>.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder<T> OrWhereNotIn<TValue>(Expression<Func<T, TValue>> memberSelector, SelectQueryBuilder subquery)
    {
        base.OrWhereNotIn(ModelMetadataProvider.GetColumnName(memberSelector), subquery);
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
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder<T>();
        configure(group);
        base.AddGroup(booleanOperator, group.Filters);
    }
}
