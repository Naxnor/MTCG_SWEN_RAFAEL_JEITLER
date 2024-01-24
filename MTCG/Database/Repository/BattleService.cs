using System.Net.Security;
using System.Text;
using MTCG.Models;
using Npgsql;

namespace MTCG.Database.Repository;


public class BattleService
{
    private static readonly Queue<int> lobbyQueue = new Queue<int>();
    private static readonly object lobbyLock = new object();
    private static readonly Dictionary<int, AutoResetEvent> waitingEvents = new Dictionary<int, AutoResetEvent>();
    

    public int EnterLobby(int userId)
    {
        lock (lobbyLock)
        {
            // If there's already someone in the lobby, match them with the current user
            if (lobbyQueue.Count > 0 && lobbyQueue.Peek() != userId)
            {
                return lobbyQueue.Dequeue(); // Dequeue the first user in the lobby
            }

            lobbyQueue.Enqueue(userId); // Add current user to the lobby
            waitingEvents[userId] = new AutoResetEvent(false); // Create an event for the current user
        }

        // Wait for an opponent to enter the lobby and get matched
        if (waitingEvents[userId].WaitOne())
        {
            // The user has been matched, return the opponent's ID
            lock (lobbyLock)
            {
                if (lobbyQueue.Count > 0 && lobbyQueue.Peek() != userId)
                {
                    waitingEvents.Remove(userId);
                    return lobbyQueue.Dequeue();
                }
            }
        }

        // No opponent was found or an error occurred
        waitingEvents.Remove(userId);
        return 0;
    }
    
    

    public string StartBattle(int userId, string userName, int opponentId, string opponentName, IEnumerable<Card> userDeck, IEnumerable<Card> opponentDeck)    {
        var userDeckList = userDeck.ToList(); // Assuming it's already a list of actual Card objects
        var opponentDeckList = opponentDeck.ToList(); // Fetch opponent's deck

       
     

        if (opponentId == userId || opponentId == 0)
        {
            throw new InvalidOperationException("No valid opponent found.");
        }
        
 

        // Execute the battle and return the result
        return ExecuteBattle(userId, userName, opponentId, opponentName, userDeckList, opponentDeckList);    }
    private void DebugPrintDeck(string deckName, IEnumerable<Card> deck)
    {
        Console.WriteLine($"{deckName}:");
        foreach (var card in deck)
        {
            Console.WriteLine($"ID: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, Element: {card.Element}, Class: {card.Class}");
        }
        Console.WriteLine();
    }
    public IEnumerable<Card> ConvertDeckDtoToDeck(IEnumerable<CardDTO> deckDto)
    {
        var cardRepository = new CardRepository();
        var cards = new List<Card>();

        foreach (var cardDto in deckDto)
        {
            var card = cardRepository.GetCardById(cardDto.Id);
            if (card != null)
            {
                cards.Add(card);
            }
            else
            {
                // Handle the case where the card isn't found
                throw new InvalidOperationException($"Card with ID {cardDto.Id} not found.");
            }
        }

        return cards;
    }

    private IEnumerable<Card> GetOpponentDeck(int opponentId)
    {
        var cardRepository = new CardRepository();
        return cardRepository.GetCardsInDeck(opponentId);
    }
    private void DebugPrintCard(string cardName, Card card)
    {
        Console.WriteLine($"{cardName}:");
        Console.WriteLine($"ID: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, Element: {card.Element}, Class: {card.Class}");
        Console.WriteLine();
    }

