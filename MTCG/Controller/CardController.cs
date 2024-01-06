using MTCG.Controller;
using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json;
namespace MTCG.Database.Repository;

public class CardController
{
    private CardRepository _cardRepository = new CardRepository();

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

}
