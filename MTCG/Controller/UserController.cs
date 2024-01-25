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
                var responseContent = new 
                {
                    Token = token,
                    Message = "User login successful"
                };

                e.Reply(200, JsonConvert.SerializeObject(responseContent));
            }
            else
            {
                e.Reply(401, "Unauthorized: Incorrect username/password");
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
        // Extract 'username' from the path
        var segments = e.Path.Trim('/').Split('/');
        if (segments.Length < 2 || string.IsNullOrEmpty(segments[1]))
        {
            e.Reply(400, "Bad Request: Username is required");
            return;
        }
        string username = segments[1];

        if (!IsAuthorized(e, username))
        {
            e.Reply(401, "Unauthorized! Access token is invalid");
            return;
        }

        try
        {
            var user = _userRepository.GetUserByUsername(username);
            if (user != null)
            {
                var userData = new UserDTO
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

    public void UpdateUserData(HttpSvrEventArgs e)
    {
        // Extract 'username' from the path
        var segments = e.Path.Trim('/').Split('/');
        if (segments.Length < 2 || string.IsNullOrEmpty(segments[1]))
        {
            e.Reply(400, "Bad Request: Username is required");
            return;
        }
        string username = segments[1];

        // Check for Token if false return 
        if (!IsAuthorized(e, username))
        {
            e.Reply(401, "Unauthorized! Access token is invalid");
            return;
        }

        try 
        {
            var updatedUserData = JsonConvert.DeserializeObject<User>(e.Payload);
            if (updatedUserData == null) // if the required fields are empty
            {
                e.Reply(400, "Invalid request");
                return;
            }
        
            bool updateSuccessful = _userRepository.UpdateUser(username, updatedUserData);
            if (updateSuccessful)
            {
                e.Reply(200, "User profile updated successfully");
            }
            else
            {
                e.Reply(404, "User not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            e.Reply(500, "Internal server error");
        }
    }


    public void GetUserStats(HttpSvrEventArgs e)
    {
        var authToken = ExtractAuthToken(e.Headers);

        if (string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Unauthorized! Access token is missing");
            return ;
        }
        
        var username = GetUsernameFromToken(e); // Extract username from token
        
        if (string.IsNullOrEmpty(username))
        {
            e.Reply(401, "Unauthorized: Missing or invalid token");
            return;
        }

        var userStats = _userRepository.GetUserStatsByUsername(username);
        if (userStats != null)
        {
            var jsonResponse = JsonConvert.SerializeObject(userStats, Formatting.Indented);
            e.Reply(200, jsonResponse);
        }
        else
        {
            e.Reply(404, "User stats not found");
        }
    }

    public void GetScoreboard(HttpSvrEventArgs e)
    {
        var authToken = ExtractAuthToken(e.Headers);

        if (string.IsNullOrEmpty(authToken))
        {
            e.Reply(401, "Unauthorized! Access token is missing");
            return ;
        }
        
        var username = GetUsernameFromToken(e); // Extract username from token
        
        if (string.IsNullOrEmpty(username))
        {
            e.Reply(401, "Unauthorized: Missing or invalid token");
            return;
        }
        
        var scoreboard = _userRepository.GetScoreboard(); 
        if (scoreboard.Any())
        {
            var jsonResponse = JsonConvert.SerializeObject(scoreboard, Formatting.Indented);
            e.Reply(200, jsonResponse);
        }
        else
        {
            e.Reply(404, "Scoreboard is empty");
        }
    }
    
    private bool IsAuthorized(HttpSvrEventArgs e, string username)
    {
        try
        {
            var authToken = ExtractAuthToken(e.Headers);

            if (string.IsNullOrEmpty(authToken))
            {
                e.Reply(401, "Unauthorized! Access token is missing");
                return false;
            }

            var requestingUser = ValidateTokenAndGetUser(authToken);

            if (requestingUser != null && (requestingUser.Username == username || requestingUser.Username == "admin"))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in authorization: {ex.Message}");
            e.Reply(500, "Internal Server Error");
            return false;
        }
    }
    
    public static string ExtractAuthToken(HttpHeader[] headers) // static to make it reusable in other parts
    {
        const string authHeaderKey = "Authorization"; 
        if (headers == null) 
        {
            return null;
        }

        foreach (var header in headers)
        {
            if (header.Name.Equals(authHeaderKey, StringComparison.OrdinalIgnoreCase))
            {
                var headerValue = header.Value;
                return headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? headerValue["Bearer ".Length..]
                    : headerValue;
            }
        }

        return null;
    }
    
    public  User ValidateTokenAndGetUser(string token)
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

    public static string GetUsernameFromToken(HttpSvrEventArgs e)
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
    
}
  



