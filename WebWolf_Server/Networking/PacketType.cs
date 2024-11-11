namespace WebWolf_Server.Networking;

public enum PacketType : uint
{
    Handshake = 0,
    Broadcast = 1,
    SendTo = 2,
    Server = 3
}