using System;
using UnityEngine;

// What the game layer needs from the lobby: a way to send bytes to the other PC and to be
// told when bytes arrive. The host (SocketsUDPServer) and the client (SocketsUDPClient)
// both are a NetLink, so GameNet does not care which one it is talking through.
//
// Both events are raised from Update(), on the main thread: nothing here touches Unity
// from a network thread.
public abstract class NetLink : MonoBehaviour
{
    // data = the datagram, sender = 0 for the host, 1 for the client
    public event Action<byte[], int> GameMessage;

    // The connection is over: the argument says why.
    public event Action<string> LinkClosed;

    public GameNet Game { get; internal set; }

    public abstract bool IsHost { get; }
    public abstract int LocalIndex { get; }

    // Host: sends to every client. Client: sends to the host. Never blocks.
    public abstract void SendGame(byte[] data);

    // Closes the socket and tells the other side we are leaving.
    public abstract void Shutdown();

    protected void RaiseGameMessage(byte[] data, int sender)
    {
        if (GameMessage != null) GameMessage(data, sender);
    }

    protected void RaiseLinkClosed(string reason)
    {
        if (LinkClosed != null) LinkClosed(reason);
    }
}
