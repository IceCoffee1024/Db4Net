namespace Db4Net.Query;

/// <summary>
/// Builds a parenthesized filter group using string-based column identifiers.
/// </summary>
public class FilterGroupBuilder
{
    private readonly FilterClauseBuilder _filters = new();

    internal IReadOnlyList<FilterNode> Filters => _filters.Filters;

    /// <summary>
    /// Adds an AND filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder Where(string column, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.And, column, op, value);
        return this;
    }

    /// <summary>
    /// Adds an AND null-check filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder Where(string column, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.And, column, op);
        return this;
    }

    /// <summary>
    /// Adds an AND filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereIf(bool condition, string column, Op op, object? value)
    {
        return condition ? Where(column, op, value) : this;
    }

    /// <summary>
    /// Adds an AND null-check filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereIf(bool condition, string column, Op op)
    {
        return condition ? Where(column, op) : this;
    }

    /// <summary>
    /// Adds an AND <c>IN</c> filter using a string-based column identifier and a single-column SELECT subquery.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereIn(string column, SelectQueryBuilder subquery)
    {
        AddSubqueryFilter(FilterBooleanOperator.And, column, negated: false, subquery);
        return this;
    }

    /// <summary>
    /// Adds an AND <c>NOT IN</c> filter using a string-based column identifier and a single-column SELECT subquery.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereNotIn(string column, SelectQueryBuilder subquery)
    {
        AddSubqueryFilter(FilterBooleanOperator.And, column, negated: true, subquery);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereGroup(Action<FilterGroupBuilder> configure)
    {
        AddGroup(FilterBooleanOperator.And, configure);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized AND filter group only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the group.</param>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder WhereGroupIf(bool condition, Action<FilterGroupBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);
        if (!condition)
        {
            return this;
        }

        var group = new FilterGroupBuilder();
        configure(group);
        if (group.Filters.Count > 0)
        {
            AddGroup(FilterBooleanOperator.And, group.Filters);
        }

        return this;
    }

    /// <summary>
    /// Adds an OR filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhere(string column, Op op, object? value)
    {
        _filters.Add(FilterBooleanOperator.Or, column, op, value);
        return this;
    }

    /// <summary>
    /// Adds an OR null-check filter using a string-based column identifier.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhere(string column, Op op)
    {
        _filters.AddValueFree(FilterBooleanOperator.Or, column, op);
        return this;
    }

    /// <summary>
    /// Adds an OR filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL comparison operator.</param>
    /// <param name="value">The value to parameterize. <see cref="Op.In"/> requires a non-string enumerable.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhereIf(bool condition, string column, Op op, object? value)
    {
        return condition ? OrWhere(column, op, value) : this;
    }

    /// <summary>
    /// Adds an OR null-check filter only when <paramref name="condition"/> is true.
    /// </summary>
    /// <param name="condition">Whether to add the filter.</param>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="op">The SQL null-check operator. Only <see cref="Op.IsNull"/> and <see cref="Op.IsNotNull"/> are supported.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhereIf(bool condition, string column, Op op)
    {
        return condition ? OrWhere(column, op) : this;
    }

    /// <summary>
    /// Adds an OR <c>IN</c> filter using a string-based column identifier and a single-column SELECT subquery.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhereIn(string column, SelectQueryBuilder subquery)
    {
        AddSubqueryFilter(FilterBooleanOperator.Or, column, negated: false, subquery);
        return this;
    }

    /// <summary>
    /// Adds an OR <c>NOT IN</c> filter using a string-based column identifier and a single-column SELECT subquery.
    /// </summary>
    /// <param name="column">The column identifier. It is validated and quoted by the configured SQL dialect.</param>
    /// <param name="subquery">The single-column SELECT subquery used for the <c>NOT IN</c> predicate.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhereNotIn(string column, SelectQueryBuilder subquery)
    {
        AddSubqueryFilter(FilterBooleanOperator.Or, column, negated: true, subquery);
        return this;
    }

    /// <summary>
    /// Adds a parenthesized OR filter group.
    /// </summary>
    /// <param name="configure">Configures the nested filter group.</param>
    /// <returns>The current group builder.</returns>
    public FilterGroupBuilder OrWhereGroup(Action<FilterGroupBuilder> configure)
    {
        AddGroup(FilterBooleanOperator.Or, configure);
        return this;
    }

    internal void AddGroup(FilterBooleanOperator booleanOperator, Action<FilterGroupBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        var group = new FilterGroupBuilder();
        configure(group);
        AddGroup(booleanOperator, group.Filters);
    }

    internal void AddGroup(FilterBooleanOperator booleanOperator, IReadOnlyList<FilterNode> filters)
    {
        _filters.AddGroup(booleanOperator, filters);
    }

    private void AddSubqueryFilter(FilterBooleanOperator booleanOperator, string column, bool negated, SelectQueryBuilder subquery)
    {
        ThrowHelper.ThrowIfNull(subquery);
        _filters.AddSubquery(booleanOperator, column, negated, subquery.ToModelSnapshot());
    }
}
