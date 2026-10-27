using System.Net;

// A client as the host knows it.
public class LobbyPlayer
{
    public string name;
    public EndPoint endpoint;
    public long lastSeenMs;     // last time ANY datagram of this player arrived
    public int pingMs = -1;
    public int index;           // 1, 2, 3...: set when the game starts (the host is 0)
    public bool ready;          // the client confirmed it received START
}
