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
        var server = new WebSocketServer("ws://127.0.0.1:" + port);
        server.Start(socket =>
        {
            socket.OnError = error =>
            {
                Console.WriteLine("ERROR" + error);
                OnClose(socket);
            };
            socket.OnOpen = () => OnOpen(socket);
            socket.OnClose = () =>
            {
                Console.WriteLine("CLOSE");
                OnClose(socket);
            };
            socket.OnMessage = message => OnMessage(socket, message);
        });
    }
    
    private void OnOpen(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Connected: {0}", clientId);
        socket.Send(JsonConvert.SerializeObject(new HandshakePacket("server", clientId)));
        
        PlayerData.Players.Add(new PlayerData(null, clientId));
        ConnectedClients.Add(clientId, socket);
    }

    private void OnClose(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Disconnected: {0}", clientId);
        
        PlayerData.Players.Remove(PlayerData.GetPlayer(clientId));
        ConnectedClients.Remove(clientId);
        
        Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Leave, "{'ID': '" + clientId +"'}")));
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
                
                PlayerData.GetPlayer(handshake.Sender)?.SetName(handshake.Name);

                var playerList = "";
                foreach (var player in PlayerData.Players)
                {
                    playerList += "{'ID': '" + player.Id + "', 'Name': '" + player.Name + "'},";
                }
                SendTo(handshake.Sender, JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SyncLobby, 
                    "{'Players': [" + playerList + "]}")));
                Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Join, 
                    "{'ID': '" + handshake.Sender + "', 'Name': '" + handshake.Name + "' }")));
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
}