    public string ExecuteBattle(int userId, string userName, int opponentId, string opponentName, IEnumerable<Card> userDeck, IEnumerable<Card> opponentDeck){
    var battleLog = new StringBuilder();
    var userCards = userDeck.ToList();
    var opponentCards = opponentDeck.ToList();

    int userWins = 0;
    int opponentWins = 0;
    int round = 1;
    for ( round = 1; round <= 1000 && userCards.Count > 0 && opponentCards.Count > 0; round++)
    {
        var userCard = ChooseCardForRound(userCards);
        var opponentCard = ChooseCardForRound(opponentCards);
        
        var outcome = SimulateRound(userCard, opponentCard);
        ApplyRoundOutcome(outcome, userCards, opponentCards);

        // Append to battle log
        battleLog.AppendLine($"Round {round}: {userCard.Name} (Damage: {outcome.UserEffectiveDamage}) vs {opponentCard.Name} (Damage: {outcome.OpponentEffectiveDamage})");        switch (outcome.Result)
        {
            case RoundResult.Win:
                userWins++;
                battleLog.AppendLine($"Winner: {userName}'s {userCard.Name}");
                battleLog.AppendLine($"{opponentName}'s {opponentCard.Name} is added to {userName}'s deck.");
                break;
            case RoundResult.Loss:
                opponentWins++;
                battleLog.AppendLine($"Winner: {opponentName}'s {opponentCard.Name}");
                battleLog.AppendLine($"{userName}'s {userCard.Name} is added to {opponentName}'s deck.");
                break;
            case RoundResult.Draw:
                battleLog.AppendLine("Draw");
                break;
        }
        battleLog.AppendLine($"Cards remaining - {userName}: {userCards.Count}, {opponentName}: {opponentCards.Count}");
        battleLog.AppendLine(); // Blank line for readability
    }

    // Announce the overall winner at the end of the battle log
    string finalResult;
     /*   if (round >=100) // uncomment for always draw in curl
    {
        finalResult = "Battle ended in a draw";
    }
    
    else */ if (userWins > opponentWins )
    {
        finalResult = $"Overall Winner: User {userName} with {userWins} rounds won";
    }
    else if (opponentWins > userWins)
    {
        finalResult = $"Overall Winner: User {opponentName} with {opponentWins} rounds won";
    }
    else
    {
        finalResult = "Battle ended in a draw";
    }
    
    battleLog.AppendLine(finalResult);

    UpdatePlayerStats(userId, opponentId, userWins, opponentWins, battleLog,round);
    return battleLog.ToString();
}



   

    private void UpdatePlayerStats(int userId, int opponentId, int userWins, int opponentWins, StringBuilder battleLog, int rounds)
    {
        // Determine the result from user's perspective
        string result;
        /*if (rounds >= 100) result = "draw"; // uncomment for always draw in curl
        else*/ if (userWins > opponentWins ) result = "win";
        else if (userWins < opponentWins) result = "loss";
        else result = "draw";

        // Update Elo ratings and stats in the database
        UpdateEloRatings(userId, opponentId, result);

        // Save the battle log
        SaveBattleLog(userId, opponentId, battleLog);
    }

    private void SaveBattleLog(int userId, int opponentId, StringBuilder battleLog)
    {
        string logFileName = $"BattleLog_{userId}_vs_{opponentId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory; // Get the project directory
        string logFolderPath = Path.Combine(projectDirectory, "Logs"); // Create the path to the "Logs" folder

        // Create the "Logs" directory if it doesn't exist
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }

        File.WriteAllText(Path.Combine(logFolderPath, logFileName), battleLog.ToString());
    }


private void UpdateEloRatings(int player1Id, int player2Id, string result)
{
    // Query to get the current ELO and games played for both players
    string getPlayerEloQuery = @"
        SELECT elo, games FROM users WHERE id = @playerId;";

    // Function to calculate the expected outcome based on ELO ratings
    Func<int, int, double> calculateExpectedOutcome = (elo1, elo2) =>
        1.0 / (1.0 + Math.Pow(10.0, (elo2 - elo1) / 400.0));

    // Function to determine the K factor based on the number of games played
    Func<int, int> calculateKFactor = (games) => games < 30 ? 32 : 24;

    using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
    {
        conn.Open();

        // Get current ELO and games for both players
        int elo1 = GetPlayerEloAndGames(conn, player1Id, out int games1);
        int elo2 = GetPlayerEloAndGames(conn, player2Id, out int games2);

        // Calculate the expected outcomes
        double expectedOutcome1 = calculateExpectedOutcome(elo1, elo2);
        double expectedOutcome2 = calculateExpectedOutcome(elo2, elo1);

        // Calculate actual outcomes based on match result
        int actualOutcome1 = result == "win" ? 1 : result == "loss" ? 0 : 0; // For player 1
        int actualOutcome2 = result == "loss" ? 1 : result == "win" ? 0 : 0; // For player 2

        // Calculate new ELO ratings
        int kFactor1 = calculateKFactor(games1);
        int kFactor2 = calculateKFactor(games2);

        int newElo1 = elo1 + (int)(kFactor1 * (actualOutcome1 - expectedOutcome1));
        int newElo2 = elo2 + (int)(kFactor2 * (actualOutcome2 - expectedOutcome2));

        // Update ELO ratings and stats in the database
        UpdatePlayerStats(conn, player1Id, newElo1, games1 + 1, result);
        UpdatePlayerStats(conn, player2Id, newElo2, games2 + 1, result == "win" ? "loss" : result == "loss" ? "win" : "draw");
    }
}

