using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Commands.CreateStakeAddress;
using OpenPrismNode.Core.Commands.CreateWalletAddress;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task CreateStakeAddress_Succeeds_For_New_Address()
    {
        // Arrange
        var stakeAddress = "stake1uyehkck0lajq8gr28t9uxnuvgcqrc6070x3k9r8048z8y5gh6ffgw";
        var request = new CreateStakeAddressRequest(stakeAddress);

        // Act
        var result = await _createStakeAddressHandler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify the stake address was created in the database
        var savedStakeAddress = await _context.StakeAddressEntities
            .FirstOrDefaultAsync(s => s.StakeAddress == stakeAddress);
        savedStakeAddress.Should().NotBeNull();
        savedStakeAddress.StakeAddress.Should().Be(stakeAddress);

        // Verify the stake address is in the cache
        var cachedStakeAddress = await _stakeAddressCache.GetOrAddAsync(stakeAddress, () => Task.FromResult<StakeAddressEntity>(null));
        cachedStakeAddress.Should().NotBeNull();
        cachedStakeAddress.StakeAddress.Should().Be(stakeAddress);
    }

    [Fact]
    public async Task CreateStakeAddress_Succeeds_For_Existing_Address()
    {
        // Arrange
        var stakeAddress = "stake1uyehkck0lajq8gr28t9uxnuvgcqrc6070x3k9r8048z8y5gh6ffgw";
        var existingStakeAddressEntity = new StakeAddressEntity { StakeAddress = stakeAddress };
        await _context.StakeAddressEntities.AddAsync(existingStakeAddressEntity);
        await _context.SaveChangesAsync();

        var request = new CreateStakeAddressRequest(stakeAddress);

        // Act
        var result = await _createStakeAddressHandler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify no new stake address was created in the database
        var stakeAddressCount = await _context.StakeAddressEntities.CountAsync(s => s.StakeAddress == stakeAddress);
        stakeAddressCount.Should().Be(1);

        // Verify the stake address is in the cache
        var cachedStakeAddress = await _stakeAddressCache.GetOrAddAsync(stakeAddress, () => Task.FromResult<StakeAddressEntity>(null));
        cachedStakeAddress.Should().NotBeNull();
        cachedStakeAddress.StakeAddress.Should().Be(stakeAddress);
    }

    [Fact]
    public async Task CreateStakeAddress_Succeeds_For_Unknown_Enterprise_Wallet()
    {
        // Arrange
        var stakeAddress = "Unknown_Enterprise_Wallet";
        var request = new CreateStakeAddressRequest(stakeAddress);

        // Act
        var result = await _createStakeAddressHandler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify the stake address was created in the database
        var savedStakeAddress = await _context.StakeAddressEntities
            .FirstOrDefaultAsync(s => s.StakeAddress == stakeAddress);
        savedStakeAddress.Should().NotBeNull();
        savedStakeAddress.StakeAddress.Should().Be(stakeAddress);

        // Verify the stake address is in the cache
        var cachedStakeAddress = await _stakeAddressCache.GetOrAddAsync(stakeAddress, () => Task.FromResult<StakeAddressEntity>(null));
        cachedStakeAddress.Should().NotBeNull();
        cachedStakeAddress.StakeAddress.Should().Be(stakeAddress);
    }
}