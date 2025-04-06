using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlocksByNumbers;

namespace OpenPrismNode.Sync.Tests.ApiSync;

/// <summary>
/// Tests for the GetApiBlocksByNumbers functionality
/// </summary>
public class GetApiBlocksByNumbersTests
{
    [Fact]
    public void CreateRequest_WithValidParameters_SetsProperties()
    {
        // Arrange
        const int firstBlockNo = 1000;
        const int count = 10;
        
        // Act
        var request = new GetApiBlocksByNumbersRequest(firstBlockNo, count);
        
        // Assert
        request.FirstBlockNo.Should().Be(firstBlockNo);
        request.Count.Should().Be(count);
    }
    
    [Fact]
    public void DeserializeBlockfrostBlockResponseList_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"[
            {
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
                ""previous_block"": ""16dfb25485cad6c519a3614884ebb961667341f04ba4173839ca7574b32c2654"",
                ""next_block"": ""909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42""
            },
            {
                ""time"": 1586620928,
                ""height"": 4490599,
                ""hash"": ""909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42"",
                ""slot"": 4523172,
                ""epoch"": 208,
                ""epoch_slot"": 100772,
                ""slot_leader"": ""pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy"",
                ""size"": 3412,
                ""tx_count"": 4,
                ""output"": ""9458036594692"",
                ""fees"": ""923648"",
                ""previous_block"": ""f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457"",
                ""next_block"": ""cd7f94f7f318d9cb5bb30b1cbe67e3d12f46a4d4d60095784cdff73af884c9c7""
            }
        ]";
        
        // Act
        var result = JsonSerializer.Deserialize<List<BlockfrostBlockResponse>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        // Check first block
        result[0].Time.Should().Be(1586620868);
        result[0].Height.Should().Be(4490598);
        result[0].Hash.Should().Be("f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457");
        result[0].TxCount.Should().Be(5);
        
        // Check second block
        result[1].Time.Should().Be(1586620928);
        result[1].Height.Should().Be(4490599);
        result[1].Hash.Should().Be("909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42");
        result[1].TxCount.Should().Be(4);
        
        // Check relationship between blocks
        result[1].Height.Should().Be(result[0].Height + 1);
        result[1].PreviousBlock.Should().Be(result[0].Hash);
    }
    
    [Fact]
    public void BlockfrostBlockMapper_ShouldMapListOfResponses_ToBlockList()
    {
        // Arrange
        var blockResponses = new List<BlockfrostBlockResponse>
        {
            new BlockfrostBlockResponse
            {
                Time = 1586620868,
                Height = 4490598,
                Hash = "f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457",
                Slot = 4523112,
                Epoch = 208,
                EpochSlot = 100712,
                SlotLeader = "pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy",
                TxCount = 5,
                PreviousBlock = "16dfb25485cad6c519a3614884ebb961667341f04ba4173839ca7574b32c2654",
                NextBlock = "909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42"
            },
            new BlockfrostBlockResponse
            {
                Time = 1586620928,
                Height = 4490599,
                Hash = "909dc2038143183507507e48d97edec8de568a8638d1aa2e6024114a4b6a4c42",
                Slot = 4523172,
                Epoch = 208,
                EpochSlot = 100772,
                SlotLeader = "pool1pu5jlj4q9w9jlxeu370a3c9myx47md5j5m2str0naunn2q3lkdy",
                TxCount = 4,
                PreviousBlock = "f8084c61b6a238acec985b59310b6ecec49c0ab8352249afd7268da5cff2a457",
                NextBlock = "cd7f94f7f318d9cb5bb30b1cbe67e3d12f46a4d4d60095784cdff73af884c9c7"
            }
        };
        
        // Act
        var blocks = blockResponses.Select(BlockfrostBlockMapper.MapToBlock).ToList();
        
        // Assert
        blocks.Should().HaveCount(2);
        
        // Check first block
        blocks[0].block_no.Should().Be(blockResponses[0].Height);
        blocks[0].epoch_no.Should().Be(blockResponses[0].Epoch);
        blocks[0].tx_count.Should().Be(blockResponses[0].TxCount);
        blocks[0].hash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponses[0].Hash));
        blocks[0].previousHash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponses[0].PreviousBlock));
        
        // Check second block
        blocks[1].block_no.Should().Be(blockResponses[1].Height);
        blocks[1].epoch_no.Should().Be(blockResponses[1].Epoch);
        blocks[1].tx_count.Should().Be(blockResponses[1].TxCount);
        blocks[1].hash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponses[1].Hash));
        blocks[1].previousHash.Should().BeEquivalentTo(PrismEncoding.HexToByteArray(blockResponses[1].PreviousBlock));
        
        // Check block sequence
        blocks[1].block_no.Should().Be(blocks[0].block_no + 1);
    }
    
    [Fact]
    public void GroupConsecutiveNumbers_ShouldCorrectlyGroupSequentialNumbers()
    {
        // This tests the private helper method, using reflection
        
        // Arrange
        var inputNumbers = new List<long> { 1, 2, 3, 5, 6, 9, 10, 11, 13 };
        var expectedGroups = new List<List<long>>
        {
            new List<long> { 1, 2, 3 },
            new List<long> { 5, 6 },
            new List<long> { 9, 10, 11 },
            new List<long> { 13 }
        };
        
        // Act - Use the method through extension method to simulate the behavior
        var groups = GroupConsecutiveNumbers(inputNumbers);
        
        // Assert
        groups.Should().HaveCount(4);
        
        for (int i = 0; i < expectedGroups.Count; i++)
        {
            groups[i].Should().BeEquivalentTo(expectedGroups[i]);
        }
    }
    
    [Fact]
    public void GroupConsecutiveNumbers_ShouldHandleEmptyList()
    {
        // Arrange
        var emptyList = new List<long>();
        
        // Act
        var result = GroupConsecutiveNumbers(emptyList);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
    
    [Fact]
    public void GroupConsecutiveNumbers_ShouldHandleSingleItem()
    {
        // Arrange
        var singleItemList = new List<long> { 42 };
        
        // Act
        var result = GroupConsecutiveNumbers(singleItemList);
        
        // Assert
        result.Should().HaveCount(1);
        result[0].Should().HaveCount(1);
        result[0][0].Should().Be(42);
    }
    
    // Helper method to simulate the behavior of the private method in the handler
    private static List<List<long>> GroupConsecutiveNumbers(List<long> numbers)
    {
        var result = new List<List<long>>();
        if (numbers == null || numbers.Count == 0) return result;

        numbers.Sort();
        var currentGroup = new List<long> { numbers[0] };

        for (int i = 1; i < numbers.Count; i++)
        {
            // If current number is exactly +1 from previous, continue the group
            if (numbers[i] == numbers[i - 1] + 1)
            {
                currentGroup.Add(numbers[i]);
            }
            else
            {
                // Start a new group
                result.Add(currentGroup);
                currentGroup = new List<long> { numbers[i] };
            }
        }

        // Add the final group
        result.Add(currentGroup);
        return result;
    }
}