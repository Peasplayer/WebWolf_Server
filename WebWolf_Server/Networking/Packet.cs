namespace WebWolf_Server.Networking;

// Paket Basis-Klasse
public class Packet
{
    public PacketType Type;
    public string Sender;

    public Packet(string sender)
    {
        Sender = sender;
    }
}