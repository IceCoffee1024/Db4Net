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
}
