namespace OpenPrismNode.Sync.Implementations.Blockfrost;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
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
    
    /// <summary>
    /// Gets the base address of the Blockfrost API.
    /// </summary>
    public Uri? BaseAddress => _httpClient?.BaseAddress;

    public BlockfrostApiClient(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings)
    {
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
        
        // Get the named HttpClient from the factory
        _httpClient = httpClientFactory.CreateClient("BlockfrostApi");
        _httpClient.BaseAddress = new Uri(blockfrostConfig.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        // Make sure we don't have duplicate headers
        if (_httpClient.DefaultRequestHeaders.Contains("Project_id"))
        {
            _httpClient.DefaultRequestHeaders.Remove("Project_id");
        }
        
        _httpClient.DefaultRequestHeaders.Add("Project_id", _apiKey);
        
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
                string headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                return Result.Fail($"Blockfrost API request failed with status {response.StatusCode}. Headers: {headers}. Content: {errorContent}");
            }

            var ff = await response.Content.ReadAsStringAsync();
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