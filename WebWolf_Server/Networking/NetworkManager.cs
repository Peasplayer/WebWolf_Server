using Fleck;
using Newtonsoft.Json;
using WebSocketServer = Fleck.WebSocketServer;

namespace WebWolf_Server.Networking;

public class NetworkManager
{
    public static NetworkManager Instance;
    
    private Dictionary<string, IWebSocketConnection> ConnectedClients;

    public NetworkManager()
    {
        Instance = this;
        ConnectedClients = new Dictionary<string, IWebSocketConnection>();
    }
    
    public void StartWebsocket(int port)
    {
        var server = new WebSocketServer("ws://0.0.0.0:" + port);
        server.Start(socket =>
        {
            socket.OnError = error =>
            {
                Console.WriteLine("Error: " + error.Message);
                OnClose(socket);
            };
            socket.OnOpen = () => OnOpen(socket);
            socket.OnClose = () =>
            {
                OnClose(socket);
            };
            socket.OnMessage = message => OnMessage(socket, message);
        });
    }
    
    private void OnOpen(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Connected: {0}", clientId);

        if (PlayerData.Players.Count >= 20)
        {
            socket.Send(JsonConvert.SerializeObject(new SendToPacket("server", PacketDataType.Disconnect,
                "Lobby ist voll!", clientId)));
            Task.Run(() =>
            {
                Task.Delay(1000).Wait();
                if (socket.IsAvailable)
                {
                    socket.Close(0);
                }
            });
            return;
        }
        
        socket.Send(JsonConvert.SerializeObject(new HandshakePacket("server", clientId)));
        
        PlayerData.Players.Add(new PlayerData(null, clientId));
        ConnectedClients.Add(clientId, socket);
    }

    private void OnClose(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Disconnected: {0}", clientId);
        ConnectedClients.Remove(clientId);
        
        var player = PlayerData.GetPlayer(clientId);
        if (player == null)
            return;
        
        PlayerData.Players.Remove(player);
        
        Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Leave, "{'Id': '" + clientId +"'}")));
        
        if (player.IsHost && PlayerData.Players.Count > 0)
            PlayerData.Players[0].SetHost();
    }

    private void OnMessage(IWebSocketConnection socket, string message)
    {
        Console.WriteLine("Message: {0}", message);
        var packet = JsonConvert.DeserializeObject<Packet>(message);    
        if (packet == null)
            return;
            
        switch (packet.Type)
        {
            case PacketType.Handshake:
                var handshake = JsonConvert.DeserializeObject<HandshakePacket>(message);
                if (handshake == null)
                    return;
                
                if (PlayerData.Players.Count == 1)
                {
                    PlayerData.GetPlayer(handshake.Sender)?.SetHost();
                }
                
                string uniqueName = UniqueName(handshake.Name);
                PlayerData.GetPlayer(handshake.Sender)?.SetName(uniqueName);

                var playerList = "";
                foreach (var player in PlayerData.Players)
                {
                    playerList += "{'Id': '" + player.Id + "', 'Name': '" + player.Name + "', " +
                                  "'IsHost': " + player.IsHost.ToString().ToLower() + "},";
                }
                SendTo(handshake.Sender, JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SyncLobby, 
                    "{'Players': [" + playerList + "]}")));
                Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Join, 
                    "{'Id': '" + handshake.Sender + "', 'Name': '" + uniqueName + "' }")));
                break;
            case PacketType.Broadcast:
                Broadcast(message);
                break;
            case PacketType.SendTo:
                var parsedPacket = JsonConvert.DeserializeObject<SendToPacket>(message);
                if (parsedPacket == null)
                    return;

                var receiver = parsedPacket.Receiver;
                if (ConnectedClients.ContainsKey(receiver))
                    SendTo(receiver, message);
                break;
        }
    }

    public void Broadcast(string message)
    {
        Console.WriteLine("Broadcasting to " + ConnectedClients.Count + " : " + message);
        for (var i = 0; i < ConnectedClients.Count; i++)
        {
            var client = ConnectedClients.Values.ToArray()[i];
            if (client.IsAvailable)
                client.Send(message);
            else
                ConnectedClients.Remove(ConnectedClients.Keys.ToArray()[i]);
        }
    }

    public void SendTo(string id, string message)
    {
        var client = ConnectedClients[id];
        if (client.IsAvailable)
            client.Send(message);
        else
            ConnectedClients.Remove(id);
    }

    private string UniqueName(string name)
    {
        var vorhandeneNamen = PlayerData.Players.Select(p => p.Name).ToList();
        if (!vorhandeneNamen.Contains(name))
            return name;

        int count = 2;
        string newName = name;
        while (vorhandeneNamen.Contains(newName))
        {
            newName = name + count;
            count++;
        }

        return newName;

    }
}