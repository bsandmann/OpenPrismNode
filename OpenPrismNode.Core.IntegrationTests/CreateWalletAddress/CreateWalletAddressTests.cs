using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OpenPrismNode.Core.Commands.CreateWalletAddress;
using OpenPrismNode.Core.Entities;

public partial class IntegrationTests
{
    [Fact]
    public async Task CreateWalletAddress_Succeeds_For_New_Address()
    {
        // Arrange
        var walletAddress = "addr1axy0z7zza8gcgd9x3k0u8q7jpcqhtm3ld8s5qhjzz5khq3drdwk0vqcsdhjzjs6lyc0wsmf38yafw9k6em7uf92yg3wqtm5jgp";
        var request = new CreateWalletAddressRequest(walletAddress);

        // Act
        var result = await _createWalletAddressHandler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify the wallet address was created in the database
        var savedWalletAddress = await _context.WalletAddressEntities
            .FirstOrDefaultAsync(w => w.WalletAddress == walletAddress);
        savedWalletAddress.Should().NotBeNull();
        savedWalletAddress.WalletAddress.Should().Be(walletAddress);

        // Verify the wallet address is in the cache
        var cachedWalletAddress = await _walletAddressCache.GetOrAddAsync(walletAddress, () => Task.FromResult<WalletAddressEntity>(null));
        cachedWalletAddress.Should().NotBeNull();
        cachedWalletAddress.WalletAddress.Should().Be(walletAddress);
    }

    [Fact]
    public async Task CreateWalletAddress_Succeeds_For_Existing_Address()
    {
        // Arrange
        var walletAddress = "addr1qxy0z7zza8gcgd9x3k0u8q7jpcqhtm3ld8s5qhjzz5khq3drdwk0vqcsdhjzjs6lyc0wsmf38yafw9k6em7uf92yg3wqtm5jgp";
        var existingWalletAddressEntity = new WalletAddressEntity { WalletAddress = walletAddress };
        await _context.WalletAddressEntities.AddAsync(existingWalletAddressEntity);
        await _context.SaveChangesAsync();

        var request = new CreateWalletAddressRequest(walletAddress);

        // Act
        var result = await _createWalletAddressHandler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify no new wallet address was created in the database
        var walletAddressCount = await _context.WalletAddressEntities.CountAsync(w => w.WalletAddress == walletAddress);
        walletAddressCount.Should().Be(1);

        // Verify the wallet address is in the cache
        var cachedWalletAddress = await _walletAddressCache.GetOrAddAsync(walletAddress, () => Task.FromResult<WalletAddressEntity>(null));
        cachedWalletAddress.Should().NotBeNull();
        cachedWalletAddress.WalletAddress.Should().Be(walletAddress);
    }
}