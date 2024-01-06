namespace MTCG.Models;

public class User
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Bio { get; set; }
    public string Image { get; set; }
    public bool IsAdmin { get; set; }
    public int wins { get; set; }
    public int looses { get; set; }
    public int elo { get; set; }
    public int coins { get; set; }
    public int Id { get; set; }

    // Constructor and any other necessary methods or properties
}