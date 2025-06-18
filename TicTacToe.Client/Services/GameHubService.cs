using Microsoft.AspNetCore.SignalR.Client;
using TicTacToe.Shared.Models;

namespace TicTacToe.Client.Services;

public interface IGameHubService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task JoinGameAsync(string gameId, string playerName);
    Task LeaveGameAsync(string gameId);
    Task MakeMoveAsync(string gameId, string playerId, int position);
    Task RestartGameAsync(string gameId);
    
    event Action<GameState>? GameStateUpdated;
    event Action<PlayerInfo>? PlayerJoined;
    event Action<string>? PlayerLeft;
    event Action<GameState>? GameStarted;
    event Action<GameState>? GameEnded;
    event Action<GameMove>? MoveMade;
    event Action<PlayerInfo>? PlayerReconnected;
    event Action<string>? Error;
}

public class GameHubService : IGameHubService, IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<GameHubService> _logger;
    private readonly string _hubUrl;

    public event Action<GameState>? GameStateUpdated;
    public event Action<PlayerInfo>? PlayerJoined;
    public event Action<string>? PlayerLeft;
    public event Action<GameState>? GameStarted;
    public event Action<GameState>? GameEnded;
    public event Action<GameMove>? MoveMade;
    public event Action<PlayerInfo>? PlayerReconnected;
    public event Action<string>? Error;

    public GameHubService(IConfiguration configuration, ILogger<GameHubService> logger)
    {
        _logger = logger;
        _hubUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7001";
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_hubUrl}/gamehub")
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        SetupEventHandlers();

        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task JoinGameAsync(string gameId, string playerName)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to hub");

        await _hubConnection.InvokeAsync("JoinGame", gameId, playerName);
    }

    public async Task LeaveGameAsync(string gameId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync("LeaveGame", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving game");
        }
    }

    public async Task MakeMoveAsync(string gameId, string playerId, int position)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to hub");

        await _hubConnection.InvokeAsync("MakeMove", gameId, playerId, position);
    }

    public async Task RestartGameAsync(string gameId)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to hub");

        await _hubConnection.InvokeAsync("RestartGame", gameId);
    }

    private void SetupEventHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<GameState>("GameStateUpdated", (gameState) =>
        {
            _logger.LogInformation($"[SignalR] GameStateUpdated received: GameId={gameState.Id}");
            GameStateUpdated?.Invoke(gameState);
        });

        _hubConnection.On<PlayerInfo>("PlayerJoined", (player) =>
        {
            _logger.LogInformation($"[SignalR] PlayerJoined received: PlayerId={player.Id}, Name={player.Name}");
            PlayerJoined?.Invoke(player);
        });

        _hubConnection.On<string>("PlayerLeft", (playerId) =>
        {
            _logger.LogInformation($"[SignalR] PlayerLeft received: PlayerId={playerId}");
            PlayerLeft?.Invoke(playerId);
        });

        _hubConnection.On<GameState>("GameStarted", (gameState) =>
        {
            _logger.LogInformation($"[SignalR] GameStarted received: GameId={gameState.Id}");
            GameStarted?.Invoke(gameState);
        });

        _hubConnection.On<GameState>("GameEnded", (gameState) =>
        {
            _logger.LogInformation($"[SignalR] GameEnded received: GameId={gameState.Id}");
            GameEnded?.Invoke(gameState);
        });

        _hubConnection.On<GameMove>("MoveMade", (move) =>
        {
            _logger.LogInformation($"[SignalR] MoveMade received: GameId={move.GameId}, Position={move.Position}, Player={move.Player}");
            MoveMade?.Invoke(move);
        });

        _hubConnection.On<PlayerInfo>("PlayerReconnected", (player) =>
        {
            _logger.LogInformation($"[SignalR] PlayerReconnected received: PlayerId={player.Id}, Name={player.Name}");
            PlayerReconnected?.Invoke(player);
        });

        _hubConnection.On<string>("Error", (error) =>
        {
            _logger.LogError($"[SignalR] Error received: {error}");
            Error?.Invoke(error);
        });

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
            await Task.Delay(new Random().Next(0, 5) * 1000);
            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect after connection closed");
            }
        };

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogInformation("SignalR reconnecting: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with new connection ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}