using MTCG.Database;
using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Npgsql;

namespace MTCG.Controller;

public class BattleController
{
    private BattleService _battleService = new BattleService();
    private CardRepository _cardRepository = new CardRepository();
    private UserRepository _userRepository = new UserRepository();

    public Dictionary<int, bool> battleInProgress = new Dictionary<int, bool>();

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
        if (HasRecentlyFought(userId))
        {
            string battleLog = GetLatestBattleLog(userId);
            e.Reply(200, battleLog); // Send the battle log as a response
            return;
        }
        if (opponentId == 0)
        {
            e.Reply(202, "Accepted: Waiting for an opponent");
            return;
        }
        
        var opponentDeckDto = _cardRepository.GetUserDeck(opponentId);
        var opponentDeck = _battleService.ConvertDeckDtoToDeck(opponentDeckDto);
        
        var user = _userRepository.GetUserById(userId);
        var opponent = _userRepository.GetUserById(opponentId);

        if (battleInProgress.ContainsKey(userId) && battleInProgress[userId])
        {
            e.Reply(409, "Conflict: Battle already in progress for user");
            return;
        }

        try
        {
            battleInProgress[userId] = true;
            string battleLog = _battleService.StartBattle(userId, user.Username, opponentId, opponent.Username, deck, opponentDeck);
            e.Reply(200, battleLog);
        }
        catch (Exception ex)
        {
            e.Reply(500, $"Internal Server Error: {ex.Message}");
        }
        finally
        {
            battleInProgress[userId] = false;
        }
    }
    private bool HasRecentlyFought(int userId)
    {
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT COUNT(*)
                FROM Battles
                WHERE (UserId1 = @UserId OR UserId2 = @UserId)
                  AND Timestamp > NOW() - INTERVAL '5 seconds'";
                cmd.Parameters.AddWithValue("@UserId", userId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
    }

    private string GetLatestBattleLog(int userId)
    {
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = @"
                SELECT BattleLog
                FROM Battles
                WHERE (UserId1 = @UserId OR UserId2 = @UserId)
                ORDER BY Timestamp DESC
                LIMIT 1";
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0); // Assuming BattleLog is stored as a text
                    }
                }
            }
        }
        return "No recent battle found."; // Or handle this case as you see fit
    }

    }
    
    
    