private int GetPlayerEloAndGames(NpgsqlConnection conn, int playerId, out int games)
{
    string query = "SELECT elo, games FROM users WHERE id = @playerId;";
    using (var cmd = new NpgsqlCommand(query, conn))
    {
        cmd.Parameters.AddWithValue("@playerId", playerId);
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                games = reader.GetInt32(reader.GetOrdinal("games"));
                return reader.GetInt32(reader.GetOrdinal("elo"));
            }
        }
    }

    games = 0;
    return 1000; // Default ELO if not found
}

private void UpdatePlayerStats(NpgsqlConnection conn, int playerId, int newElo, int newGames, string result)
{
    string updateQuery = @"
        UPDATE users
        SET elo = @newElo, games = @newGames, 
            wins = CASE WHEN @result = 'win' THEN wins + 1 ELSE wins END,
            losses = CASE WHEN @result = 'loss' THEN losses + 1 ELSE losses END
        WHERE id = @playerId;";

    using (var cmd = new NpgsqlCommand(updateQuery, conn))
    {
        cmd.Parameters.AddWithValue("@newElo", newElo);
        cmd.Parameters.AddWithValue("@newGames", newGames);
        cmd.Parameters.AddWithValue("@result", result);
        cmd.Parameters.AddWithValue("@playerId", playerId);

        cmd.ExecuteNonQuery();
    }
}

    private void ApplyRoundOutcome(RoundOutcome outcome, List<Card> userDeck, List<Card> opponentDeck)
    {
        if (outcome.Result == RoundResult.Win)
        {
            // Add the loser's card to the winner's deck
            userDeck.Add(outcome.LoserCard);
            // Remove the loser's card from the loser's deck
            opponentDeck.Remove(outcome.LoserCard);
        }
        else if (outcome.Result == RoundResult.Loss)
        {
            // Add the loser's card to the winner's deck
            opponentDeck.Add(outcome.LoserCard);
            // Remove the loser's card from the loser's deck
            userDeck.Remove(outcome.LoserCard);
        }
        // In case of a draw, do nothing
    }

    private RoundOutcome SimulateRound(Card userCard, Card opponentCard)
    {
        var outcome = new RoundOutcome();
        outcome.UserEffectiveDamage = CalculateEffectiveDamage(userCard, opponentCard);
        outcome.OpponentEffectiveDamage = CalculateEffectiveDamage(opponentCard, userCard);

        if (outcome.UserEffectiveDamage > outcome.OpponentEffectiveDamage)
        {
            outcome.WinnerCard = userCard;
            outcome.LoserCard = opponentCard;
            outcome.Result = RoundResult.Win;
        }
        else if (outcome.UserEffectiveDamage < outcome.OpponentEffectiveDamage)
        {
            outcome.WinnerCard = opponentCard;
            outcome.LoserCard = userCard;
            outcome.Result = RoundResult.Loss;
        }
        else
        {
            outcome.Result = RoundResult.Draw;
        }

        return outcome;
    }

