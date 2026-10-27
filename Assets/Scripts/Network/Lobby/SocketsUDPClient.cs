using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The client: join screen, waiting room and chat in one scene, and, once the host starts the
// game, the other end of every game message.
//
// UDP does not guarantee anything, so the JOIN is repeated until the host answers, and a PING
// goes out every second so the host knows we are still here.
public class SocketsUDPClient : NetLink
{
    public TMP_InputField inputName;
    public TMP_InputField inputIP;
    public TMP_InputField inputChat;
    public TextMeshProUGUI playersListText;
    public TextMeshProUGUI chatHistoryText;
    public int port = 9050;

    [Header("Paneles")]
    public GameObject joinPanel;                     // name + IP + Join button
    public GameObject roomPanel;                     // players + chat + Leave button

    [Header("Partida")]
    public TextMeshProUGUI statusText;               // status and error messages
    public ScrollRect chatScroll;                    // optional: the chat scrolls down by itself
    public string gameSceneName = "Level0";
    public string joinSceneName = "S_JoinGame_UDP";
    public string menuSceneName = "MainMenu";

    // Long on purpose: the host cannot send while it loads a scene (its main thread is busy).
    const int HostTimeoutMs = 10000;

    readonly ConcurrentQueue<NetPacket> m_inbox = new ConcurrentQueue<NetPacket>();
    readonly Dictionary<string, int> m_pings = new Dictionary<string, int>();
    Socket m_socket;
    EndPoint m_serverEndPoint;
    volatile bool m_running;
    volatile bool m_confirmed;          // the host already answered us
    volatile bool m_lost;               // the receive thread died without us asking
    bool m_gameStarted;
    int m_localIndex;
    string m_playerName = "";
    string[] m_names = new string[0];
    int m_maxPlayers;
    long m_lastJoinMs;
    int m_joinAttempts;
    long m_lastHeardMs;
    bool m_hostHeartbeat;               // the host sends SPING: from then on we watch for its silence
    string m_status = "";

    // ---------------------------------------------------------------- NetLink ---
    public override bool IsHost { get { return false; } }
    public override int LocalIndex { get { return m_localIndex; } }

    public override void SendGame(byte[] data) { SendRaw(data); }

    public override void Shutdown()
    {
        if (!m_running) return;
        SendText("LEAVE:");
        m_running = false;
        CloseConnection();
    }

    // -------------------------------------------------------------- Life cycle ---
    void Start()
    {
        Application.runInBackground = true;
        ShowRoom(false);

        if (!string.IsNullOrEmpty(NetSession.LastMessage))
        {
            SetStatus(NetSession.LastMessage);
            NetSession.LastMessage = "";
        }
    }

    void ShowRoom(bool inRoom)
    {
        if (joinPanel != null) joinPanel.SetActive(!inRoom);
        if (roomPanel != null) roomPanel.SetActive(inRoom);
    }

    // Button "Unirse".
    public void ConnectToServer()
    {
        if (m_running) return;

        string ip = inputIP != null ? inputIP.text.Trim() : "";
        string rawName = inputName != null ? inputName.text.Trim() : "";

        if (ip.Length == 0 || rawName.Length == 0)
        {
            SetStatus("Escribe tu nombre y la IP del servidor");
            return;
        }

        if (ip.ToLower() == "localhost") ip = "127.0.0.1";

        IPAddress address;
        if (!IPAddress.TryParse(ip, out address))
        {
            SetStatus("La IP '" + ip + "' no es valida (ejemplo: 192.168.1.20)");
            return;
        }

        try
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // On Windows a UDP socket must be bound BEFORE it receives. Without this the receive
            // thread dies at once and the host removes us by timeout.
            m_socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            NetUtil.DisableUdpConnReset(m_socket);
            m_serverEndPoint = new IPEndPoint(address, port);
        }
        catch (Exception e)
        {
            SetStatus("No se pudo crear el socket: " + e.Message);
            return;
        }

        m_playerName = NetUtil.CleanName(rawName, 16);
        m_confirmed = false;
        m_lost = false;
        m_joinAttempts = 0;
        m_lastJoinMs = -1000;           // the first JOIN goes out in the next Update
        m_hostHeartbeat = false;
        m_lastHeardMs = NetUtil.NowMs;
        m_running = true;
        SetStatus("Conectando a " + ip + ":" + port + " ...");

