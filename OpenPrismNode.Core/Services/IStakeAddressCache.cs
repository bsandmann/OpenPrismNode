namespace OpenPrismNode.Core.Services;

using Entities;

public interface IStakeAddressCache
{
    Task<StakeAddressEntity?> GetOrAddAsync(string stakeAddressString, Func<Task<StakeAddressEntity?>> factory);
    Task SetAsync(string stakeAddressString, StakeAddressEntity stakeAddress);
}