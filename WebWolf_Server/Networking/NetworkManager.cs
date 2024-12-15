using Fleck;
using Newtonsoft.Json;
using WebSocketServer = Fleck.WebSocketServer;

namespace WebWolf_Server.Networking;

public class NetworkManager
{
    // Globale Instanz des NetworkManagers
    public static NetworkManager Instance;
    
    // Liste aller verbundenen Clients
    private Dictionary<string, IWebSocketConnection> ConnectedClients;

    public NetworkManager()
    {
        Instance = this;
        ConnectedClients = new Dictionary<string, IWebSocketConnection>();
    }
    
    // Startet den Websocket-Server auf dem angegebenen Port
    public void StartWebsocket(int port)
    {
        var server = new WebSocketServer("ws://0.0.0.0:" + port);
        // Richtet Ereignisse für den Server ein
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
    
    // Wenn ein Client sich verbindet ...
    private void OnOpen(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Connected: {0}", clientId);

        // ... aber die Lobby voll ist ...
        if (PlayerData.Players.Count >= 20)
        {
            // ... wird der Client getrennt
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
        
        // ... wird der Client begrüßt
        socket.Send(JsonConvert.SerializeObject(new HandshakePacket("server", clientId)));
        
        // ... und in die Liste der verbundenen Clients und aller Spieler eingetragen
        PlayerData.Players.Add(new PlayerData(null, clientId));
        ConnectedClients.Add(clientId, socket);
    }

    // Wenn ein Client die Verbindung trennt ...
    private void OnClose(IWebSocketConnection socket)
    {
        var clientId = socket.ConnectionInfo.Id.ToString();
        Console.WriteLine("Disconnected: {0}", clientId);
        ConnectedClients.Remove(clientId);
        
        var player = PlayerData.GetPlayer(clientId);
        if (player == null)
            return;
        
        // ... wird er aus der Liste der Spieler entfernt
        PlayerData.Players.Remove(player);
        
        // ... und alle anderen Spieler über sein Verlassen informiert
        Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Leave, "{'Id': '" + clientId +"'}")));
        
        // ... falls der Host das Spiel verlässt, wird der nächste Spieler zum Host
        if (player.IsHost && PlayerData.Players.Count > 0)
            PlayerData.Players[0].SetHost();
    }

    // Verarbeitet eingehende Nachrichten
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
                
                // Wenn der Client der einzige Spieler ist, wird er zum Host
                if (PlayerData.Players.Count == 1)
                {
                    PlayerData.GetPlayer(handshake.Sender)?.SetHost();
                }
                
                // Namensdopplungen werden verhindert
                string uniqueName = UniqueName(handshake.Name);
                PlayerData.GetPlayer(handshake.Sender)?.SetName(uniqueName);

                // Der Client wird über alle Spieler informiert
                var playerList = "";
                foreach (var player in PlayerData.Players)
                {
                    playerList += "{'Id': '" + player.Id + "', 'Name': '" + player.Name + "', " +
                                  "'IsHost': " + player.IsHost.ToString().ToLower() + "},";
                }
                SendTo(handshake.Sender, JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.SyncLobby, 
                    "{'Players': [" + playerList + "]}")));
                
                // Alle anderen Spieler werden über den neuen Spieler informiert
                Broadcast(JsonConvert.SerializeObject(new NormalPacket("server", PacketDataType.Join, 
                    "{'Id': '" + handshake.Sender + "', 'Name': '" + uniqueName + "' }")));
                break;
            // Broadcast-Nachricht wird an alle Spieler gesendet
            case PacketType.Broadcast:
                Broadcast(message);
                break;
            // SendTo-Nachricht wird an einen bestimmten Spieler gesendet
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

    // Sendet eine Nachricht an alle verbundenen Clients
    public void Broadcast(string message)
    {
        Console.WriteLine("Broadcasting to " + ConnectedClients.Count + " : " + message);
        for (var i = 0; i < ConnectedClients.Count; i++)
        {
            var client = ConnectedClients.Values.ToArray()[i];
            if (client.IsAvailable)
                client.Send(message);
            // Falls der Client nicht mehr verfügbar ist, wird er entfernt
            else
                ConnectedClients.Remove(ConnectedClients.Keys.ToArray()[i]);
        }
    }

    // Sendet eine Nachricht an einen bestimmten Client
    public void SendTo(string id, string message)
    {
        var client = ConnectedClients[id];
        if (client.IsAvailable)
            client.Send(message);
        else
            ConnectedClients.Remove(id);
    }

    // Berechnet einen einzigartigen Namen
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