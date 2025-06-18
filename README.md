# Tic Tac Toe - Distributed Systems Project

**Real-Time Tic Tac Toe Multiplayer with .NET Web API, SignalR, and MongoDB**

**Author:** Art Hajrullahu  
**Student ID:** 130976

## Project Overview

This project implements a real-time, distributed multiplayer Tic Tac Toe game using modern .NET technologies. It demonstrates key distributed systems concepts including autonomous agents, shared state distribution, fault tolerance, and real-time communication.

## System Architecture

The project consists of three main components:

1. **TicTacToeApi** - ASP.NET Core Web API with SignalR Hub
2. **TicTacToe.Shared** - Shared class library for models
3. **TicTacToe.Client** - Blazor WebAssembly frontend

### Distributed System Characteristics

- **Multiple Autonomous Agents**: Two players compete over a shared resource (game board)
- **Shared State Distribution**: Game state is synchronized across client nodes via SignalR and stored in MongoDB
- **Fault Tolerance**: Handles client disconnections gracefully with automatic reconnection
- **Real-time Communication**: Instant updates using SignalR hubs
- **Persistent Storage**: MongoDB for game session and player data persistence

## Technologies Used

- **Backend**: ASP.NET Core 9.0 Web API
- **Real-time Communication**: SignalR
- **Database**: MongoDB
- **Frontend**: Blazor WebAssembly
- **Language**: C#
- **Architecture**: Distributed Systems with Microservices

## Features

- ✅ Real-time multiplayer gameplay
- ✅ Player registration and game session management
- ✅ Automatic reconnection handling
- ✅ Game state persistence in MongoDB
- ✅ Server-side move validation
- ✅ Win/draw detection
- ✅ Game restart functionality
- ✅ Connection status monitoring
- ✅ Fault tolerance and error handling

## Prerequisites

- .NET 9.0 SDK
- MongoDB (local installation or MongoDB Atlas)
- Visual Studio 2022 or VS Code

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd project
```

### 2. Configure MongoDB

#### Option A: Local MongoDB
1. Install MongoDB Community Server
2. Start MongoDB service
3. The API will connect to `mongodb://localhost:27017` by default

#### Option B: MongoDB Atlas
1. Create a MongoDB Atlas account
2. Create a cluster and get your connection string
3. Update the connection string in `TicTacToeApi/appsettings.json`

### 3. Configure API Settings

Update `TicTacToeApi/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 4. Build and Run

```bash
# Build the entire solution
dotnet build

# Run the API (from TicTacToeApi directory)
cd TicTacToeApi
dotnet run

# Run the Blazor client (from TicTacToe.Client directory)
cd ../TicTacToe.Client
dotnet run
```

The API will be available at `https://localhost:7001` and the Blazor client at `https://localhost:5001`.

## API Endpoints

### Games Controller
- `POST /api/games` - Create a new game
- `GET /api/games` - Get all active games
- `GET /api/games/{id}` - Get specific game
- `POST /api/games/{id}/join` - Join a game
- `DELETE /api/games/{id}` - Delete a game
- `GET /api/games/{id}/sessions` - Get game sessions

### SignalR Hub
- `/gamehub` - Real-time game communication

## How to Play

1. **Create a Game**: Enter your name and click "Create Game"
2. **Join a Game**: Enter a game ID and your name, or select from active games
3. **Play**: Take turns making moves on the 3x3 grid
4. **Win**: Get three of your symbols in a row (horizontal, vertical, or diagonal)
5. **Replay**: Click "Play Again" to restart the game

## Distributed Systems Features

### Fault Tolerance
- Automatic SignalR reconnection
- MongoDB persistence ensures game state survives server restarts
- Graceful handling of client disconnections

### Scalability
- Stateless API design
- MongoDB for horizontal scaling
- SignalR supports multiple concurrent games

### Real-time Communication
- Instant game state updates
- Live player status indicators
- Real-time move validation

### Data Consistency
- Server-side game logic validation
- MongoDB transactions for data integrity
- Optimistic concurrency control

## Project Structure

```
project/
├── TicTacToeApi/                 # ASP.NET Core Web API
│   ├── Controllers/              # REST API controllers
│   ├── Hubs/                    # SignalR hubs
│   ├── Services/                # Business logic services
│   └── Program.cs               # Application configuration
├── TicTacToe.Shared/            # Shared models library
│   └── Models/                  # Game models and DTOs
├── TicTacToe.Client/            # Blazor WebAssembly client
│   ├── Components/              # Blazor components
│   ├── Services/                # Client-side services
│   └── Program.cs               # Client configuration
└── README.md                    # This file
```

## Development

### Adding New Features
1. Update models in `TicTacToe.Shared`
2. Implement API endpoints in `TicTacToeApi`
3. Add SignalR hub methods if real-time updates needed
4. Update Blazor components in `TicTacToe.Client`

### Testing
- API endpoints can be tested using Swagger UI at `/swagger`
- SignalR hub can be tested using browser developer tools
- MongoDB data can be inspected using MongoDB Compass

## Troubleshooting

### Common Issues

1. **MongoDB Connection Error**
   - Ensure MongoDB is running
   - Check connection string in `appsettings.json`
   - Verify network connectivity

2. **SignalR Connection Issues**
   - Check CORS configuration
   - Verify hub URL in client configuration
   - Check browser console for errors

3. **Build Errors**
   - Ensure .NET 9.0 SDK is installed
   - Run `dotnet restore` to restore packages
   - Check all project references are correct

## Conclusion

This project successfully demonstrates distributed systems concepts through a real-time multiplayer game. It showcases:

- **Autonomous Agents**: Independent players with their own decision-making
- **Shared State**: Synchronized game state across multiple clients
- **Fault Tolerance**: Robust error handling and recovery mechanisms
- **Real-time Communication**: Instant updates using modern web technologies
- **Scalability**: Architecture that can handle multiple concurrent games

The implementation serves as an excellent example of building reliable, interactive multiplayer applications using modern .NET technologies and distributed systems principles.