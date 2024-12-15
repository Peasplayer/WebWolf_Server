namespace WebWolf_Server.Networking;

// Packet mit Daten, welche an alle Clients gesendet werden sollen
public class BroadcastPacket : NormalPacket
{
    public BroadcastPacket(string sender, PacketDataType dataType, string data) : base(sender, dataType, data)
    {
        Type = PacketType.Broadcast;
    }
}