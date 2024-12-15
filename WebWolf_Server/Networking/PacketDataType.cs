namespace WebWolf_Server.Networking;

public enum PacketDataType : uint
{
    Join = 0,
    Leave = 1,
    SyncLobby = 2,
    SetHost = 3,
    Disconnect = 4,
}