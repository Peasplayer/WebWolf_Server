namespace WebWolf_Server.Networking;

// Normales Datenpaket
public class NormalPacket : Packet
{
    public PacketDataType DataType;
    public string Data;
    
    public NormalPacket(string sender, PacketDataType dataType, string data) : base(sender)
    {
        Type = PacketType.Server;
        DataType = dataType;
        Data = data;
    }
}