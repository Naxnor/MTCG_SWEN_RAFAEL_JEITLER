using MTCG.Database.Repository;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json;

namespace MTCG.Controller;

public class UserController
{
    private UserRepository _userRepository = new UserRepository();

    public void CreateUser(HttpSvrEventArgs e)
    {
        var user = JsonConvert.DeserializeObject<User>(e.Payload);
        //Console.WriteLine(user.Password);
        try
        {
            _userRepository.CreateUser(user);
            e.Reply(201, "User Created");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            e.Reply(409, "User Already Exists");
        }
    }

    public void LoginUser(HttpSvrEventArgs e)
    {
        try
        {
            var formname = JsonConvert.DeserializeObject<User>(e.Payload);
            if (formname == null || string.IsNullOrWhiteSpace(formname.Username) ||
                string.IsNullOrWhiteSpace(formname.Password))
            {
                e.Reply(400, "Invalid request");
                return;
            }

            bool isAuthenticated = _userRepository.AuthenticateUser(formname.Username, formname.Password);
            if (isAuthenticated)
            {
                string token = GenerateSimpleToken(formname.Username);
                e.Reply(200, JsonConvert.SerializeObject(new { Token = token }));
            }
            else
            {
                e.Reply(401, "Unauthorized: Incorrect username or password");
            }
        }
        catch (Exception)
        {
            e.Reply(500, "Internal server error");
        }
    }

    private string GenerateSimpleToken(string FormUsername)
    {
        string token = FormUsername + "-mtcgToken";
        return token;
    }

    public void GetUser(HttpSvrEventArgs e)
    {
        if (!e.Parameters.TryGetValue("username", out string username))
        {
            e.Reply(400, "Bad Request: Username is required");
            return;
        }

        if (!IsAuthorized(e, username))
        {
            e.Reply(401, "Unauthorized");
            return;
        }

        try
        {
            var user = _userRepository.GetUserByUsername(username);
            if (user != null)
            {
                var userData = new UserDTO // new data transfer object for added security 
                {
                    Name = user.Name,
                    Bio = user.Bio,
                    Image = user.Image
                };

                e.Reply(200, JsonConvert.SerializeObject(new { userData.Name, userData.Bio, userData.Image }));
            }
            else
            {
                e.Reply(404, "User not found");
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            e.Reply(500, "Internal Server Error");
        }
    }

    private bool IsAuthorized(HttpSvrEventArgs e, string username)
    {
        // Extract the authentication token from the request headers
        var authToken = ExtractAuthToken(e.Headers);

        // Validate the token and retrieve the user information
        // This is a placeholder - you need to implement the actual token validation and user retrieval
        var requestingUser = ValidateTokenAndGetUser(authToken);

        // Check if the user is authorized
        if (requestingUser != null && (requestingUser.Username == username || requestingUser.Username == "admin"))
        {
            return true;
        }

        return false; 
    }

// Placeholder method to extract the authentication token from the headers
    private string ExtractAuthToken(HttpHeader[] headers)
    {
        const string authHeaderKey = "Authorization";
        foreach (var header in headers)
        {
            if (header.Name.Equals(authHeaderKey, StringComparison.OrdinalIgnoreCase))
            {
                // Assuming the scheme is "Bearer", strip it off here
                return header.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? header.Value["Bearer ".Length..]
                    : header.Value;
            }
        }

        return null;
    }

// Placeholder method to validate the token and get the user
    private User ValidateTokenAndGetUser(string token)
    {
        var tokenParts = token.Split('-');
        if (tokenParts.Length > 1)
        {
            var extractedUsername = tokenParts[0];
            
                var user = _userRepository.GetUserByUsername(extractedUsername);
                return user;
        }
        return null; // Token format is incorrect or user not found
    }
}
  