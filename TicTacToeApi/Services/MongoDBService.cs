using MongoDB.Driver;
using TicTacToe.Shared.Models;

namespace TicTacToeApi.Services;

public interface IMongoDBService
{
    Task<GameState> CreateGameAsync(GameState game);
    Task<GameState?> GetGameAsync(string gameId);
    Task<bool> UpdateGameAsync(GameState game);
    Task<bool> DeleteGameAsync(string gameId);
    Task<List<GameState>> GetActiveGamesAsync();
    Task<List<GameSession>> GetGameSessionsAsync(string gameId);
    Task<GameSession> CreateSessionAsync(GameSession session);
    Task<bool> UpdateSessionAsync(GameSession session);
    Task<bool> IsConnectedAsync();
    Task<List<GameState>> GetGamesByConnectionIdAsync(string connectionId);
}

public class MongoDBService : IMongoDBService
{
    private readonly IMongoCollection<GameState> _gamesCollection;
    private readonly IMongoCollection<GameSession> _sessionsCollection;
    private readonly ILogger<MongoDBService> _logger;
    private readonly MongoClient _client;

    public MongoDBService(IConfiguration configuration, ILogger<MongoDBService> logger)
    {
        _logger = logger;
        try
        {
            var connectionString = configuration.GetConnectionString("MongoDB");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured");
            }

            _client = new MongoClient(connectionString);
            var database = _client.GetDatabase("TicTacToe");
            
            _gamesCollection = database.GetCollection<GameState>("Games");
            _sessionsCollection = database.GetCollection<GameSession>("Sessions");
            
            // Create indexes for better performance
            CreateIndexes();
            
            _logger.LogInformation("MongoDB service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB service");
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            await _client.ListDatabaseNamesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB");
            return false;
        }
    }

    private void CreateIndexes()
    {
        try
        {
            var gameIndexBuilder = Builders<GameState>.IndexKeys;
            var gameIndexModel = new CreateIndexModel<GameState>(
                gameIndexBuilder.Ascending(g => g.Status)
            );
            _gamesCollection.Indexes.CreateOne(gameIndexModel);

            var sessionIndexBuilder = Builders<GameSession>.IndexKeys;
            var sessionIndexModel = new CreateIndexModel<GameSession>(
                sessionIndexBuilder.Ascending(s => s.GameId)
            );
            _sessionsCollection.Indexes.CreateOne(sessionIndexModel);
            
            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
            // Don't throw here, indexes are not critical for functionality
        }
    }

    public async Task<GameState> CreateGameAsync(GameState game)
    {
        try
        {
            await _gamesCollection.InsertOneAsync(game);
            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create game in MongoDB");
            throw;
        }
    }

    public async Task<GameState?> GetGameAsync(string gameId)
    {
        try
        {
            var filter = Builders<GameState>.Filter.Eq(g => g.Id, gameId);
            return await _gamesCollection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game from MongoDB");
            throw;
        }
    }

    public async Task<bool> UpdateGameAsync(GameState game)
    {
        try
        {
            var filter = Builders<GameState>.Filter.Eq(g => g.Id, game.Id);
            var result = await _gamesCollection.ReplaceOneAsync(filter, game);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update game in MongoDB");
            throw;
        }
    }

    public async Task<bool> DeleteGameAsync(string gameId)
    {
        try
        {
            var filter = Builders<GameState>.Filter.Eq(g => g.Id, gameId);
            var result = await _gamesCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete game from MongoDB");
            throw;
        }
    }

    public async Task<List<GameState>> GetActiveGamesAsync()
    {
        try
        {
            var filter = Builders<GameState>.Filter.Ne(g => g.Status, GameStatus.Finished);
            return await _gamesCollection.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active games from MongoDB");
            throw;
        }
    }

    public async Task<List<GameSession>> GetGameSessionsAsync(string gameId)
    {
        try
        {
            var filter = Builders<GameSession>.Filter.Eq(s => s.GameId, gameId);
            return await _sessionsCollection.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game sessions from MongoDB");
            throw;
        }
    }

    public async Task<GameSession> CreateSessionAsync(GameSession session)
    {
        try
        {
            await _sessionsCollection.InsertOneAsync(session);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session in MongoDB");
            throw;
        }
    }

    public async Task<bool> UpdateSessionAsync(GameSession session)
    {
        try
        {
            var filter = Builders<GameSession>.Filter.Eq(s => s.SessionId, session.SessionId);
            var result = await _sessionsCollection.ReplaceOneAsync(filter, session);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session in MongoDB");
            throw;
        }
    }

    public async Task<List<GameState>> GetGamesByConnectionIdAsync(string connectionId)
    {
        var filter = Builders<GameState>.Filter.ElemMatch(g => g.Players, p => p.ConnectionId == connectionId);
        return await _gamesCollection.Find(filter).ToListAsync();
    }
} 