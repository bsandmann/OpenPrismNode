namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Options;
using OpenPrismNode.Core.Common;

/// <summary>
/// Client for the Blockfrost API that handles HTTP requests and authentication.
/// </summary>
public class BlockfrostApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public BlockfrostApiClient(HttpClient httpClient, IOptions<AppSettings> appSettings)
    {
        _httpClient = httpClient;
        var blockfrostConfig = appSettings.Value.Blockfrost;
        
        if (string.IsNullOrEmpty(blockfrostConfig?.BaseUrl))
        {
            throw new ArgumentException("Blockfrost BaseUrl is not configured in AppSettings");
        }
        
        if (string.IsNullOrEmpty(blockfrostConfig?.ApiKey))
        {
            throw new ArgumentException("Blockfrost ApiKey is not configured in AppSettings");
        }
        
        _apiKey = blockfrostConfig.ApiKey;
        _httpClient.BaseAddress = new Uri(blockfrostConfig.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("project_id", _apiKey);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Performs a GET request to the Blockfrost API and deserializes the response
    /// </summary>
    public async Task<Result<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Fail($"Blockfrost API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return result != null 
                ? Result.Ok(result) 
                : Result.Fail<T>("Could not deserialize response from Blockfrost API");
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail<T>($"HTTP request error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Result.Fail<T>($"JSON deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail<T>($"Unexpected error calling Blockfrost API: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a GET request to the Blockfrost API that returns a list and deserializes the response
    /// </summary>
    public async Task<Result<List<T>>> GetListAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Fail<List<T>>($"Blockfrost API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<T>>(_jsonOptions, cancellationToken);
            return result != null 
                ? Result.Ok(result) 
                : Result.Fail<List<T>>("Could not deserialize response from Blockfrost API");
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail<List<T>>($"HTTP request error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Result.Fail<List<T>>($"JSON deserialization error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail<List<T>>($"Unexpected error calling Blockfrost API: {ex.Message}");
        }
    }
}