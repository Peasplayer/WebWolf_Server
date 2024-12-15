namespace WebWolf_Server.Networking;

// Paket zum Senden von Daten an einen bestimmten Client
public class SendToPacket : NormalPacket
{
    public string Receiver;
    public SendToPacket(string sender, PacketDataType dataType, string data, string receiver) : base(sender, dataType, data)
    {
        Type = PacketType.SendTo;
        Receiver = receiver;
    }
}