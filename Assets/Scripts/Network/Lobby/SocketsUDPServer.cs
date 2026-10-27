using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The host: waiting room, chat and, once the game starts, the other end of every game message.
//
// Rule of thumb: the network thread only puts packets in m_packets. Everything else (the list
// of players, the chat, sending, the UI) happens in Update(), so there are no locks and no
// "Collection was modified".
//
// Text messages (JOIN, CHAT, START...) travel as MsgType.Text. Any other MsgType belongs to the
// game: the host checks that the sender is in the room and hands it to GameNet.
public class SocketsUDPServer : NetLink
{
    [Header("Sala")]
    public TextMeshProUGUI playersText;
    public TMP_InputField inputChat;
    public TextMeshProUGUI chatHistoryText;
    public int port = 9050;
    public string serverName = "Host";

    [Header("Partida")]
    public int maxPlayers = 2;                       // includes the host
    public string gameSceneName = "Level0";
    public string menuSceneName = "MainMenu";
    public TMP_InputField inputKick;                 // field of the Kick button
    public TextMeshProUGUI statusText;               // status and error messages
    public ScrollRect chatScroll;                    // optional: the chat scrolls down by itself
    public bool bonusMessages = true;                // false = only the plain lobby messages

    const int TimeoutMs = 5000;

    readonly ConcurrentQueue<NetPacket> m_packets = new ConcurrentQueue<NetPacket>();
    readonly List<LobbyPlayer> m_players = new List<LobbyPlayer>();
    Socket m_socket;
    volatile bool m_running;
    bool m_gameStarted;
    long m_nextTickMs;
    string m_status = "";

    // ---------------------------------------------------------------- NetLink ---
    public override bool IsHost { get { return true; } }
    public override int LocalIndex { get { return 0; } }

    public override void SendGame(byte[] data)
    {
        foreach (LobbyPlayer p in m_players) SendRaw(p.endpoint, data);
    }

    public override void Shutdown() { Disconnect(); }

    // -------------------------------------------------------------- Life cycle ---
    void Start()
    {
        Application.runInBackground = true;
        StartNetwork();
    }

    public void StartNetwork()
    {
        if (m_running) return;

        try
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_socket.Bind(new IPEndPoint(IPAddress.Any, port));
            NetUtil.DisableUdpConnReset(m_socket);
        }
        catch (Exception e)
        {
            NetUtil.CloseSocket(m_socket);
            m_socket = null;
            SetStatus("No se pudo abrir el puerto " + port + ": " + e.Message);
            return;
        }

        m_running = true;
        Socket socket = m_socket;
        NetUtil.StartThread(delegate { ReceiveLoop(socket); });

