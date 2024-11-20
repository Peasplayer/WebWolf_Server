namespace WebWolf_Server;

public class PlayerData
{
    public static List<PlayerData> Players = new List<PlayerData>();

    public static PlayerData? GetPlayer(string id)
    {
        return Players.Find(player => player.Id == id);
    }
    
    public string? Name { get; private set; }
    public string Id { get; }
    
    public PlayerData(string? name, string id)
    {
        Name = name;
        Id = id;
    }

    public void SetName(string name)
    {
        if (Name == null)
            Name = name;
    }
}