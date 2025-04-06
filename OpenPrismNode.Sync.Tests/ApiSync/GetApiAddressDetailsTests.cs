using System.Text.Json;
using FluentAssertions;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiAddressDetails;

namespace OpenPrismNode.Sync.Tests.ApiSync;

/// <summary>
/// Tests for the GetApiAddressDetails functionality
/// </summary>
public class GetApiAddressDetailsTests
{
    private const string TestAddress = "addr1qxck7s0avpq025302qd2dt7dlqwhfs5wc6dlpmgcnqfzxpxrh37tv4lxg5aaptxw689mgjr5jmqmpl34y9ufwdqlya2q7uv9cd";

    [Fact]
    public void CreateRequest_WithValidAddress_SetsAddressProperty()
    {
        // Arrange
        const string address = "addr1test123456";
        
        // Act
        var request = new GetApiAddressDetailsRequest(address);
        
        // Assert
        request.Address.Should().Be(address);
    }
    
    [Fact]
    public void ApiResponseAddress_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""address"": ""addr1qxck7s0avpq025302qd2dt7dlqwhfs5wc6dlpmgcnqfzxpxrh37tv4lxg5aaptxw689mgjr5jmqmpl34y9ufwdqlya2q7uv9cd"",
            ""amount"": [
                {
                    ""unit"": ""lovelace"",
                    ""quantity"": ""5000000""
                }
            ],
            ""stake_address"": ""stake_test1uqrw9tjymlm8wrwq7jk68n6v7fs9qz8z0wl8rt9e392fgc0ckqz7a"",
            ""type"": ""shelley"",
            ""script"": false
        }";
        
        // Act
        var result = JsonSerializer.Deserialize<ApiResponseAddress>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result!.Address.Should().Be(TestAddress);
        result.Amount.Should().HaveCount(1);
        result.Amount[0].Unit.Should().Be("lovelace");
        result.Amount[0].Quantity.Should().Be("5000000");
        result.StakeAddress.Should().Be("stake_test1uqrw9tjymlm8wrwq7jk68n6v7fs9qz8z0wl8rt9e392fgc0ckqz7a");
        result.Type.Should().Be("shelley");
        result.Script.Should().BeFalse();
    }
    
    [Fact]
    public void AddressAmount_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""unit"": ""lovelace"",
            ""quantity"": ""5000000""
        }";

        // Act
        var result = JsonSerializer.Deserialize<AddressAmount>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result!.Unit.Should().Be("lovelace");
        result.Quantity.Should().Be("5000000");
    }
}