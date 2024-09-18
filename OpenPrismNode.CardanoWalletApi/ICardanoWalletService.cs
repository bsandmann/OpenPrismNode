using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;

public interface ICardanoWalletService
{
    public Task<Result<CreateWalletResponse>> CreateWalletAsync(CreateWalletRequest request);

    public Task<Result<CreateWalletResponse>> GetWalletAsync(string walletId);

    public Task<Result<List<AddressResponse>>> ListAddressesAsync(string walletId);

    public Task<Result<string>> CreateAndSubmitTransactionAsync(string walletId, string passphrase, Payment payment, object metadata);

    public Task<Result<TransactionDetailsResponse>> GetTransactionDetailsAsync(string walletId, string transactionId);

    public Task<Result<NetworkInformationResponse>> GetNetworkInformationAsync();

}

