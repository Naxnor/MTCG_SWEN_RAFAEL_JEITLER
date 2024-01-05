
namespace MTCG.Models;

public class UserDTO
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
    // Include other properties as needed, but exclude sensitive data like passwords
}
