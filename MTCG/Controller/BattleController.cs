using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;

namespace MTCG.Controller;

public class BattleController
{
    private BattleService _battleService = new BattleService();
    private CardRepository _cardRepository = new CardRepository();
    private UserRepository _userRepository = new UserRepository();
    
    public void StartBattle(HttpSvrEventArgs e)
    {
   
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var username = UserController.GetUsernameFromToken(e);
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Unauthorized: Missing or invalid token");
            return;
        }
       
        int userId = _userRepository.GetUserIdByUsername(username);
        if (userId == 0)
        {
            e.Reply(404, "User not found");
            return;
        }
     
        var deckDto = _cardRepository.GetUserDeck(userId);
        if (deckDto == null || !deckDto.Any())
        {
            e.Reply(400, "Bad Request: No deck configured");
            return;
        }

        var deck = _battleService.ConvertDeckDtoToDeck(deckDto); // Convert DTO to actual Card objects

        int opponentId = _battleService.EnterLobby(userId);
        if (opponentId == 0)
        {
            e.Reply(202, "Accepted: Waiting for an opponent");
            return;
        }

        // Debugging: Print decks
        
        var opponentDeckDto = _cardRepository.GetUserDeck(opponentId);
        var opponentDeck = _battleService.ConvertDeckDtoToDeck(opponentDeckDto);
        
        var user = _userRepository.GetUserById(userId);
        var opponent = _userRepository.GetUserById(opponentId);

        if (user == null || opponent == null)
        {
            e.Reply(500, "Internal Server Error: User or opponent not found");
            return;
        }
        
        try
        {
            
            string battleLog = _battleService.StartBattle(userId, user.Username, opponentId, opponent.Username, deck,opponentDeck);            e.Reply(200, battleLog);
        }
        catch (Exception ex)
        {
            e.Reply(500, $"Internal Server Error: {ex.Message}");
        }
    }
    
    
    
}


