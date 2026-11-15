using BoredGames.API.Models;
using BoredGames.Common.Enums;
using BoredGames.Common.Exceptions;
using BoredGames.Server.GameServer.Commands;
using BoredGames.Server.GameServer.Grains.Base;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace BoredGames.API.Hubs;

public class GameHub : Hub
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<GameHub> _logger;
    
    public GameHub(IGrainFactory grainFactory, ILogger<GameHub> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }
    
    public async Task SubscribeToGameStateChange(string gameId, string playerId)
    {
        var parsed = Guid.TryParse(gameId, out var gameIdAsGuid);
        if (!parsed)
        {
            _logger.LogError($"Invalid gameId. Can't parse gameId {gameId}");
        }
        parsed = Guid.TryParse(playerId, out var playerIdAsGuid);
        if (!parsed)
        {
            _logger.LogError($"Invalid playerId. Can't parse playerId {playerId}");
        }
        
        var game = _grainFactory.GetGrain<IGameGrain>(gameIdAsGuid);
        var gameState = await game.GetState();

        if (gameState.Players.Any())
        {
            _logger.LogInformation(
                $"Players {gameState.Players.Count}: {string.Join(",", gameState.Players.Select(x => x.Id))}");
        }
        else
        {
            _logger.LogInformation($"Players {gameState.Players.Count}");
        }

        if (gameState.Players.All(x => x.Id != playerIdAsGuid))
        {
            throw new InvalidOperationException($"Player with {playerIdAsGuid} not found in the game session.");
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await Clients.Group(gameId).SendAsync("OnPlayerSubscribed", playerId);
        _logger.LogInformation($"Player {playerId} subscribed to the game {gameId}");
        
        await Clients
            .Group(gameId)
            .SendAsync("OnGameStateReceived", JsonConvert.SerializeObject(gameState));
    }
    
    public async Task MakeMove(string payload)
    {
        var makeMove = JsonConvert.DeserializeObject<MakeMove>(payload);
        if (makeMove == null)
        {
            throw new Exception("Invalid payload.");
        }
        var parsed = Guid.TryParse(makeMove.GameId, out var gameIdAsGuid);
        if (!parsed)
        {
            _logger.LogError($"Invalid gameId. Can't parse gameId {makeMove.GameId}");
        }
        parsed = Guid.TryParse(makeMove.PlayerId, out var playerIdAsGuid);
        if (!parsed)
        {
            _logger.LogError($"Invalid playerId. Can't parse playerId {makeMove.PlayerId}");
        }
        
        var game = _grainFactory.GetGrain<IGameGrain>(gameIdAsGuid);
        var gameState = await game.GetState();

        if (gameState.Players.All(x => x.Id != playerIdAsGuid))
        {
            throw new InvalidActionException("Make move",
                $"Can't make move since player id {playerIdAsGuid} doesn't exist in the game's session.");
        }
        
        if (gameState.GameStatus != GameStatus.InPlay)
        {
            throw new InvalidActionException("Make move",
                $"Can't make move since game status is {gameState.GameStatus}.");
        }

        var player = _grainFactory.GetGrain<IPlayerGrain>(playerIdAsGuid);
        var playerDetails= await player.GetProfile();
        gameState = await game.MakeMove(new MakeMoveCommand
        {
            ActionType = makeMove.ActionType,
            PlayerId = playerIdAsGuid,
            PlayerNickName = playerDetails.NickName,
            SelectedTileRow = makeMove.SelectedTile?.Row,
            SelectedTileColumn = makeMove.SelectedTile?.Column,
        });
            
        await Clients
            .Group(gameState.GameId.ToString())
            .SendAsync("OnGameStateReceived", JsonConvert.SerializeObject(gameState));
    }
}