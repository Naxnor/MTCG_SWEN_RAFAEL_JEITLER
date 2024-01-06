using MTCG.Database;
using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json;
using Npgsql;

namespace MTCG.Controller;

public class TransactionController
{
    private UserRepository _userRepository = new UserRepository();
    private CardRepository _cardRepository = new CardRepository();

    public void BuyPackage(HttpSvrEventArgs e)
    {
        // Get the user's ID from the token
        var userId = GetUserIdFromToken(e); // Make sure this method is implemented to extract the user ID from the token
        if (userId == 0)
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Deduct coins from the user's account
        if (!_userRepository.DeductCoins(userId, 5)) // Assuming a package costs 5 coins
        {
            e.Reply(403, "\t\n\nNot enough money for buying a card package");
            return;
        }

        // Fetch a random package of cards
        var packageId = _cardRepository.GetRandomPackageId();
        if (packageId == Guid.Empty)
        {
            // Refund the coins if no package is available
            _userRepository.AddCoins(userId, 5);
            e.Reply(404, "No card package available for buying");
            return;
        }

        // Assign the cards to the user and delete the package
        var cards = _cardRepository.GetCardsByPackageId(packageId);
        foreach (var card in cards)
        {
            _userRepository.AddCardToUser(userId, card.Id);
        }
        _cardRepository.DeletePackage(packageId);

        // Create a list of CardDTO objects
        var cardDtos = cards.Select(card => new CardDTO
        {
            Id = card.Id,
            Name = card.Name,
            Damage = card.Damage
        }).ToList();

// Create a response object that includes both the cards and the success message
        var response = new
        {
            Message = "A package has been successfully bought",
            Cards = cardDtos
        };

// Serialize the response object to JSON and send it back to the client
        var jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented); // Use Formatting.Indented for a nicely formatted output
        e.Reply(200, jsonResponse);
    }

    private int GetUserIdFromToken(HttpSvrEventArgs e)
        {
            const string authHeaderKey = "Authorization";
            const string tokenPrefix = "Bearer ";

            string username = null;

            foreach (var header in e.Headers)
            {
                if (header.Name.Equals(authHeaderKey, StringComparison.OrdinalIgnoreCase))
                {
                    var token = header.Value.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase)
                        ? header.Value.Substring(tokenPrefix.Length)
                        : header.Value;

                    // Extract the username part from the token (assuming format "username-mtcgToken")
                    var tokenParts = token.Split('-');
                    if (tokenParts.Length > 0)
                    {
                        username = tokenParts[0];
                    }
                }
            }

            if (username != null)
            {
                return _userRepository.GetUserIdByUsername(username);
            }

            return 0;
        }


    }
