namespace Karasu.ERP.Persistence.Configuration;

public enum DatabaseProvider
{
    SqlServer,
    PostgreSQL
}

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
    public string? ConnectionString { get; set; }
}
