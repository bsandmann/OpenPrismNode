using System.Text.Json;
using FluentAssertions;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockInEpoch;

namespace OpenPrismNode.Sync.Tests.ApiSync;

/// <summary>
/// Tests for the GetApiBlockInEpoch functionality
/// </summary>
public class GetApiBlockInEpochTests
{
    [Fact]
    public void CreateRequest_WithValidEpochAndSlotNumbers_SetsProperties()
    {
        // Arrange
        const int epochNumber = 208;
        const int slotNumber = 100712;
        
        // Act
        var request = new GetApiBlockInEpochRequest(epochNumber, slotNumber);
        
        // Assert
        request.EpochNumber.Should().Be(epochNumber);
        request.SlotNumber.Should().Be(slotNumber);
    }
    
    [Fact]
    public void BlockfrostBlockResponse_FromEpochSlotEndpoint_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""time"": 1586620868,
            ""height"": 4490598,
            ""hash"": ""f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457"",
            ""slot"": 4523112,
            ""epoch"": 208,
            ""epoch_slot"": 100712,
            ""slot_leader"": ""pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy"",
            ""size"": 3408,
            ""tx_count"": 5,
            ""output"": ""12589943761360"",
            ""fees"": ""1035200"",
            ""block_vrf"": ""vrf_vk1wvqpqrndy4stunzd8kswc9snqsdpr8z4xh5n5qcmmvnav7c40f8qlkm2qp"",
            ""op_cert"": ""a9a77be6da0d440f53ca7ff4d93a47fa8660dc9c33e61cd9e501a4772cd7523e"",
            ""op_cert_counter"": ""5"",
            ""previous_block"": ""16dfb25485cad6c519a3614884ebb961667341f04ba4173839ca7574b32c2654"",
            ""next_block"": ""909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42"",
            ""confirmations"": 2154755
        }";
        
        // Act
        var result = JsonSerializer.Deserialize<BlockfrostBlockResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result!.Time.Should().Be(1586620868);
        result.Height.Should().Be(4490598);
        result.Hash.Should().Be("f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457");
        result.Slot.Should().Be(4523112);
        result.Epoch.Should().Be(208);
        result.EpochSlot.Should().Be(100712); // This is important for this specific endpoint
        result.SlotLeader.Should().Be("pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy");
        result.Size.Should().Be(3408);
        result.TxCount.Should().Be(5);
    }
    
    [Fact]
    public void BlockfrostBlockMapper_ShouldMapResponseFromEpochSlot_ToBlock()
    {
        // Arrange
        var blockResponse = new BlockfrostBlockResponse
        {
            Time = 1586620868,
            Height = 4490598,
            Hash = "f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457",
            Slot = 4523112,
            Epoch = 208,
            EpochSlot = 100712, // Specific for epoch/slot endpoint
            SlotLeader = "pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy",
            TxCount = 5,
            PreviousBlock = "16dfb25485cad6c519a3614884ebb961667341f04ba4173839ca7574b32c2654"
        };
        
        // Act
        var result = BlockfrostBlockMapper.MapToBlock(blockResponse);
        
        // Assert
        result.Should().NotBeNull();
        result.block_no.Should().Be(blockResponse.Height);
        result.epoch_no.Should().Be(blockResponse.Epoch);
        result.tx_count.Should().Be(blockResponse.TxCount);
        
        // Verify DateTime conversion
        var expectedTime = DateTimeOffset.FromUnixTimeSeconds(blockResponse.Time).DateTime;
        result.time.Should().Be(expectedTime);
        
        // Verify byte array conversions
        result.hash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponse.Hash));
        result.previousHash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponse.PreviousBlock));
        
        // Verify placeholder values
        result.id.Should().Be(-1);
        result.previous_id.Should().Be(-1);
    }
    
    [Fact]
    public void BlockResponseWithEpochInformationShouldHaveCorrectEpochAndSlot()
    {
        // Arrange
        const int epochNumber = 208;
        const int slotNumber = 100712;
        
        var blockResponse = new BlockfrostBlockResponse
        {
            Epoch = epochNumber,
            EpochSlot = slotNumber,
            Slot = 4523112, // absolute slot number (across all epochs)
            Time = 1586620868,
            Height = 4490598,
            Hash = "f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457",
            TxCount = 5
        };
        
        // Assert
        blockResponse.Epoch.Should().Be(epochNumber);
        blockResponse.EpochSlot.Should().Be(slotNumber);
        
        // Additional verification
        blockResponse.Slot.Should().BeGreaterThan(blockResponse.EpochSlot); // Absolute slot should be larger than epoch slot
    }
}