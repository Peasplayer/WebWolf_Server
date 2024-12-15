using Fleck;
using Newtonsoft.Json;
using WebWolf_Server.Networking;

namespace WebWolf_Server;

class Program
{
    static void Main(string[] args)
    {
        // Startet den Websocket-Server auf dem angegebenen Port
        var port = args.Length > 0 ? int.Parse(args[0]) : 8443;
        var net = new NetworkManager();
        net.StartWebsocket(port);
        
        Console.WriteLine("Press any key to exit the programm");
        Console.ReadKey(true);
    }
}