using MTCG.Controller;
using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json;
using System.Linq;
using System.Linq.Expressions;

namespace MTCG.Database.Repository;

public class CardController
{
    private CardRepository _cardRepository = new CardRepository();
    private UserRepository _userRepository = new UserRepository();
    public void CreatePackage(HttpSvrEventArgs e)
    {
        // Check if the Authorization header is present
        if (!e.Headers.Any(header => header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Authentication and admin check
        if (!IsAdmin(e))
        {
            e.Reply(403, "Provided user is not \"admin\"");
            return;
        }

        try
        {
            var cards = JsonConvert.DeserializeObject<List<Card>>(e.Payload);
            if (cards == null || cards.Count == 0)
            {
                e.Reply(400, "Bad Request: Invalid package data");
                return;
            }

            // Check if any card already exists
            if (cards.Any(card => _cardRepository.DoesCardExist(card.Id)))
            {
                e.Reply(409, "At least one card in the packages already exists");
                return;
            }

            if (_cardRepository.AddPackage(cards))
            {
                e.Reply(201, "Package and cards successfully created");
            }
            else
            {
                e.Reply(500, "Internal Server Error: Failed to create package");
            }
        }
        catch (JsonException)
        {
            e.Reply(400, "Bad Request: Invalid JSON format");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreatePackage: {ex.Message}");
            e.Reply(500, "Internal Server Error");
        }
    }

    private bool IsAdmin(HttpSvrEventArgs e)
    {
        // Extract the authentication token from the request headers
        var authToken = UserController.ExtractAuthToken(e.Headers);

        // Check if the token is valid and indicates an admin
        if (authToken != null && authToken.StartsWith("admin-"))
        {
            return true; 
        }

        return false; // Not an admin user or token is invalid
    }

    public void GetAllUserCards(HttpSvrEventArgs e)
    {
        // Extract the user ID from the token
        var username = GetUsernameFromToken(e); // Implement this method based on your token structure
        if (string.IsNullOrEmpty(username))
        {
            e.Reply(401, "Unauthorized: Access token is missing or invalid");
            return;
        }

        // Get the user ID from the username
        var userId = _userRepository.GetUserIdByUsername(username);
        if (userId == 0)
        {
            e.Reply(404, "User not found");
            return;
        }

        // Fetch the user's cards
        var cards = _cardRepository.GetUserCards(userId);
        if (!cards.Any())
        {
            e.Reply(200, "The request was fine but, The user doesn't have any cards"); // using 200 since 204 does not allow a payload
            return;
        }

        // Create a list of CardDTO objects from the fetched cards
        var cardDtos = cards.Select(card => new CardDTO
        {
            Id = card.Id,
            Name = card.Name,
            Damage = card.Damage,
            Element = card.Element,
            Class = card.Class,
            Type = card.Type
        }).ToList();

// Serialize the list of CardDTO objects with indentation
        var jsonResponse = JsonConvert.SerializeObject(cardDtos, Formatting.Indented);

// Send the nicely formatted JSON response back to the client
        e.Reply(200, jsonResponse);
    }
    
    private string GetUsernameFromToken(HttpSvrEventArgs e)
    {
        const string authHeaderKey = "Authorization";
        const string tokenPrefix = "Bearer ";

        foreach (var header in e.Headers)
        {
            if (header.Name.Equals(authHeaderKey, StringComparison.OrdinalIgnoreCase))
            {
                var token = header.Value.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase)
                    ? header.Value.Substring(tokenPrefix.Length)
                    : header.Value;

                // Assuming the token format is "username-mtcgToken"
                var tokenParts = token.Split('-');
                if (tokenParts.Length > 1)
                {
                    return tokenParts[0]; // return the username part
                }
            }
        }
        return null; // or throw an appropriate exception
    }
    
    public void ConfigureUserDeck(HttpSvrEventArgs e)
    {
        var username = GetUsernameFromToken(e); // Implement this based on your token structure
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

        var cardIds = JsonConvert.DeserializeObject<List<Guid>>(e.Payload);
        if (cardIds == null || cardIds.Count != 4)
        {
            e.Reply(400, "The provided deck did not include the required amount of cards");
            return;
        }
        
        var userCards = _cardRepository.GetUserCards(userId).Select(card => card.Id);
        if (!cardIds.All(id => userCards.Contains(id)))
        {
            e.Reply(403, "At least one of the provided cards does not belong to the user or is not available.");
            return;
        }

        if (_cardRepository.ConfigureDeck(userId, cardIds))
        {
            e.Reply(200, "The deck has been successfully configured");
        }
        else
        {
            e.Reply(500, "Internal Server Error: Could not configure deck");
        }
    }
    
    public void GetUserDeck(HttpSvrEventArgs e)
    {
        var username = GetUsernameFromToken(e); // Implement this based on your token structure
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

        var deck = _cardRepository.GetUserDeck(userId);
        if (!deck.Any())
        {
            e.Reply(204); 
            return;
        }

        string formatValue;
        if (e.QueryParameters.TryGetValue("format", out formatValue) && formatValue.Equals("plain", StringComparison.OrdinalIgnoreCase))
        {
            var plainTextResponse = string.Join(Environment.NewLine, deck.Select(card => $"Id: {card.Id}, Name: {card.Name}, Damage: {card.Damage}, Element: {card.Element}, Class: {card.Class}, Type: {card.Type}"));
            e.Reply(200, plainTextResponse);
        }
        else
        {
            var jsonResponse = JsonConvert.SerializeObject(deck, Formatting.Indented);
            e.Reply(200, jsonResponse);
        }
    }

    
}
