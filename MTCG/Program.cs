using MTCG.Controller;
using MTCG.Database.Repository;
using MTCG.Server;

namespace MTCG
{
    internal class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // entry point                                                                                                      //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Main entry point.</summary>
        /// <param name="args">Arguments.</param>
        static void Main(string[] args)
        {
            HttpSvr svr = new();
            svr.Incoming += _ProcessMessage;

            svr.Run();
        }


        /// <summary>Event handler for incoming server requests.</summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private static void _ProcessMessage(object sender, HttpSvrEventArgs e)
        {
            
            UserController _userController = new UserController();
            CardController _cardController = new CardController();
            TransactionController _transactionController = new TransactionController();
            BattleController _battleController = new BattleController();
            
            if (e.Path.StartsWith("/users" )&& e.Method.Equals("POST"))
            {
                _userController.CreateUser(e);
            }
            else if (e.Path.StartsWith("/users") && e.Method.Equals("GET"))
            {
                _userController.GetUser(e);
            }
            else if (e.Path.StartsWith("/session") && e.Method.Equals("POST"))
            {
                _userController.LoginUser(e);
            }
            else if (e.Path.StartsWith("/users") && e.Method.Equals("PUT"))
            {
                _userController.UpdateUserData(e);
            }
            else if (e.Path.StartsWith("/packages") && e.Method.Equals("POST"))
            {
                _cardController.CreatePackage(e);
            }
            else if (e.Path.StartsWith("/transactions/packages") && e.Method.Equals("POST"))
            {
                _transactionController.BuyPackage(e); 
            }
            else if (e.Path.Equals("/cards") && e.Method.Equals("GET"))
            {
                _cardController.GetAllUserCards(e);
            }
            else if (e.Path.Equals("/deck") && e.Method.Equals("GET"))
            {
                _cardController.GetUserDeck(e);
            }
            else if (e.Path.Equals("/deck") && e.Method.Equals("PUT"))
            {
                _cardController.ConfigureUserDeck(e);
            }
            else if (e.Path.Equals("/stats") && e.Method.Equals("GET"))
            {
                _userController.GetUserStats(e);
            }
            else if (e.Path.Equals("/scoreboard") && e.Method.Equals("GET"))
            {
                _userController.GetScoreboard(e);
            }
            else if (e.Path.Equals("/battles") && e.Method.Equals("POST"))
            {
                _battleController.StartBattle(e);
            }
            else if (e.Path.Equals("/tradings") && e.Method.Equals("POST"))
            {
                _transactionController.CreateTradingDeal(e);
            }
            else if (e.Path.Equals("/tradings") && e.Method.Equals("GET"))
            {
                _transactionController.GetTradingDeals(e);
            }
            else if (e.Path.StartsWith("/tradings/") && e.Method.Equals("DELETE"))
            {
                    // Extract the tradingdealid from the path
                    var tradingDealId = e.Path.Split('/').LastOrDefault(); 

                    // It's important to validate that tradingDealId is not null or empty before proceeding
                    if (!string.IsNullOrEmpty(tradingDealId))
                    {
                        // Convert the tradingDealId to a Guid and call the DeleteTradingDeal method
                        if (Guid.TryParse(tradingDealId, out var guidTradingDealId))
                        {
                            _transactionController.DeleteTradingDeal(e, guidTradingDealId);
                        }
                        else
                        {
                            e.Reply(400, "Bad Request: Invalid trading deal ID");
                        }
                    }
                    else
                    {
                        e.Reply(404, "Not Found: Trading deal ID is required");
                    }
            }
            else if (e.Path.StartsWith("/tradings/") && e.Method.Equals("POST"))
            {
                var tradingDealId = e.Path.Split('/').LastOrDefault(); // extract the  ID

                // IValidate that tradingDealId is not null or empty before proceeding
                if (!string.IsNullOrEmpty(tradingDealId))
                {
                    // Convert the tradingDealId to a Guid and call the Execute  method
                    if (Guid.TryParse(tradingDealId, out var guidTradingDealId))
                    {

                        _transactionController.ExecuteTrade(e, guidTradingDealId);
                    }
                    else
                    {
                        e.Reply(400, "Bad Request: Invalid trading deal ID");
                    }
                }
                else
                {
                    e.Reply(404, "Not Found: Trading deal ID is required");
                }
            }

        }
    }
}