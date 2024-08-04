namespace OpenPrismNode.Sync.Tests.TransactionSerialization;

using System.Text.Json;
using FluentAssertions;
using OpenPrismNode.Grpc.Models;
using TestDocuments;

public class Deserialization
{
    [Fact]
    public void CreateDid_Transaction_deserializes_correctly_for_Prism_v1()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV1_CreateDid_Transaction;

        // Act
        var transaction = JsonSerializer.Deserialize<TransactionModel>(serializedTransaction);

        // Assert
        transaction.Should().NotBeNull();
        transaction!.Content.Count.Should().Be(5);
        transaction.Version.Should().Be(1);
    }
    
    [Fact]
    public void CreateDid_Transaction_deserializes_correctly_for_Prism_v2()
    {
        // Arrange
        var serializedTransaction = TransactionSampleData.PrismV2_CreateDid_Transaction;

        // Act
        var transaction = JsonSerializer.Deserialize<TransactionModel>(serializedTransaction);

        // Assert
        transaction.Should().NotBeNull();
        transaction!.Content.Count.Should().Be(5);
        transaction.Version.Should().Be(1);
    }
}