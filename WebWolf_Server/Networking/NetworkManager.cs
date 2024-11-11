using Fleck;
using Newtonsoft.Json;
using WebSocketServer = Fleck.WebSocketServer;

namespace WebWolf_Server.Networking;

public class NetworkManager
{
    public static NetworkManager Instance;
    
    private Dictionary<Guid, IWebSocketConnection> ConnectedClients;

    public NetworkManager()
    {
        Instance = this;
        ConnectedClients = new Dictionary<Guid, IWebSocketConnection>();
    }
    
    public void StartWebsocket(int port)
    {
        var server = new WebSocketServer("ws://127.0.0.1:" + port);
        server.Start(socket =>
        {
            socket.OnOpen = () => OnOpen(socket);
            socket.OnClose = () => OnClose(socket);
            socket.OnMessage = message => OnMessage(socket, message);
        });
    }
    
    private void OnOpen(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id;
        Console.WriteLine("Connected: {0}", clientId);
        socket.Send(JsonConvert.SerializeObject(new HandshakePacket("server", clientId.ToString())));

        var playerList = "";
        foreach (var (id, name) in PlayerManager.Players)
        {
            playerList += "{'ID': '" + id + "', 'Name': '" + name + "'},";
        }
        socket.Send(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SyncLobby, 
            "{'Players': [" + playerList + "]}")));
        
        PlayerManager.Players.Add(clientId.ToString(), "");
        ConnectedClients.Add(clientId, socket);
    }

    private void OnClose(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id;
        Console.WriteLine("Disconnected: {0}", clientId);
        PlayerManager.Players.Remove(clientId.ToString());
        ConnectedClients.Remove(clientId);
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
                
                PlayerManager.Players[handshake.Sender] = handshake.Name;
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

                var receiver = Guid.Parse(parsedPacket.Receiver);
                if (ConnectedClients.ContainsKey(receiver))
                    SendTo(receiver, message);
                break;
        }
    }

    public void Broadcast(string message)
    {
        for (var i = 0; i < ConnectedClients.Count; i++)
        {
            var client = ConnectedClients.Values.ToArray()[i];
            if (client.IsAvailable)
                client.Send(message);
            else
                ConnectedClients.Remove(ConnectedClients.Keys.ToArray()[i]);
        }
    }

    public void SendTo(Guid id, string message)
    {
        var client = ConnectedClients[id];
        if (client.IsAvailable)
            client.Send(message);
        else
            ConnectedClients.Remove(id);
    }
}