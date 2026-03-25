namespace App.Infrastructure.Config;

public sealed class SqlServerOptions
{
    public required string ConnectionString { get; init; }
}
