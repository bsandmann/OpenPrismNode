namespace OpenPrismNode.Core.Services;

using Entities;

public interface IWalletAddressCache
{
    Task<WalletAddressEntity?> GetOrAddAsync(string walletAddressString, Func<Task<WalletAddressEntity?>> factory);
    Task SetAsync(string walletAddressString, WalletAddressEntity walletAddress);
}