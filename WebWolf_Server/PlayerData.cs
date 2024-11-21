using Newtonsoft.Json;
using WebWolf_Server.Networking;

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
    public bool IsHost { get; private set; }
    
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
    
    public void SetHost()
    {
        foreach (var playerData in Players)
        {
            playerData.IsHost = false;
        }

        IsHost = true;
        NetworkManager.Instance.Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SetHost, 
            "{'ID': '" + Id + "'}")));
    }
}