using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;

namespace OpenPrismNode.Core.Services;

using Models.CardanoWallet;

public class CardanoWalletService : ICardanoWalletService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly Random _random = new Random();
    private static readonly string _chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz123456789";


    public CardanoWalletService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("CardanoWalletApi");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<Result<CreateWalletResponse>> CreateWalletAsync(CreateWalletRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v2/wallets", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var walletResponse = JsonSerializer.Deserialize<CreateWalletResponse>(responseContent, _jsonOptions);
                return Result.Ok(walletResponse);
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return Result.Fail("Wallet already exists. Use the walletId for authorization.");
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<Result<CreateWalletResponse>> GetWalletAsync(string walletId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/wallets/{walletId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var walletResponse = JsonSerializer.Deserialize<CreateWalletResponse>(responseContent, _jsonOptions);
                return Result.Ok(walletResponse);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<Result<List<AddressResponse>>> ListAddressesAsync(string walletId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v2/wallets/{walletId}/addresses");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var addresses = JsonSerializer.Deserialize<List<AddressResponse>>(responseContent, _jsonOptions);
                return Result.Ok(addresses);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<Result<TransactionConstructResponse>> ConstructTransactionAsync(string walletId, TransactionConstructRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v2/wallets/{walletId}/transactions-construct", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var txResponse = JsonSerializer.Deserialize<TransactionConstructResponse>(responseContent, _jsonOptions);
                return Result.Ok(txResponse);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<Result<string>> SignTransactionAsync(string walletId, TransactionSignRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v2/wallets/{walletId}/transactions-sign", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var signResponse = JsonSerializer.Deserialize<TransactionSignResponse>(responseContent, _jsonOptions);
                return Result.Ok(signResponse.Transaction);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    private async Task<Result<string>> SubmitTransactionAsync(string walletId, TransactionSubmitRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v2/wallets/{walletId}/transactions-submit", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var submitResponse = JsonSerializer.Deserialize<TransactionSubmitResponse>(responseContent, _jsonOptions);
                return Result.Ok(submitResponse.Id);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<Result<string>> CreateAndSubmitTransactionAsync(string walletId, string passphrase, Payment payment, object? metadata)
    {
        // Step 1: Construct the transaction
        var constructRequest = new TransactionConstructRequest
        {
            Payments = new List<Payment> { payment },
            Metadata = metadata,
            Encoding = "base64"
        };

        if (metadata is null)
        {
            // We have a withdrawal transaction, so we need accomodate for the fee for a full withdrawal
            // TODO - This is a temporary solution. Is there a better way to calculate the fee?
            var amount = payment.Amount.Quantity - 200_000;
            if (amount <= 0)
            {
                return Result.Fail("Insufficient funds for withdrawal.");
            }

            constructRequest = new TransactionConstructRequest
            {
                Payments = new List<Payment> { new Payment { Address = payment.Address, Amount = new Amount { Quantity = amount, Unit = "lovelace" } } },
                Encoding = "base64"
            };
        }

        var constructResult = await ConstructTransactionAsync(walletId, constructRequest);

        if (constructResult.IsFailed && metadata is not null)
        {
            return Result.Fail(constructResult.Errors);
        }


        // Step 2: Sign the transaction
        var signRequest = new TransactionSignRequest
        {
            Passphrase = passphrase,
            Transaction = constructResult.Value.Transaction,
            Encoding = "base64"
        };

        var signResult = await SignTransactionAsync(walletId, signRequest);

        if (signResult.IsFailed)
        {
            return Result.Fail(signResult.Errors);
        }

        // Step 3: Submit the transaction
        var submitRequest = new TransactionSubmitRequest
        {
            Transaction = signResult.Value
        };

        var submitResult = await SubmitTransactionAsync(walletId, submitRequest);

        if (submitResult.IsFailed)
        {
            return Result.Fail(submitResult.Errors);
        }

        return Result.Ok(submitResult.Value);
    }

    private async Task<List<Error>> HandleErrorResponse(HttpResponseMessage response)
    {
        var errors = new List<Error>();
        var responseContent = await response.Content.ReadAsStringAsync();

        try
        {
            // Try to parse the error response from the API
            var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(responseContent, _jsonOptions);

            if (apiError != null && !string.IsNullOrWhiteSpace(apiError.Message))
            {
                errors.Add(new Error(apiError.Message));
            }
            else
            {
                errors.Add(new Error("An error occurred but no message was provided by the API."));
            }
        }
        catch (JsonException)
        {
            // If parsing fails, add the raw response content
            errors.Add(new Error($"API returned an error: {responseContent}"));
        }

        // Add specific errors based on status code
        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
                errors.Add(new Error("Bad request. Please check your input parameters."));
                break;
            case HttpStatusCode.Unauthorized:
                errors.Add(new Error("Unauthorized. Please check your credentials."));
                break;
            case HttpStatusCode.Forbidden:
                errors.Add(new Error("Forbidden. You do not have permission to perform this action."));
                break;
            case HttpStatusCode.NotFound:
                errors.Add(new Error("Resource not found. Please check the provided IDs."));
                break;
            case HttpStatusCode.Conflict:
                errors.Add(new Error("Conflict. The resource already exists."));
                break;
            case HttpStatusCode.InternalServerError:
                errors.Add(new Error("Internal server error. Please try again later."));
                break;
            default:
                errors.Add(new Error($"HTTP Error: {response.StatusCode}"));
                break;
        }

        return errors;
    }

    public async Task<Result<TransactionDetailsResponse>> GetTransactionDetailsAsync(string walletId, string transactionId)
    {
        try
        {
            var endpoint = $"/v2/wallets/{walletId}/transactions/{transactionId}";
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var transactionDetails = JsonSerializer.Deserialize<TransactionDetailsResponse>(responseContent, _jsonOptions);
                return Result.Ok(transactionDetails);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<Result<NetworkInformationResponse>> GetNetworkInformationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v2/network/information");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var networkInfo = JsonSerializer.Deserialize<NetworkInformationResponse>(responseContent, _jsonOptions);
                return Result.Ok(networkInfo);
            }
            else
            {
                var errorResult = await HandleErrorResponse(response);
                return Result.Fail(errorResult);
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred: {ex.Message}");
        }
    }

    public string GeneratePassphrase(int length = 24)
    {
        return new string(Enumerable.Repeat(_chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public string GenerateMnemonic()
    {
        var nms = new CardanoSharp.Wallet.MnemonicService();
        return nms.Generate(24).Words;
    }
}