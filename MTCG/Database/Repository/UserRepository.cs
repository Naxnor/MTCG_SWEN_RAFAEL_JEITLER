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
        
        string selectQuery = "SELECT username, password, name, bio, image, IsAdmin,coins FROM users WHERE username = @username";

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
                            IsAdmin = (bool)reader["IsAdmin"]
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
}