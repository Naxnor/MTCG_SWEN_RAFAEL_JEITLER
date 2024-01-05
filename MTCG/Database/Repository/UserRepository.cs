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


    public User? GetUserByUsername(string username)
    {
        string selectQuery =
            "SELECT username, password, name, bio, image, IsAdmin,coins FROM users WHERE username = @username";

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
}