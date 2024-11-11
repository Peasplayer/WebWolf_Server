namespace WebWolf_Server.Networking;

public class HandshakePacket : Packet
{
    public string Name;
    
    public HandshakePacket(string sender, string name) : base(sender)
    {
        Type = PacketType.Handshake;
        Name = name;
    }
}