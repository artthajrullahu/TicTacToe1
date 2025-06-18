using Microsoft.AspNetCore.SignalR;
using TicTacToe.Shared.Models;
using TicTacToeApi.Services;

namespace TicTacToeApi.Hubs;

public class GameHub : Hub
{
    private readonly IMongoDBService _mongoDBService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IMongoDBService mongoDBService, ILogger<GameHub> logger)
    {
        _mongoDBService = mongoDBService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await HandlePlayerDisconnection();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGame(string gameId, string playerName)
    {
        _logger.LogInformation($"JoinGame called: gameId={gameId}, playerName={playerName}, connectionId={Context.ConnectionId}");
        try
        {
            var game = await _mongoDBService.GetGameAsync(gameId);
            if (game == null)
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            // Check if game is full
            if (game.Players.Count >= 2)
            {
                await Clients.Caller.SendAsync("Error", "Game is full");
                return;
            }

            // Check if player already exists
            var existingPlayer = game.Players.FirstOrDefault(p => p.Name == playerName);
            if (existingPlayer != null)
            {
                // Player reconnecting
                existingPlayer.ConnectionId = Context.ConnectionId;
                existingPlayer.IsConnected = true;
                await _mongoDBService.UpdateGameAsync(game);
                
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
                await Clients.Caller.SendAsync("PlayerReconnected", existingPlayer);
                await Clients.Group(gameId).SendAsync("GameStateUpdated", game);
                return;
            }

            // Add new player
            var playerSymbol = game.Players.Count == 0 ? Player.X : Player.O;
            var newPlayer = new PlayerInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = playerName,
                ConnectionId = Context.ConnectionId,
                Symbol = playerSymbol,
                IsConnected = true,
                JoinedAt = DateTime.UtcNow
            };

            game.Players.Add(newPlayer);

            // Create session
            var session = new GameSession
            {
                SessionId = Guid.NewGuid().ToString(),
                GameId = gameId,
                PlayerId = newPlayer.Id,
                PlayerName = playerName,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _mongoDBService.CreateSessionAsync(session);

            // Update game status if we have 2 players
            if (game.Players.Count == 2)
            {
                game.Status = GameStatus.Playing;
            }

            await _mongoDBService.UpdateGameAsync(game);

            // Join the game group
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            // Notify all players in the game
            await Clients.Group(gameId).SendAsync("PlayerJoined", newPlayer);
            await Clients.Group(gameId).SendAsync("GameStateUpdated", game);

            if (game.Status == GameStatus.Playing)
            {
                await Clients.Group(gameId).SendAsync("GameStarted", game);
            }

            _logger.LogInformation($"Player {playerName} joined game {gameId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            await Clients.Caller.SendAsync("Error", "Failed to join game");
        }
    }

    public async Task LeaveGame(string gameId)
    {
        _logger.LogInformation($"LeaveGame called: gameId={gameId}, connectionId={Context.ConnectionId}");
        try
        {
            var game = await _mongoDBService.GetGameAsync(gameId);
            if (game == null) return;

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player != null)
            {
                // Remove the player from the game
                game.Players.Remove(player);

                // Update game status based on remaining players
                if (game.Players.Count == 0)
                {
                    game.Status = GameStatus.Finished;
                    game.Board = new string[9];  // Reset board
                }
                else
                {
                    game.Status = GameStatus.Waiting;
                }

                await _mongoDBService.UpdateGameAsync(game);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
                await Clients.Group(gameId).SendAsync("PlayerLeft", player.Id);
                await Clients.Group(gameId).SendAsync("GameStateUpdated", game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving game");
        }
    }

    public async Task MakeMove(string gameId, string playerId, int position)
    {
        _logger.LogInformation($"MakeMove called: gameId={gameId}, position={position}, connectionId={Context.ConnectionId}");
        try
        {
            var game = await _mongoDBService.GetGameAsync(gameId);
            if (game == null)
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            if (game.Status != GameStatus.Playing)
            {
                await Clients.Caller.SendAsync("Error", "Game is not active");
                return;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found");
                return;
            }

            if (player.Symbol != game.CurrentPlayer)
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (position < 0 || position >= 9 || !string.IsNullOrEmpty(game.Board[position]))
            {
                await Clients.Caller.SendAsync("Error", "Invalid move");
                return;
            }

            // Make the move
            game.Board[position] = ((Player)player.Symbol).ToString();
            game.LastMoveAt = DateTime.UtcNow;

            // Check for winner
            var winner = CheckWinner(game.Board);
            if (winner != null)
            {
                game.Winner = winner;
                game.Status = GameStatus.Finished;
            }
            else if (game.Board.All(cell => !string.IsNullOrEmpty(cell)))
            {
                // Draw
                game.Status = GameStatus.Finished;
            }
            else
            {
                // Switch turns
                game.CurrentPlayer = game.CurrentPlayer == Player.X ? Player.O : Player.X;
            }

            await _mongoDBService.UpdateGameAsync(game);

            // Notify all players
            var move = new GameMove
            {
                GameId = gameId,
                PlayerId = player.Id,
                Position = position,
                Player = player.Symbol ?? Player.X,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group(gameId).SendAsync("MoveMade", move);
            await Clients.Group(gameId).SendAsync("GameStateUpdated", game);

            if (game.Status == GameStatus.Finished)
            {
                await Clients.Group(gameId).SendAsync("GameEnded", game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
            await Clients.Caller.SendAsync("Error", "Failed to make move");
        }
    }

    public async Task RestartGame(string gameId)
    {
        _logger.LogInformation($"RestartGame called: gameId={gameId}, connectionId={Context.ConnectionId}");
        try
        {
            var game = await _mongoDBService.GetGameAsync(gameId);
            if (game == null)
            {
                await Clients.Caller.SendAsync("Error", "Game not found");
                return;
            }

            // Reset game state
            game.Board = new string[9];
            game.CurrentPlayer = Player.X;
            game.Winner = null;
            game.Status = game.Players.Count == 2 ? GameStatus.Playing : GameStatus.Waiting;
            game.LastMoveAt = null;

            await _mongoDBService.UpdateGameAsync(game);

            await Clients.Group(gameId).SendAsync("GameStateUpdated", game);
            await Clients.Group(gameId).SendAsync("GameStarted", game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting game");
            await Clients.Caller.SendAsync("Error", "Failed to restart game");
        }
    }

    private async Task HandlePlayerDisconnection()
    {
        try
        {
            var games = await _mongoDBService.GetGamesByConnectionIdAsync(Context.ConnectionId);
            foreach (var game in games)
            {
                var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    player.IsConnected = false;
                    await _mongoDBService.UpdateGameAsync(game);

                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, game.Id);
                    await Clients.Group(game.Id).SendAsync("PlayerLeft", player.Id);
                    await Clients.Group(game.Id).SendAsync("GameStateUpdated", game);

                    // If both players are disconnected, mark the game as abandoned
                    if (game.Players.All(p => !p.IsConnected))
                    {
                        game.Status = GameStatus.Abandoned;
                        await _mongoDBService.UpdateGameAsync(game);
                        await Clients.Group(game.Id).SendAsync("GameStateUpdated", game);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling player disconnection");
        }
    }

    private static Player? CheckWinner(string[] board)
    {
        // Winning combinations
        var lines = new[]
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // columns
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 } // diagonals
        };

        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(board[line[0]]) &&
                board[line[0]] == board[line[1]] &&
                board[line[0]] == board[line[2]])
            {
                return Enum.Parse<Player>(board[line[0]]);
            }
        }

        return null;
    }
}