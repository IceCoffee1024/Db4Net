using MySqlConnector;

namespace Db4Net.Tests;

internal static class ExternalDatabaseTestSupport
{
    public static string GetRequiredConnectionString(string environmentVariable)
    {
        var connectionString = Environment.GetEnvironmentVariable(environmentVariable);
        Skip.If(string.IsNullOrWhiteSpace(connectionString), $"{environmentVariable} is not set.");
        return connectionString!;
    }

    public static string CreateTableName(string provider, string purpose)
    {
        return $"db4net_{provider}_{purpose}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Skips the test if the connected MySQL server does not support row-alias upsert syntax
    /// (INSERT ... AS _new ON DUPLICATE KEY UPDATE). Requires MySQL >= 8.0.19.
    /// MySQL 5.7, 8.0.0–8.0.18, and all MariaDB versions will fail with a syntax error.
    /// </summary>
    public static async Task SkipIfMySqlRowAliasSyntaxUnsupportedAsync(MySqlConnection connection)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT VERSION()";
        var versionString = (string?)await cmd.ExecuteScalarAsync();
        if (versionString is null) return;

        // MariaDB reports versions like "10.x.y-MariaDB" — always skip.
        if (versionString.Contains("MariaDB", StringComparison.OrdinalIgnoreCase))
        {
            Skip.If(true, "Row-alias ON DUPLICATE KEY UPDATE syntax is not supported on MariaDB.");
            return;
        }

        // Parse major.minor.patch from "8.0.19" or "8.0.19-something".
        var dotParts = versionString.Split('-')[0].Split('.');
        if (dotParts.Length >= 3
            && int.TryParse(dotParts[0], out var major)
            && int.TryParse(dotParts[1], out var minor)
            && int.TryParse(dotParts[2], out var patch))
        {
            var version = new Version(major, minor, patch);
            Skip.If(version < new Version(8, 0, 19),
                $"Row-alias ON DUPLICATE KEY UPDATE syntax requires MySQL >= 8.0.19. Server: {versionString}");
        }
    }
}
