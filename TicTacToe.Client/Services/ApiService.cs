using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TicTacToe.Shared.Models;

namespace TicTacToe.Client.Services;

public interface IApiService
{
    Task<GameState> CreateGameAsync(CreateGameRequest request);
    Task<GameState?> GetGameAsync(string gameId);
    Task<List<GameState>> GetActiveGamesAsync();
    Task<GameState> JoinGameAsync(string gameId, JoinGameRequest request);
    Task DeleteGameAsync(string gameId);
    Task<List<GameSession>> GetGameSessionsAsync(string gameId);
}

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;
    private readonly string _baseUrl;

    public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IOptions<ApiServiceOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = options.Value.BaseUrl;
    }

    public async Task<GameState> CreateGameAsync(CreateGameRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/games", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameState>() ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create game");
            throw;
        }
    }

    public async Task<GameState?> GetGameAsync(string gameId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/games/{gameId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GameState>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<GameState>> GetActiveGamesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/games");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<GameState>>() ?? new List<GameState>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active games");
            throw;
        }
    }

    public async Task<GameState> JoinGameAsync(string gameId, JoinGameRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/games/{gameId}/join", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GameState>() ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join game {GameId}", gameId);
            throw;
        }
    }

    public async Task DeleteGameAsync(string gameId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/games/{gameId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete game {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<GameSession>> GetGameSessionsAsync(string gameId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/games/{gameId}/sessions");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<GameSession>>() ?? new List<GameSession>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game sessions for {GameId}", gameId);
            throw;
        }
    }
} 