namespace OpenPrismNode.Core.IntegrationTests;

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class TransactionalTestDatabaseFixture
{
    public DataContext CreateContext()
    {
        var connectionString = System.Environment.GetEnvironmentVariable("PrismPostgresConnectionString");
        return new DataContext(
            new DbContextOptionsBuilder<DataContext>()
                .UseNpgsql(connectionString)
                .EnableSensitiveDataLogging(true)
                .LogTo(Console.WriteLine, LogLevel.Information)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .Options);
    }

    public TransactionalTestDatabaseFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        Cleanup();
    }

    public void Cleanup()
    {
    }
}