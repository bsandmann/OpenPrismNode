namespace OpenPrismNode.Core.IntegrationTests;

using Xunit;

[CollectionDefinition("TransactionalTests", DisableParallelization = true)]
public class TransactionalTestsCollection : ICollectionFixture<TransactionalTestDatabaseFixture>
{
}