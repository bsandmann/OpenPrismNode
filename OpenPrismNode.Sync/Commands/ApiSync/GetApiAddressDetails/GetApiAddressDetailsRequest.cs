namespace OpenPrismNode.Sync.Commands.ApiSync.GetApiAddressDetails;

using FluentResults;
using MediatR;

/// <summary>
/// Request to retrieve address details by its Bech32 address from the Blockfrost API.
/// </summary>
public class GetApiAddressDetailsRequest : IRequest<Result<ApiResponseAddress>>
{
    /// <summary>
    /// The Bech32 address to retrieve.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetApiAddressDetailsRequest"/> class.
    /// </summary>
    /// <param name="address">The Bech32 address.</param>
    public GetApiAddressDetailsRequest(string address)
    {
        Address = address;
    }
}