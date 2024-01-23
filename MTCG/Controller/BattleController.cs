using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;

namespace MTCG.Controller;

public class BattleController
{
    private UserRepository _userRepository;
    private CardRepository _cardRepository;
    private BattleService _battleService;

    public BattleController(UserRepository userRepository, CardRepository cardRepository, BattleService battleService)
    {
        _userRepository = userRepository;
        _cardRepository = cardRepository;
        _battleService = battleService;
    }

    public void StartBattle(HttpSvrEventArgs e)
    {
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var username = UserController.GetUsernameFromToken(e);

        if (string.IsNullOrEmpty(username))
        {
            e.Reply(401, "Unauthorized: Missing or invalid token");
            return;
        }

        var userId = _userRepository.GetUserIdByUsername(username);
        if (userId == 0)
        {
            e.Reply(404, "User not found");
            return;
        }

        // Get the player's deck
        IEnumerable<CardDTO> deck = _cardRepository.GetUserDeck(userId);
        if (!deck.Any())
        {
            e.Reply(400, "Bad Request: No deck configured");
            return;
        }

        // Start the battle process
        try
        {
            string battleLog = _battleService.StartBattle(userId, deck);
            // Reply with the battle log
            e.Reply(200, battleLog);
        }
        catch (InvalidOperationException ex)
        {
            e.Reply(400, $"Bad Request: {ex.Message}");
            return;
        }
        
        
    }
}


