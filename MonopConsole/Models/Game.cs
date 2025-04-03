namespace MonopConsole.Models;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Finished { get; set; }
    public string GameState { get; set; }

    public ICollection<User> Users { get; set; }
}
