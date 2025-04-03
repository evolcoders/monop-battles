namespace MonopConsole.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TGName { get; set; }
    public string AboutInfo { get; set; }
    public string UserRank { get; set; }
    public DateTime AddedAt { get; set; }
    public ICollection<Game> Games { get; set; }
}
