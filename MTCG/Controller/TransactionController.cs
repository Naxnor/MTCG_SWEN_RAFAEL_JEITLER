﻿using System.Formats.Asn1;
using MTCG.Database;
using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json;
using Npgsql;

namespace MTCG.Controller;

public class TransactionController
{
    private TransactionRepository _transactionRepository = new TransactionRepository();
    private UserRepository _userRepository = new UserRepository();
    private CardRepository _cardRepository = new CardRepository();
    private UserController _userController = new UserController();

    public void BuyPackage(HttpSvrEventArgs e)
    {
        // Get the user's ID from the token
        var userId =
            GetUserIdFromToken(e); 
        if (userId == 0)
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Deduct coins from the user's account
        if (!_userRepository.DeductCoins(userId, 5)) // 5 = Price change if needed
        {
            e.Reply(403, "\t\n\nNot enough money for buying a card package");
            return;
        }

        // Fetch a random package of cards
        var packageId = _cardRepository.GetRandomPackageId();
        var oldestPackageId = _cardRepository.GetOldestPackageId(); // delete for random 
        packageId = oldestPackageId; // delete for random 
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
            Damage = card.Damage,
            Element = card.Element,
            Class = card.Class,
            Type = card.Type
        }).ToList();

// Create a response object that includes both the cards and the success message // Change if not needed like this!
        var response = new
        {
            Message = "A package has been successfully bought",
            Cards = cardDtos
        };

// Serialize the response object to JSON and send it back to the client
        var jsonResponse =
            JsonConvert.SerializeObject(response,
                Formatting.Indented); // Use Formatting.Indented for a formatted output
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

                // Extract the username part from the token 
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

    public void CreateTradingDeal(HttpSvrEventArgs e)
    {
        // Extract and validate the token
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var user = _userController.ValidateTokenAndGetUser(authToken);
        int userID = _userRepository.GetUserIdByUsername(user.Username);
        
        
    
        if (string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Deserialize the request payload into a TradingDeal object
        var tradingDeal = JsonConvert.DeserializeObject<TradingDeal>(e.Payload);

        if (tradingDeal == null)
        {
            e.Reply(400, "Bad Request: Invalid trading deal data");
            return;
        }

        // Check if the card is owned by the user and not in a deck
        if (!_cardRepository.IsCardOwnedAndNotInDeck(tradingDeal.CardToTrade,userID))
        {
            e.Reply(403, "Forbidden: The card is not owned by the user or is in the deck");
            return;
        }

        // Check if a deal with this ID already exists
        if (_transactionRepository.DoesTradingDealExist(tradingDeal.Id))
        {
            e.Reply(409, "A deal with this deal ID already exists.");
            return;
        }

        // Create the trading deal
        if (_transactionRepository.CreateTradingDeal(tradingDeal, userID))
        {
            e.Reply(201, "Trading deal successfully created");
        }
        else
        {
            e.Reply(500, "Internal Server Error: Could not create trading deal");
        }
    }

    public void GetTradingDeals(HttpSvrEventArgs e)
    {
        // Authenticate the user
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var user = _userController.ValidateTokenAndGetUser(authToken);

        if (user == null || string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Fetch all trading deals
        var deals = _transactionRepository.GetAllTradingDeals();

        if (deals.Any())
        {
            // Serialize the trading deals to JSON
            var dealsJson = JsonConvert.SerializeObject(deals, Formatting.Indented);
            e.Reply(200, dealsJson);
        }
        else
        {
            e.Reply(204, "The request was fine, but there are no trading deals available");
        }
    }
    public void DeleteTradingDeal(HttpSvrEventArgs e, Guid tradingDealId)
    {
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var user = _userController.ValidateTokenAndGetUser(authToken);
        int userID = _userRepository.GetUserIdByUsername(user.Username);
        
        if (string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Unauthorized: Access token is missing or invalid");
            return;
        }

        if (!_transactionRepository.DoesTradingDealExist(tradingDealId))
        {
            e.Reply(404, "Not Found: The provided deal ID was not found.");
            return;
        }

        if (!_transactionRepository.IsTradingDealOwnedByUser(tradingDealId, userID))
        {
            e.Reply(403, "Forbidden: The deal contains a card that is not owned by the user.");
            return;
        }
        

        if (_transactionRepository.DeleteTradingDeal(tradingDealId))
        {
            e.Reply(200, "Trading deal successfully deleted");
        }
        else
        {
            e.Reply(500, "Internal Server Error: Could not delete trading deal");
        }
    }


    public void ExecuteTrade(HttpSvrEventArgs e, Guid tradingDealId)
    {
        // Extract and validate the token
        var authToken = UserController.ExtractAuthToken(e.Headers);
        var user = _userController.ValidateTokenAndGetUser(authToken);
        int userID = _userRepository.GetUserIdByUsername(user.Username);
        
        if (string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        // Deserialize the card ID from the request payload
        var offeredCardId = JsonConvert.DeserializeObject<Guid>(e.Payload);

        // Check if the trading deal exists and is not created by the requesting user
        var tradingDeal = _transactionRepository.GetTradingDeal(tradingDealId);
        if (tradingDeal == null)
        {
            e.Reply(404, "The provided deal ID was not found.");
            return;
        }
        if (tradingDeal.UserId == userID)
        {
            e.Reply(403, "Trading with oneself is not allowed.");
            return;
        }

        // Check if the offered card is owned by the user and meets the deal requirements
        if (!_cardRepository.IsCardOwnedAndNotInDeck(offeredCardId, userID) ||
            !_transactionRepository.DoesCardMeetTradeRequirements(offeredCardId, tradingDeal))
        {
            e.Reply(403, "The offered card is not owned by the user, or the requirements are not met (Type, MinimumDamage), or the offered card is locked in the deck.");
            return;
        }
        
        
        // Execute the trade and delete the trading deal
        if (_transactionRepository.ExecuteTradeAndDeleteDeal(offeredCardId, tradingDealId, userID))
        {
            e.Reply(200, "Trading deal successfully executed.");
        }
        else
        {
            e.Reply(500, "Internal Server Error: Could not execute the trading deal.");
        }
    }

}
