using System.ComponentModel.DataAnnotations;

namespace TicTacToe.Shared.Models;

public enum Player
{
    X,
    O
}

public enum GameStatus
{
    Waiting,
    Playing,
    Finished,
    Abandoned,
    Aborted
}

public class GameState
{
    public string Id { get; set; } = string.Empty;
    public string[] Board { get; set; } = new string[9];
    public Player CurrentPlayer { get; set; } = Player.X;
    public Player? Winner { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public List<PlayerInfo> Players { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMoveAt { get; set; }
    public bool IsDraw => Board.All(cell => !string.IsNullOrEmpty(cell)) && Winner == null;
}

public class PlayerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public Player? Symbol { get; set; }
    public bool IsConnected { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class GameMove
{
    public string GameId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public int Position { get; set; }
    public Player Player { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CreateGameRequest
{
    [Required]
    [StringLength(50)]
    public string PlayerName { get; set; } = string.Empty;
}

public class JoinGameRequest
{
    [Required]
    public string GameId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string PlayerName { get; set; } = string.Empty;
}

public class MakeMoveRequest
{
    [Required]
    public string GameId { get; set; } = string.Empty;
    
    [Required]
    public string PlayerId { get; set; } = string.Empty;
    
    [Range(0, 8)]
    public int Position { get; set; }
}

public class GameSession
{
    public string SessionId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}