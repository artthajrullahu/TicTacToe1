using Microsoft.AspNetCore.Mvc;
using TicTacToe.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using TicTacToeApi.Hubs;
using TicTacToeApi.Services;

namespace TicTacToeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IMongoDBService _mongoDBService;
    private readonly ILogger<GamesController> _logger;
    private readonly IHubContext<GameHub> _hubContext;

    public GamesController(IMongoDBService mongoDBService, ILogger<GamesController> logger, IHubContext<GameHub> hubContext)
    {
        _mongoDBService = mongoDBService;
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<ActionResult<GameState>> CreateGame([FromBody] CreateGameRequest request)
    {
        try
        {
            var game = new GameState
            {
                Id = Guid.NewGuid().ToString(),
                Board = new string[9],
                CurrentPlayer = Player.X,
                Status = GameStatus.Waiting,
                Players = new List<PlayerInfo>(),
                CreatedAt = DateTime.UtcNow
            };

            await _mongoDBService.CreateGameAsync(game);
            
            _logger.LogInformation($"Game created: {game.Id}");
            await _hubContext.Clients.All.SendAsync("NewGameCreated", game);
            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return StatusCode(500, "Failed to create game");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GameState>> GetGame(string id)
    {
        try
        {
            var game = await _mongoDBService.GetGameAsync(id);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            return Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game");
            return StatusCode(500, "Failed to get game");
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<GameState>>> GetActiveGames()
    {
        try
        {
            var games = await _mongoDBService.GetActiveGamesAsync();
            return Ok(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active games");
            return StatusCode(500, "Failed to get active games");
        }
    }

    [HttpPost("{id}/join")]
    public async Task<ActionResult<GameState>> JoinGame(string id, [FromBody] JoinGameRequest request)
    {
        try
        {
            var game = await _mongoDBService.GetGameAsync(id);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            if (game.Status == GameStatus.Finished)
            {
                return BadRequest("Game is finished");
            }

            if (game.Players.Count >= 2)
            {
                return BadRequest("Game is full");
            }

            // Check if player already exists
            var existingPlayer = game.Players.FirstOrDefault(p => p.Name == request.PlayerName);
            if (existingPlayer != null)
            {
                return Ok(game); // Player already in game
            }

            // Add new player
            var playerSymbol = game.Players.Count == 0 ? Player.X : Player.O;
            var newPlayer = new PlayerInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.PlayerName,
                Symbol = playerSymbol,
                IsConnected = false, // Will be set to true when they connect via SignalR
                JoinedAt = DateTime.UtcNow
            };

            game.Players.Add(newPlayer);

            // Update game status if we have 2 players
            if (game.Players.Count == 2)
            {
                game.Status = GameStatus.Playing;
            }

            await _mongoDBService.UpdateGameAsync(game);

            _logger.LogInformation($"Player {request.PlayerName} joined game {id}");
            return Ok(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            return StatusCode(500, "Failed to join game");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGame(string id)
    {
        try
        {
            var success = await _mongoDBService.DeleteGameAsync(id);
            if (!success)
            {
                return NotFound("Game not found");
            }

            _logger.LogInformation($"Game deleted: {id}");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game");
            return StatusCode(500, "Failed to delete game");
        }
    }

    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<List<GameSession>>> GetGameSessions(string id)
    {
        try
        {
            var sessions = await _mongoDBService.GetGameSessionsAsync(id);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game sessions");
            return StatusCode(500, "Failed to get game sessions");
        }
    }
}