namespace Db4Net;

/// <summary>
/// SQL operators supported by Db4Net filters.
/// </summary>
public enum Op
{
    /// <summary>Equal to.</summary>
    Eq,

    /// <summary>Not equal to.</summary>
    NotEq,

    /// <summary>Greater than.</summary>
    Gt,

    /// <summary>Greater than or equal to.</summary>
    Gte,

    /// <summary>Less than.</summary>
    Lt,

    /// <summary>Less than or equal to.</summary>
    Lte,

    /// <summary>SQL LIKE comparison.</summary>
    Like,

    /// <summary>SQL IN comparison. The value must be a non-string enumerable.</summary>
    In,

    /// <summary>SQL IS NULL comparison.</summary>
    IsNull,

    /// <summary>SQL IS NOT NULL comparison.</summary>
    IsNotNull
}
