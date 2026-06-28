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
}
