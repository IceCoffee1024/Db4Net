using System.Data;

namespace Db4Net;

/// <summary>
/// Configures options used when executing a rendered Db4Net command through Dapper.
/// </summary>
public sealed class Db4NetExecutionOptions
{
    /// <summary>
    /// Gets or initializes the transaction used by Dapper.
    /// </summary>
    public IDbTransaction? Transaction { get; init; }

    /// <summary>
    /// Gets or initializes the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; init; }

    /// <summary>
    /// Gets or initializes the database command type.
    /// </summary>
    public CommandType? CommandType { get; init; }

    internal Action? ValidateBeforeExecute { get; init; }

    internal void Validate()
    {
        ValidateBeforeExecute?.Invoke();
    }

    internal static Db4NetExecutionOptions? Merge(Db4NetExecutionOptions? defaults, Db4NetExecutionOptions? overrides)
    {
        if (defaults is null)
        {
            return overrides;
        }

        if (overrides is null)
        {
            return defaults;
        }

        return new Db4NetExecutionOptions
        {
            Transaction = overrides.Transaction ?? defaults.Transaction,
            CommandTimeout = overrides.CommandTimeout ?? defaults.CommandTimeout,
            CommandType = overrides.CommandType ?? defaults.CommandType,
            ValidateBeforeExecute = Combine(defaults.ValidateBeforeExecute, overrides.ValidateBeforeExecute)
        };
    }

    private static Action? Combine(Action? first, Action? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        return () =>
        {
            first();
            second();
        };
    }
}
