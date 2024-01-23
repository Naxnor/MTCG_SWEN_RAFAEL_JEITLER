using MTCG.Models;
using Npgsql;

namespace MTCG.Database.Repository;

public class UserRepository
{

    public void CreateUser(User user)
    {
        string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";

        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
        {
            try
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@password", user.Password);
                cmd.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }
    }
    public bool DeductCoins(int userId, int amount)
         {
             string updateQuery = "UPDATE users SET Coins = Coins - @amount WHERE id = @userId AND Coins >= @amount";
             using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
             {
                 conn.Open();
                 using (var cmd = new NpgsqlCommand(updateQuery, conn))
                 {
                     cmd.Parameters.AddWithValue("@amount", amount);
                     cmd.Parameters.AddWithValue("@userId", userId);
     
                     int affectedRows = cmd.ExecuteNonQuery();
                     return affectedRows > 0; // true if coins were successfully deducted
                 }
             }
         }

    public void AddCardToUser(int userId, Guid cardId, bool inDeck = false)
    {
        string insertQuery = "INSERT INTO UserCards (CardId, UserId, InDeck) VALUES (@CardId, @UserId, @InDeck)";
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@CardId", cardId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@InDeck", inDeck);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public int GetUserIdByUsername(string username)
    {
        string selectQuery = "SELECT id FROM users WHERE username = @username";

        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(reader.GetOrdinal("id"));
                    }
                }
            }
        }
        return 0; // or handle this case appropriately
    }
    
    public User? GetUserByUsername(string username)
    {
        string selectQuery =
            "SELECT username, password, name, bio, image,coins FROM users WHERE username = @username";

        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(selectQuery, conn))
        {
            cmd.Parameters.AddWithValue("@username", username);
            try
            {
                conn.Open();
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Username = reader["username"].ToString(),
                            Password = reader["password"].ToString(),
                            Name = reader["name"].ToString(),
                            Bio = reader["bio"].ToString(),
                            Image = reader["image"].ToString(),
                        };
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user: {ex.Message}");
                throw;
            }
        }

        return null;
    }
    
    public UserStats GetUserStatsByUsername(string username)
    {
        string selectQuery = "SELECT username, elo, wins, losses FROM users WHERE username = @username";
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@username", username);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserStats
                        {
                            Name = reader.GetString(reader.GetOrdinal("username")),
                            Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                            Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                            WinLoseRatio = CalculateWinLoseRatio(reader.GetInt32(reader.GetOrdinal("wins")), reader.GetInt32(reader.GetOrdinal("losses")))
                        };
                    }
                }
            }
        }
        return null;
    }

    private double CalculateWinLoseRatio(int wins, int losses)
    {
        if (losses == 0)
        {
            if (wins > 0)
            {
                return double.PositiveInfinity; // You can handle this case as per your requirements
            }
            else
            {
                return 0.0; // No wins and no losses, win-lose ratio is 0%
            }
        }
        else
        {
            return (double)wins / losses;
        }
    }

    public IEnumerable<UserStats> GetScoreboard()
    {
        string selectQuery = "SELECT * FROM leaderboard"; // Using the view you've created
        var scoreboard = new List<UserStats>();
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(selectQuery, conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        scoreboard.Add(new UserStats
                        {
                            Name = reader.GetString(reader.GetOrdinal("username")),
                            Elo = reader.GetInt32(reader.GetOrdinal("elo")),
                            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                            Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                            WinLoseRatio = CalculateWinLoseRatio(reader.GetInt32(reader.GetOrdinal("wins")), reader.GetInt32(reader.GetOrdinal("losses")))

                        });
                    }
                }
            }
        }
        return scoreboard;
    }
    
    public bool AuthenticateUser(string formnameUsername, string formnamePassword)
    {
        string selectQuery = "SELECT password FROM users WHERE username = @formnameUsername";
        string storedPassword = null;
        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(selectQuery, conn))
        {
            cmd.Parameters.AddWithValue("@formnameUsername", formnameUsername);
            try
            {
                conn.Open();
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        storedPassword = reader["password"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user: {ex.Message}");
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
        // Check if a password was retrieved and verify it
        if (!string.IsNullOrEmpty(storedPassword))
        {
            return VerifyPassword(formnamePassword, storedPassword);
        }

        return false; // User not found or password does not match
    }
    
    private bool VerifyPassword(string formnamePassword, string? storedPassword)
    {
        if (formnamePassword == storedPassword)
        {
            return true;
        }
        else return false;
        
    }

    public bool UpdateUser(string username, User updatedUserData)
    {
        string updateQuery = "UPDATE users SET name = @name, bio = @bio, image = @image WHERE username = @username";

        using (NpgsqlConnection conn = new NpgsqlConnection(DBManager.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(updateQuery, conn))
        {
            cmd.Parameters.AddWithValue("@name", updatedUserData.Name);
            cmd.Parameters.AddWithValue("@bio", updatedUserData.Bio);
            cmd.Parameters.AddWithValue("@image", updatedUserData.Image);
            cmd.Parameters.AddWithValue("@username", username);

            try
            {
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                return rowsAffected > 0; // Returns true if the update affected at least one row
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user: {ex.Message}");
                // Optionally: log the exception
                return false;
            }
            finally
            {
                conn.Close();
            }
        }
    }

    public bool AddCoins(int userId, int amount)
    {
        string updateQuery = "UPDATE users SET Coins = Coins + @amount WHERE id = @userId";
        using (var conn = new NpgsqlConnection(DBManager.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@userId", userId);

                int affectedRows = cmd.ExecuteNonQuery();
                return affectedRows > 0; // true if coins were successfully added
            }
        }
    }
}