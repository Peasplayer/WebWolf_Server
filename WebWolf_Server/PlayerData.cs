using Newtonsoft.Json;
using WebWolf_Server.Networking;

namespace WebWolf_Server;

public class PlayerData
{
    // Liste aller Spieler
    public static List<PlayerData> Players = new List<PlayerData>();

    // Findet einen Spieler anhand seiner ID
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

    // Der Spieler wird bei allen Clients zum Host erklärt
    public void RpcSetHost()
    {
        NetworkManager.Instance.Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SetHost, 
            "{'ID': '" + Id + "'}")));
        
        SetHost();
    }
    
    public void SetHost()
    {
        foreach (var playerData in Players)
        {
            playerData.IsHost = false;
        }

        IsHost = true;
    }
}