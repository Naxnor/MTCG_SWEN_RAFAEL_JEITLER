using MTCG.Controller;
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
            if (e.Path.StartsWith("/users" )&& e.Method.Equals("POST"))
            {
                _userController.CreateUser(e);
            }
            else if (e.Path.StartsWith("/users") && e.Method.Equals("GET"))
            {
                // Call the GetUser method for GET requests
                _userController.GetUser(e);
            }
            //Console.WriteLine(e.PlainMessage);

            //e.Reply(200, "Yo! Understood.");
            
        }
    }
}