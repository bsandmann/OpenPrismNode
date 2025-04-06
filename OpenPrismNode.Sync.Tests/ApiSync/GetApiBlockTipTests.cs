using System.Text.Json;
using FluentAssertions;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip;

namespace OpenPrismNode.Sync.Tests.ApiSync;

/// <summary>
/// Tests for the GetApiBlockTip functionality
/// </summary>
public class GetApiBlockTipTests
{
    [Fact]
    public void CreateRequest_ShouldCreateValidRequest()
    {
        // Act
        var request = new GetApiBlockTipRequest();
        
        // Assert
        request.Should().NotBeNull();
    }
    
    [Fact]
    public void BlockfrostBlockResponse_FromLatestEndpoint_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""time"": 1619817600,
            ""height"": 6529015,
            ""hash"": ""9a76db4727cfca14f2b90b3e5b8eacc4cd5f4841bcf4e53cc6e257a732ef12fd"",
            ""slot"": 39916982,
            ""epoch"": 259,
            ""epoch_slot"": 362182,
            ""slot_leader"": ""pool19f6guwy034dl0fmjgkheasvad37g3ndjn5jhnkgyr8yczmwnnny"",
            ""size"": 79107,
            ""tx_count"": 30,
            ""output"": ""7731947443010307"",
            ""fees"": ""6592330"",
            ""block_vrf"": ""vrf_vk1s3pmh5a3x2l6mmudlg70vy0k4aumezpnkn4fh700v0yvk5fe0eesn66zn3"",
            ""op_cert"": ""95bacd7bff752f4b0c0b7a258d47198f7d938f22f7202a63df2ff1208ccc585d"",
            ""op_cert_counter"": ""3"",
            ""previous_block"": ""40f9f8a3c001b2b85e5d7c95ca3a044fef8166f3e6a4ed4b0ccb872eb81fbfe6"",
            ""confirmations"": 0
        }";
        
        // Act
        var result = JsonSerializer.Deserialize<BlockfrostBlockResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result!.Time.Should().Be(1619817600);
        result.Height.Should().Be(6529015);
        result.Hash.Should().Be("9a76db4727cfca14f2b90b3e5b8eacc4cd5f4841bcf4e53cc6e257a732ef12fd");
        result.Slot.Should().Be(39916982);
        result.Epoch.Should().Be(259);
        result.EpochSlot.Should().Be(362182);
        result.SlotLeader.Should().Be("pool19f6guwy034dl0fmjgkheasvad37g3ndjn5jhnkgyr8yczmwnnny");
        result.Size.Should().Be(79107);
        result.TxCount.Should().Be(30);
        result.Output.Should().Be("7731947443010307");
        result.Fees.Should().Be("6592330");
        result.BlockVrf.Should().Be("vrf_vk1s3pmh5a3x2l6mmudlg70vy0k4aumezpnkn4fh700v0yvk5fe0eesn66zn3");
        result.OpCert.Should().Be("95bacd7bff752f4b0c0b7a258d47198f7d938f22f7202a63df2ff1208ccc585d");
        result.OpCertCounter.Should().Be("3");
        result.PreviousBlock.Should().Be("40f9f8a3c001b2b85e5d7c95ca3a044fef8166f3e6a4ed4b0ccb872eb81fbfe6");
        result.Confirmations.Should().Be(0); // Latest block typically has 0 confirmations
    }
    
    [Fact]
    public void BlockfrostBlockMapper_ShouldMapLatestBlockResponse_ToBlock()
    {
        // Arrange
        var blockResponse = new BlockfrostBlockResponse
        {
            Time = 1619817600,
            Height = 6529015,
            Hash = "9a76db4727cfca14f2b90b3e5b8eacc4cd5f4841bcf4e53cc6e257a732ef12fd",
            Slot = 39916982,
            Epoch = 259,
            EpochSlot = 362182,
            SlotLeader = "pool19f6guwy034dl0fmjgkheasvad37g3ndjn5jhnkgyr8yczmwnnny",
            Size = 79107,
            TxCount = 30,
            Output = "7731947443010307",
            Fees = "6592330",
            BlockVrf = "vrf_vk1s3pmh5a3x2l6mmudlg70vy0k4aumezpnkn4fh700v0yvk5fe0eesn66zn3",
            OpCert = "95bacd7bff752f4b0c0b7a258d47198f7d938f22f7202a63df2ff1208ccc585d",
            OpCertCounter = "3",
            PreviousBlock = "40f9f8a3c001b2b85e5d7c95ca3a044fef8166f3e6a4ed4b0ccb872eb81fbfe6",
            Confirmations = 0 // Latest block has 0 confirmations
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
    public void LatestBlock_ShouldHaveZeroConfirmations()
    {
        // Arrange
        var json = @"{
            ""time"": 1619817600,
            ""height"": 6529015,
            ""hash"": ""9a76db4727cfca14f2b90b3e5b8eacc4cd5f4841bcf4e53cc6e257a732ef12fd"",
            ""slot"": 39916982,
            ""epoch"": 259,
            ""confirmations"": 0
        }";
        
        // Act
        var result = JsonSerializer.Deserialize<BlockfrostBlockResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Assert
        result.Should().NotBeNull();
        result!.Confirmations.Should().Be(0);
    }
}