private float CalculateEffectiveDamage(Card attackingCard, Card defendingCard)
{
    float damage = attackingCard.Damage;

    // Elemental advantages and disadvantages
    switch (attackingCard.Element)
    {
        case "Fire":
            if (defendingCard.Element == "Water") damage /= 2;
            if (defendingCard.Element == "Ice") damage *= 2;
            if (defendingCard.Element == "Plant") damage *= 2;
            break;
        case "Water":
            if (defendingCard.Element == "Ice") damage /= 2;
            if (defendingCard.Element == "Fire") damage *= 2;
            if (defendingCard.Class == "Knight" ) damage = 999999 ;
            //if (defendingCard.Class == "Knight" && attackingCard.Element == "Spell") damage = 999999 ;
            break;
        case "Air":
            if (defendingCard.Element == "Electro") damage /= 2;
            break;
        case "Ice":
            if (defendingCard.Element == "Fire") damage /= 2;
            if (defendingCard.Element == "Water") damage *= 2;
            break;
        case "Plant":
            if (defendingCard.Element == "Fire") damage /= 2;
            if (defendingCard.Element == "Ground") damage *= 2;
            break;
        case "Electro":
            if (defendingCard.Element == "Ground") damage = 0;
            if (defendingCard.Element == "Water") damage *= 2;
            break;
        case "Ground":
            if (defendingCard.Element == "Plant") damage /= 2;
            if (defendingCard.Element == "Electro") damage *= 2;
            break;
        // Add more elemental interactions as needed
    }

    // Class-based interactions
    switch (attackingCard.Class)
    {
        case "Goblin":
            if (defendingCard.Class == "Dragon") damage = 0 ;
            break;
        case "Elf":
            if (defendingCard.Class == "Dragon") damage *= 2; // Elves do more damage to Dragons
            if (defendingCard.Class == "Dwarf") damage *= 2; // Elves deal extra damage to Dwarfs
            if (defendingCard.Class == "Orc") damage *= 2; // Elves deal extra damage to Orcs
            if (defendingCard.Class == "Goblin") damage *= 2; // Elves deal extra damage to Goblins
            break;
        case "Wizard":
            if (defendingCard.Class == "Ork") damage = 0; // Wizards mind-control Orks, making them miss attacks
            if (defendingCard.Class == "Goblin") damage = 0; // Wizards mind-control Goblins, making them miss attacks
            break;
        case "Dwarf":
            if (defendingCard.Class == "Dragon") damage *= 2; // Dwarfs deal extra damage to Dragons
            break;
        case "Kraken":
            if (defendingCard.Class == "Spell") damage = 0; // Kraken is immune to Spells
            if (defendingCard.Class == "Trap") damage *= 2; // Kraken is vulnerable to Traps
            break;
        case "Vampire":
            if (defendingCard.Class == "Elf" || defendingCard.Class == "Dwarf" || defendingCard.Class == "Orc" || defendingCard.Class == "Goblin")
                damage *= 2; // Vampires do double damage to Elves, Dwarfs, Orcs, and Goblins
            if (defendingCard.Class == "Knight") damage = 0; // Knights are immune to Vampires
            break;
        case "Trap":
            if (defendingCard.Class == "Troll") damage *= 2; // Trolls are vulnerable to Traps
            break;
        
        case "Regular":
            break;
        // Add more class interactions as needed
    }

    return damage;
}

// A class to represent the outcome of a round
    public class RoundOutcome
    {
        public Card WinnerCard { get; set; }
        public Card LoserCard { get; set; }
        public RoundResult Result { get; set; }
        public float UserEffectiveDamage { get; set; }
        public float OpponentEffectiveDamage { get; set; }
    }

    public enum RoundResult
    {
        Win,
        Loss,
        Draw
    }

    private Card ChooseCardForRound(List<Card> deck)
    {
        if(deck == null || deck.Count == 0)
        {
            throw new InvalidOperationException("Cannot choose a card from an empty or null deck.");
        }

        // Randomly select a card index
        Random rnd = new Random();
        int cardIndex = rnd.Next(deck.Count);

        // Return the selected card
        return deck[cardIndex];
    }


   
}
