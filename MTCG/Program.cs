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
        }
    }
}