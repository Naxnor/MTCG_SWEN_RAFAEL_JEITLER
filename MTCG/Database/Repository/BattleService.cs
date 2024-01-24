using System.Net.Security;
using System.Text;
using MTCG.Models;
using Npgsql;

namespace MTCG.Database.Repository;

public class BattleService
{
    private static readonly Queue<int> lobbyQueue = new Queue<int>();
    private static readonly object lobbyLock = new object();
    public string StartBattle(int userId, IEnumerable<CardDTO> deck)
    {
        // Matchmaking logic to find an opponent
        int opponentId = FindOpponent(userId);
        
        // If an opponent is found, proceed with the battle
        if (opponentId != userId && opponentId != 0)
        {
            // Convert CardDTO to Card or whatever format ExecuteBattle expects
            var userDeck = ConvertDeckDtoToDeck(deck);

            // Execute the battle and return the result
            var battleResult = ExecuteBattle(userId, opponentId, userDeck);
            return battleResult;
        }
        else
        {
            // The user is waiting for an opponent in the lobby
            // This would normally be an async process, but for simplicity, we're just returning a message
            return "Waiting for an opponent...";
        }
    }

    private IEnumerable<Card> ConvertDeckDtoToDeck(IEnumerable<CardDTO> deckDto)
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

    public string ExecuteBattle(int userId, int opponentId, IEnumerable<Card> userDeck)
    {
        var battleLog = new StringBuilder();
        var userCards = userDeck.ToList();
        var opponentCards = GetOpponentDeck(opponentId).ToList();

        int userWins = 0;
        int opponentWins = 0;

        // Simulate rounds - here we simply loop for a set number of rounds for simplicity
        for (int round = 1; round <= 100; round++)
        {
            var userCard = ChooseCardForRound(userCards);
            var opponentCard = ChooseCardForRound(opponentCards);

            var outcome = SimulateRound(userCard, opponentCard);
            ApplyRoundOutcome(outcome, userCards, opponentCards);

            if (userCards.Count == 0 || opponentCards.Count == 0)
            {
                // One player has run out of cards, end the battle
                break;
            }
        }

        UpdatePlayerStats(userId, opponentId, userWins, opponentWins,battleLog);
        return battleLog.ToString();
    }

    private void UpdatePlayerStats(int userId, int opponentId, int userWins, int opponentWins, StringBuilder battleLog)
    {
        // Determine the result from user's perspective
        string result;
        if (userWins > opponentWins) result = "win";
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
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        File.WriteAllText(Path.Combine(logDirectory, logFileName), battleLog.ToString());
    }

    private void UpdateEloRatings(int player1Id, int player2Id, string result)
    {
        string query = "SELECT update_elo_after_game(@player1Id, @player2Id, @result);";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@player1Id", player1Id);
                cmd.Parameters.AddWithValue("@player2Id", player2Id);
                cmd.Parameters.AddWithValue("@result", result); // 'win', 'loss', or 'draw'

                cmd.ExecuteNonQuery();
            }
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
    // Initialize the outcome object
    var outcome = new RoundOutcome();

    // Apply elemental damage effects
    float userEffectiveDamage = CalculateEffectiveDamage(userCard, opponentCard);
    float opponentEffectiveDamage = CalculateEffectiveDamage(opponentCard, userCard);

    // Determine round outcome based on effective damage
    if (userEffectiveDamage > opponentEffectiveDamage)
    {
        outcome.WinnerCard = userCard;
        outcome.LoserCard = opponentCard;
        outcome.Result = RoundResult.Win;
    }
    else if (userEffectiveDamage < opponentEffectiveDamage)
    {
        outcome.WinnerCard = opponentCard;
        outcome.LoserCard = userCard;
        outcome.Result = RoundResult.Loss;
    }
    else
    {
        // Handle the draw case
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
            if (defendingCard.Class == "Knight") damage = 999999 ;
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


    public int FindOpponent(int userId)
    {
        lock (lobbyLock)
        {
            // Check if there's already someone in the lobby
            if (lobbyQueue.Count > 0)
            {
                int opponentId = lobbyQueue.Dequeue();
                // Make sure the opponent is not the current user
                if (opponentId != userId)
                {
                    return opponentId;
                }
            }

            // If no opponent found, the user enters the lobby
            lobbyQueue.Enqueue(userId);
        }

        // Wait for an opponent to enter the lobby
        while (true)
        {
            lock (lobbyLock)
            {
                if (lobbyQueue.Peek() != userId)
                {
                    return lobbyQueue.Dequeue();
                }
            }

            // Wait for a short period before checking the lobby again
            Thread.Sleep(1000);
        }
    }
}