        SetStatus("Servidor UDP abierto en el puerto " + port + ". IP para el otro jugador: " + NetUtil.LocalIps());
        RefreshUI();
    }

    void ReceiveLoop(Socket socket)
    {
        byte[] buffer = new byte[NetUtil.MaxPacketSize];

        while (m_running)
        {
            EndPoint from = new IPEndPoint(IPAddress.Any, 0);
            int received;
            try { received = socket.ReceiveFrom(buffer, ref from); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException e)
            {
                if (!m_running) return;
                if (e.SocketErrorCode == SocketError.ConnectionReset) continue;   // a client closed: not our problem
                System.Threading.Thread.Sleep(10);
                continue;
            }

            if (received <= 0) continue;

            byte[] data = new byte[received];
            Array.Copy(buffer, data, received);
            m_packets.Enqueue(new NetPacket { from = from, data = data, ms = NetUtil.NowMs });
        }
    }

    public void Disconnect()
    {
        if (!m_running) return;
        Broadcast("CLOSED:");
        m_running = false;
        m_players.Clear();
        NetUtil.CloseSocket(m_socket);
        m_socket = null;
    }

    // Button "Volver" of the create-game scene.
    public void CloseRoom()
    {
        Disconnect();
        NetSession.Reset();
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy() { Disconnect(); }
    void OnApplicationQuit() { Disconnect(); }

    void Update()
    {
        NetPacket packet;
        int guard = 0;
        while (guard++ < 2000 && m_packets.TryDequeue(out packet)) HandlePacket(packet);

        if (!m_running) return;

        long now = NetUtil.NowMs;
        CheckTimeouts(now);

        if (now >= m_nextTickMs)
        {
            m_nextTickMs = now + 1000;
            SecondTick(now);
        }
    }

    // ------------------------------------------------------------ Receiving ---
    void HandlePacket(NetPacket packet)
    {
        if (!m_running || packet.data.Length == 0) return;

        if (Serializer.TypeOf(packet.data) == MsgType.Text)
        {
            string text;
            try { text = Serializer.ReadText(packet.data); }
            catch (Exception) { return; }          // garbage: drop it, never take the room down
            HandleText(packet, text);
            return;
        }

        LobbyPlayer player = FindPlayer(packet.from);
        if (player == null)
        {
            SendText(packet.from, "KICK:");        // not in the room any more (timeout, kicked)
            return;
        }

        player.lastSeenMs = packet.ms;
        if (m_gameStarted) RaiseGameMessage(packet.data, player.index);
    }

    void HandleText(NetPacket packet, string text)
    {
        LobbyPlayer player = FindPlayer(packet.from);

        if (text.StartsWith("JOIN:"))
        {
            if (player != null)
            {
                player.lastSeenMs = packet.ms;     // repeated JOIN (the answer got lost): send the info again
                SendRoomInfoTo(player.endpoint);
            }
            else
            {
                HandleJoin(packet.from, text.Substring(5), packet.ms);
            }
            return;
        }

        if (player == null)
        {
            SendText(packet.from, "KICK:");
            return;
        }

        player.lastSeenMs = packet.ms;

        if (text.StartsWith("PING:")) return;

        if (text.StartsWith("LEAVE:"))
        {
            RemovePlayer(player, "ha salido");
        }
        else if (text.StartsWith("READY:"))
        {
            player.ready = true;
        }
        else if (text.StartsWith("CHAT:"))
        {
            string msg = text.Substring(5).Replace("\r", " ").Replace("\n", " ").Trim();
            if (msg.Length == 0) return;
            if (msg.Length > 300) msg = msg.Substring(0, 300);

            string line = player.name + ": " + msg;
            AddChatLine(line);
            Broadcast("CHAT:" + line);
        }
        else if (text.StartsWith("SPONG:"))
        {
            long sent;
            if (long.TryParse(text.Substring(6), out sent))
                player.pingMs = (int)Math.Max(0, Math.Min(packet.ms - sent, 9999));
        }
    }

    void HandleJoin(EndPoint from, string rawName, long ms)
    {
        if (m_gameStarted || m_players.Count + 1 >= maxPlayers)   // +1 = the host
        {
            SendText(from, "FULL:");
            return;
        }

        LobbyPlayer player = new LobbyPlayer();
        player.name = UniqueName(NetUtil.CleanName(rawName, 16));
        player.endpoint = from;
        player.lastSeenMs = ms;
        m_players.Add(player);

        string joined = "* " + player.name + " se ha unido";
        AddChatLine(joined);
        Broadcast("CHAT:" + joined);
        BroadcastPlayerList();
    }

    void RemovePlayer(LobbyPlayer player, string reason)
    {
        m_players.Remove(player);

        string line = "* " + player.name + " " + reason;
        AddChatLine(line);
        Broadcast("CHAT:" + line);
        BroadcastPlayerList();

        // In the middle of a game the other player is gone: GameNet takes us back to the menu.
        if (m_gameStarted) RaiseLinkClosed(player.name + " " + reason);
    }

    void CheckTimeouts(long now)
    {
        for (int i = m_players.Count - 1; i >= 0; i--)
        {
            if (now - m_players[i].lastSeenMs <= TimeoutMs) continue;

            RemovePlayer(m_players[i], "se ha desconectado (timeout)");
            if (!m_running) return;     // GameNet closed the connection in reaction: stop here
        }
    }

    // Once per second: ping of every player and, until the clients confirm it, START again.
    void SecondTick(long now)
    {
        if (m_gameStarted)
        {
            foreach (LobbyPlayer p in m_players)
            {
                if (!p.ready) SendText(p.endpoint, "START:" + p.index);
            }
        }

        if (bonusMessages && m_players.Count > 0)
        {
            StringBuilder sb = new StringBuilder("PINGS:");
            for (int i = 0; i < m_players.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(m_players[i].name).Append('=').Append(m_players[i].pingMs);
            }
            Broadcast(sb.ToString());
            Broadcast("SPING:" + now);
        }
        RefreshUI();
    }

    // --------------------------------------------------------------- Sending ---
    void SendRaw(EndPoint to, byte[] data)
    {
        Socket socket = m_socket;
        if (socket == null || to == null) return;
        try { socket.SendTo(data, to); }
        catch (Exception) { }
    }

    void SendText(EndPoint to, string text) { SendRaw(to, Serializer.Text(text)); }

    void Broadcast(string text)
    {
        byte[] data = Serializer.Text(text);
        foreach (LobbyPlayer p in m_players) SendRaw(p.endpoint, data);
    }

    void BroadcastPlayerList()
    {
        if (bonusMessages) Broadcast("MAX:" + maxPlayers);
        Broadcast("PLAYERS:" + string.Join(",", PlayerNames()));
        RefreshUI();
    }

    void SendRoomInfoTo(EndPoint to)
    {
        if (bonusMessages) SendText(to, "MAX:" + maxPlayers);
        SendText(to, "PLAYERS:" + string.Join(",", PlayerNames()));
    }

    string[] PlayerNames()
    {
        string[] names = new string[m_players.Count + 1];
        names[0] = HostLabel;                       // the host plays too: it is in the list
        for (int i = 0; i < m_players.Count; i++) names[i + 1] = m_players[i].name;
        return names;
    }

    // ---------------------------------------------------- Buttons of the host ---
    public void SendChatMessage()
    {
        if (inputChat == null || string.IsNullOrEmpty(inputChat.text)) return;

        string text = inputChat.text.Trim();
        inputChat.text = "";
        if (text.Length == 0) return;

        string line = HostLabel + ": " + text;
        AddChatLine(line);
        Broadcast("CHAT:" + line);
    }

    public void KickPlayer()
    {
        if (inputKick == null) return;
        KickByName(inputKick.text);
        inputKick.text = "";
    }

    public bool KickByName(string name)
    {
        name = (name ?? "").Trim();
        LobbyPlayer target = null;
        foreach (LobbyPlayer p in m_players)
        {
            if (string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase)) target = p;
        }

        if (target == null)
        {
            SetStatus("No hay ningun jugador llamado '" + name + "'");
            return false;
        }

        SendText(target.endpoint, "KICK:");
        RemovePlayer(target, "ha sido expulsado");
        return true;
    }

    // Button "Iniciar partida".
    public void StartGame()
    {
        if (!m_running || m_gameStarted) return;

        if (m_players.Count == 0)
        {
            SetStatus("Necesitas que se una otro jugador para empezar");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SetStatus("La escena '" + gameSceneName + "' no esta en Build Settings");
            return;
        }

        m_gameStarted = true;

        NetSession.IsOnline = true;
        NetSession.IsHost = true;
        NetSession.LocalIndex = 0;
        NetSession.LocalName = HostLabel;
        NetSession.PlayerNames = PlayerNames();

        for (int i = 0; i < m_players.Count; i++)
        {
            m_players[i].index = i + 1;
            SendText(m_players[i].endpoint, "START:" + m_players[i].index);   // repeated each second until READY
        }

        DontDestroyOnLoad(gameObject);     // the socket stays open in the game scene
        GameNet.Create(this);
        SceneManager.LoadScene(gameSceneName);
    }

    // ------------------------------------------------------------- Utilities ---
    string HostLabel { get { return NetUtil.CleanName(serverName, 16) + " (Host)"; } }

    LobbyPlayer FindPlayer(EndPoint endpoint)
    {
        foreach (LobbyPlayer p in m_players)
        {
            if (p.endpoint.Equals(endpoint)) return p;
        }
        return null;
    }

    string UniqueName(string baseName)
    {
        string name = baseName;
        int n = 2;
        while (NameTaken(name))
        {
            name = baseName + " (" + n + ")";
            n++;
        }
        return name;
    }

    bool NameTaken(string name)
    {
        if (string.Equals(name, HostLabel, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (LobbyPlayer p in m_players)
        {
            if (string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    void AddChatLine(string line)
    {
        if (chatHistoryText == null) return;
        chatHistoryText.text += "\n" + line;
        if (chatScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }

    void RefreshUI()
    {
        if (playersText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.Append("Sala de espera (").Append(m_players.Count + 1).Append('/').Append(maxPlayers).Append("):\n");
        sb.Append(HostLabel);
        foreach (LobbyPlayer p in m_players)
        {
            sb.Append('\n').Append(p.name);
            if (p.pingMs >= 0) sb.Append(" - ").Append(p.pingMs).Append(" ms");
        }
        playersText.text = sb.ToString();
    }

    void SetStatus(string text)
    {
        m_status = text;
        if (statusText != null) statusText.text = text;
        Debug.Log(text);
    }

    // No Status Text assigned: show the message in a box (useful in the .exe).
    void OnGUI()
    {
        if (statusText == null && !m_gameStarted) StatusBox.Draw(m_status);
    }
}