        Socket socket = m_socket;
        NetUtil.StartThread(delegate { ReceiveLoop(socket); });
        NetUtil.StartThread(PingLoop);
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
                if (e.SocketErrorCode == SocketError.ConnectionReset) continue;
                m_lost = true;
                return;
            }
            catch (Exception)
            {
                if (m_running) m_lost = true;     // the thread must not die in silence
                return;
            }

            if (received <= 0) continue;

            byte[] data = new byte[received];
            Array.Copy(buffer, data, received);
            m_inbox.Enqueue(new NetPacket { from = from, data = data, ms = NetUtil.NowMs });
        }
    }

    void PingLoop()
    {
        while (m_running)
        {
            if (m_confirmed) SendText("PING:");
            Thread.Sleep(1000);
        }
    }

    void Update()
    {
        NetPacket packet;
        int guard = 0;
        while (guard++ < 2000 && m_inbox.TryDequeue(out packet))
        {
            if (m_running) HandlePacket(packet);
        }

        if (!m_running) return;

        if (m_lost)
        {
            m_lost = false;
            Disconnected("Se ha perdido la conexion con el servidor", false);
            return;
        }

        if (m_hostHeartbeat && NetUtil.NowMs - m_lastHeardMs > HostTimeoutMs)
        {
            Disconnected("El servidor ha dejado de responder", false);
            return;
        }

        if (!m_confirmed) RetryJoin();
    }

    void RetryJoin()
    {
        long now = NetUtil.NowMs;
        if (now - m_lastJoinMs < 1000) return;

        if (m_joinAttempts >= 5)
        {
            Disconnected("El servidor no responde. Revisa la IP, el firewall (UDP " + port + ") y que ambos esteis en la misma red", false);
            return;
        }

        m_joinAttempts++;
        m_lastJoinMs = now;
        SendText("JOIN:" + m_playerName);
    }

    // ------------------------------------------------------------ Receiving ---
    void HandlePacket(NetPacket packet)
    {
        if (packet.data.Length == 0) return;

        // The timestamp is the one of the network thread: a long frame (loading a scene)
        // does not look like "the host stopped talking".
        m_lastHeardMs = packet.ms;

        if (!m_confirmed)
        {
            m_confirmed = true;
            SetStatus("Conectado");
            ShowRoom(true);
        }

        if (Serializer.TypeOf(packet.data) != MsgType.Text)
        {
            if (m_gameStarted) RaiseGameMessage(packet.data, 0);
            return;
        }

        string text;
        try { text = Serializer.ReadText(packet.data); }
        catch (Exception) { return; }

        HandleText(text);
    }

    void HandleText(string text)
    {
        if (text.StartsWith("PLAYERS:"))
        {
            m_names = ParseList(text.Substring(8));
            RefreshPlayers();
        }
        else if (text.StartsWith("PINGS:"))
        {
            foreach (string entry in text.Substring(6).Split(','))
            {
                int eq = entry.LastIndexOf('=');
                int value;
                if (eq > 0 && int.TryParse(entry.Substring(eq + 1), out value))
                    m_pings[entry.Substring(0, eq).Trim()] = value;
            }
            RefreshPlayers();
        }
        else if (text.StartsWith("MAX:"))
        {
            int max;
            if (int.TryParse(text.Substring(4), out max)) m_maxPlayers = max;
            RefreshPlayers();
        }
        else if (text.StartsWith("CHAT:"))
        {
            AddChat(text.Substring(5));
        }
        else if (text.StartsWith("SPING:"))
        {
            m_hostHeartbeat = true;
            SendText("SPONG:" + text.Substring(6));
        }
        else if (text.StartsWith("START:"))
        {
            int index;
            if (int.TryParse(text.Substring(6), out index)) OnStart(index);
        }
        else if (text.StartsWith("KICK:"))
        {
            Disconnected("Has sido expulsado de la sala", false);
        }
        else if (text.StartsWith("CLOSED:"))
        {
            Disconnected("El host ha cerrado la sala", false);
        }
        else if (text.StartsWith("FULL:"))
        {
            Disconnected("La sala esta llena o la partida ya ha empezado", false);
        }
    }

    // ------------------------------------------------------------ Start game ---
    void OnStart(int index)
    {
        SendText("READY:");                 // always: the host repeats START until it hears this
        if (m_gameStarted) return;

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SetStatus("El host ha iniciado la partida pero la escena '" + gameSceneName + "' no esta en Build Settings");
            return;
        }

        m_gameStarted = true;
        m_localIndex = index;

        NetSession.IsOnline = true;
        NetSession.IsHost = false;
        NetSession.LocalIndex = index;
        NetSession.LocalName = m_playerName;
        NetSession.PlayerNames = m_names;

        DontDestroyOnLoad(gameObject);      // the socket stays open in the game scene
        GameNet.Create(this);
        SceneManager.LoadScene(gameSceneName);
    }

    // --------------------------------------------------------------- Leaving ---
    // Button "Salir" of the room panel.
    public void LeaveRoom() { Disconnected(null, true); }

    // Button "Volver" of the join panel: back to the main menu.
    public void BackToMenu()
    {
        Shutdown();
        NetSession.Reset();
        SceneManager.LoadScene(menuSceneName);
    }

    void Disconnected(string message, bool sendLeave)
    {
        if (!m_running) return;
        if (sendLeave) SendText("LEAVE:");

        m_running = false;
        CloseConnection();

        m_names = new string[0];
        m_pings.Clear();
        m_maxPlayers = 0;
        m_hostHeartbeat = false;

        // In a game GameNet shows the message and takes us to the menu.
        if (m_gameStarted)
        {
            RaiseLinkClosed(message ?? "Has salido de la partida");
            return;
        }

        NetSession.LastMessage = message ?? "";
        if (playersListText != null) playersListText.text = "";
        ShowRoom(false);

        if (SceneManager.GetActiveScene().name != joinSceneName && Application.CanStreamedLevelBeLoaded(joinSceneName))
        {
            SceneManager.LoadScene(joinSceneName);
            Destroy(gameObject);
            return;
        }
        SetStatus(string.IsNullOrEmpty(message) ? "Has salido de la sala" : message);
    }

    void CloseConnection()
    {
        NetUtil.CloseSocket(m_socket);
        m_socket = null;
    }

    void OnDestroy()
    {
        m_running = false;
        CloseConnection();
    }

    void OnApplicationQuit()
    {
        if (m_running) SendText("LEAVE:");
        m_running = false;
        CloseConnection();
    }

    // --------------------------------------------------------------- Sending ---
    void SendRaw(byte[] data)
    {
        Socket socket = m_socket;
        EndPoint server = m_serverEndPoint;
        if (socket == null || server == null) return;

        try { socket.SendTo(data, server); }
        catch (Exception) { }
    }

    void SendText(string text) { SendRaw(Serializer.Text(text)); }

    // Button "Enviar".
    public void SendChatMessage()
    {
        if (inputChat == null || string.IsNullOrEmpty(inputChat.text)) return;

        string text = inputChat.text.Trim();
        inputChat.text = "";
        if (text.Length == 0) return;
        SendText("CHAT:" + text);
    }

    // -------------------------------------------------------------------- UI ---
    static string[] ParseList(string s)
    {
        if (s.Trim().Length == 0) return new string[0];
        string[] parts = s.Split(',');
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
        return parts;
    }

    void AddChat(string line)
    {
        if (chatHistoryText == null) return;
        chatHistoryText.text += "\n" + line;
        if (chatScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }

    void RefreshPlayers()
    {
        if (playersListText == null) return;

        StringBuilder sb = new StringBuilder("Jugadores");
        if (m_maxPlayers > 0) sb.Append(" (").Append(m_names.Length).Append('/').Append(m_maxPlayers).Append(')');
        sb.Append(':');

        foreach (string name in m_names)
        {
            sb.Append('\n').Append(name);
            int ping;
            if (m_pings.TryGetValue(name, out ping) && ping >= 0) sb.Append(" - ").Append(ping).Append(" ms");
        }
        playersListText.text = sb.ToString();
    }

    void SetStatus(string text)
    {
        m_status = text;
        if (statusText != null) statusText.text = text;
        Debug.Log(text);
    }

    void OnGUI()
    {
        if (statusText == null && !m_gameStarted) StatusBox.Draw(m_status);
    }
}
