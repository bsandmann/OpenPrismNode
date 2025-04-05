using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OpenPrismNode.Core.DbSyncModels;
using OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockInEpoch;
using OpenPrismNode.Sync.Commands.ApiSync.GetFirstBlockOfEpoch;

namespace OpenPrismNode.Sync.Tests.ApiSync;

/// <summary>
/// Tests for the GetFirstBlockOfEpoch functionality
/// </summary>
public class GetFirstBlockOfEpochTests
{
    [Fact]
    public void CreateRequest_WithValidEpochNumber_SetsEpochNumberProperty()
    {
        // Arrange
        const int epochNumber = 250;
        
        // Act
        var request = new GetFirstBlockOfEpochRequest(epochNumber);
        
        // Assert
        request.EpochNumber.Should().Be(epochNumber);
    }
    
    [Fact]
    public async Task Handler_ShouldReturnFirstBlock_WhenBlockFoundInFirstSlot()
    {
        // Arrange
        const int epochNumber = 250;
        const int firstSlot = 0;
        
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<GetFirstBlockOfEpochHandler>>();
        
        var expectedBlock = new Block
        {
            id = 123,
            block_no = 5000000,
            epoch_no = epochNumber,
            tx_count = 10,
            time = DateTime.UtcNow,
            hash = new byte[32] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 },
            previousHash = new byte[32],
            previous_id = 122
        };
        
        // Setup the mediator to return a block for the first slot (slot 0)
        mockMediator
            .Setup(m => m.Send(
                It.Is<GetApiBlockInEpochRequest>(req => req.EpochNumber == epochNumber && req.SlotNumber == firstSlot),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<Block?>(expectedBlock));
        
        var handler = new GetFirstBlockOfEpochHandler(mockMediator.Object, mockLogger.Object);
        
        // Act
        var result = await handler.Handle(new GetFirstBlockOfEpochRequest(epochNumber), CancellationToken.None);
        
        // Assert
        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedBlock);
        
        // Verify that mediator was called exactly once with the right parameters
        mockMediator.Verify(
            m => m.Send(
                It.Is<GetApiBlockInEpochRequest>(req => req.EpochNumber == epochNumber && req.SlotNumber == firstSlot),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handler_ShouldReturnError_WhenBlockInEpochRequestFails()
    {
        // Arrange
        const int epochNumber = 250;
        const string expectedErrorMessage = "API connection failure";
        
        var mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<GetFirstBlockOfEpochHandler>>();
        
        // Setup the mediator to return an error
        mockMediator
            .Setup(m => m.Send(
                It.IsAny<GetApiBlockInEpochRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<Block?>(expectedErrorMessage));
        
        var handler = new GetFirstBlockOfEpochHandler(mockMediator.Object, mockLogger.Object);
        
        // Act
        var result = await handler.Handle(new GetFirstBlockOfEpochRequest(epochNumber), CancellationToken.None);
        
        // Assert
        result.Should().BeFailure();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be(expectedErrorMessage);
        
        // Verify that mediator was called exactly once (since we return error immediately)
        mockMediator.Verify(
            m => m.Send(
                It.IsAny<GetApiBlockInEpochRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}