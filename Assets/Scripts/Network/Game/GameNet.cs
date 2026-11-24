using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Network of the game scenes, created by the lobby when the host presses Start.
//   player state: unreliable, ~30 per second, each PC sends its own player
//   replication:  unreliable, 20 per second, host to client (world state)
//   events:       reliable, resent until acknowledged
// Only the host changes scenes; the client asks with a SceneRequest.
public class GameNet : MonoBehaviour
{
    public static GameNet Instance { get; private set; }

    public string menuSceneName = "MainMenu";

    public event Action<PlayerStateDTO> PlayerStateReceived;

    NetLink m_link;
    readonly ReliableChannel m_reliable = new ReliableChannel();
    readonly Dictionary<int, PlayerStateDTO> m_playerStates = new Dictionary<int, PlayerStateDTO>();
    readonly List<byte[]> m_resends = new List<byte[]>();
    ushort m_sequence;

    const float TickRate = 20f;
    public readonly NetLinkingContext Linking = new NetLinkingContext();
    ReplicationManagerServer m_repServer;
    ReplicationManagerClient m_repClient;
    float m_nextTick;
    int m_bytesThisSecond, m_bytesLastSecond, m_packetsThisSecond, m_packetsLastSecond;
    float m_statsAt;
    bool m_showStats;
    string m_requestedScene = "";
    string m_banner = "";
    float m_bannerUntil;

    public bool IsHost { get { return m_link != null && m_link.IsHost; } }
    public bool IsConnected { get { return m_link != null; } }
    public int PendingReliable { get { return m_reliable.PendingCount; } }

    public static GameNet Create(NetLink link)
    {
        GameObject go = new GameObject("GameNet");
        DontDestroyOnLoad(go);
        GameNet net = go.AddComponent<GameNet>();
        net.Init(link);
        return net;
    }

    public void Init(NetLink link)
    {
        Instance = this;
        m_link = link;
        link.Game = this;
        m_repServer = new ReplicationManagerServer(Linking);
        m_repClient = new ReplicationManagerClient(Linking, NetPrefabTable.Load());
        link.GameMessage += OnGameMessage;
        link.LinkClosed += OnLinkClosed;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (m_link != null)
        {
            m_link.GameMessage -= OnGameMessage;
            m_link.LinkClosed -= OnLinkClosed;
        }
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (m_link == null) return;

        m_reliable.CollectResends(NetUtil.NowMs, m_resends);
        foreach (byte[] message in m_resends) m_link.SendGame(message);

        if (m_link.IsHost && Time.unscaledTime >= m_nextTick)
        {
            m_nextTick = Time.unscaledTime + 1f / TickRate;
            ReplicationTick();
        }

        if (UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame) m_showStats = !m_showStats;
        if (Time.unscaledTime >= m_statsAt)
        {
            m_statsAt = Time.unscaledTime + 1f;
            m_bytesLastSecond = m_bytesThisSecond;
            m_packetsLastSecond = m_packetsThisSecond;
            m_bytesThisSecond = 0;
            m_packetsThisSecond = 0;
        }
    }

    // ------------------------------------------------------------ Replication ---
    void ReplicationTick()
    {
        foreach (int id in Linking.IdsSnapshot())
        {
            GameObject go = Linking.Find(id);
            if (go == null)
            {
                Linking.Unregister(id);
                m_repServer.MarkDestroy(id);
                continue;
            }
            NetObject obj = go.GetComponent<NetObject>();
            if (obj != null && obj.ShouldSend()) m_repServer.MarkUpdate(id);
        }

        foreach (byte[] packet in m_repServer.WritePackets())
        {
            m_link.SendGame(packet);
            m_bytesThisSecond += packet.Length;
            m_packetsThisSecond++;
        }
    }

    public int RegisterNew(NetObject obj)
    {
        int id = Linking.RegisterNew(obj.gameObject);
        obj.id = id;
        m_repServer.MarkCreate(id);
        return id;
    }

    public void RegisterWithId(NetObject obj, int id)
    {
        obj.id = id;
        Linking.Register(obj.gameObject, id);
        if (IsHost) m_repServer.MarkCreate(id);
    }

    public void NetObjectDestroyed(NetObject obj)
    {
        Linking.Unregister(obj.id);
        if (IsHost) m_repServer.MarkDestroy(obj.id);
    }

    // ---------------------------------------------------------------- Sending ---
    public ushort NextSequence() { return ++m_sequence; }

    public void SendPlayerState(PlayerStateDTO dto)
    {
        if (m_link != null) m_link.SendGame(Serializer.Serialize(dto));
    }

    public void SendEvent(GameEventDTO ev)
    {
        if (m_link == null) return;

        ushort id = m_reliable.NextId();
        byte[] message = Serializer.Serialize(ev, id);
        m_reliable.Track(id, message, NetUtil.NowMs);
        m_link.SendGame(message);
    }

    // -------------------------------------------------------------- Receiving ---
    // Last state received, for a scene that loads after the packet arrived.
    public bool TryGetPlayerState(int index, out PlayerStateDTO dto)
    {
        return m_playerStates.TryGetValue(index, out dto);
    }

    void OnGameMessage(byte[] data, int sender)
    {
        try
        {
            switch (Serializer.TypeOf(data))
            {
                case MsgType.PlayerState:
                    HandlePlayerState(Serializer.DeserializePlayerState(data), sender);
                    break;
                case MsgType.Replication:
                    if (!IsHost)
                    {
                        BinaryReader reader = new BinaryReader(new MemoryStream(data));
                        reader.ReadByte();
                        m_repClient.ReadPacket(reader);
                    }
                    break;
                case MsgType.Event:
                    HandleEvent(data, sender);
                    break;
                case MsgType.Ack:
                    m_reliable.Acknowledge(Serializer.ReadAck(data));
                    break;
            }
        }
        catch (Exception e)
        {
            // Unreadable messages are dropped.
            Debug.LogWarning("[GameNet] mensaje ilegible: " + e.Message);
        }
    }

    void HandlePlayerState(PlayerStateDTO dto, int sender)
    {
        // The host does not trust the index inside the packet.
        if (IsHost) dto.playerIndex = (byte)sender;

        PlayerStateDTO old;
        if (m_playerStates.TryGetValue(dto.playerIndex, out old) && !NetUtil.IsNewer(dto.sequence, old.sequence)) return;

        m_playerStates[dto.playerIndex] = dto;
        if (PlayerStateReceived != null) PlayerStateReceived(dto);
    }

    void HandleEvent(byte[] data, int sender)
    {
        ushort id;
        GameEventDTO ev = Serializer.DeserializeEvent(data, out id);

        // Acknowledge repeated events too: the Ack may have been lost.
        m_link.SendGame(Serializer.Ack(id));
        if (!m_reliable.IsNew(id)) return;

        switch (ev.kind)
        {
            case GameEventKind.SceneRequest:
                if (IsHost) RequestScene(ev.text);
                break;
            case GameEventKind.SceneLoad:
                if (!IsHost)
                {
                    m_requestedScene = ev.text;
                    LoadSceneLocal(ev.text);
                }
                break;
            default:
                NetCube.OnEvent(ev, sender);
                break;
        }
    }

    // ---------------------------------------------------------- Scene changes ---
    // Returns true when the caller must not load the scene by itself.
    public bool InterceptSceneLoad(string sceneName)
    {
        if (m_link == null) return false;

        if (m_link.IsHost)
        {
            // Five bones enter the trigger: only the first counts.
            if (sceneName == m_requestedScene) return true;
            m_requestedScene = sceneName;

            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.SceneLoad;
            ev.text = sceneName;
            SendEvent(ev);
            return false;
        }

        if (sceneName != m_requestedScene)
        {
            m_requestedScene = sceneName;
            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.SceneRequest;
            ev.text = sceneName;
            SendEvent(ev);
        }
        return true;
    }

    void RequestScene(string sceneName)
    {
        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.LoadScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    void LoadSceneLocal(string sceneName)
    {
        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.LoadSceneLocal(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (m_link == null) return;
        m_requestedScene = "";
        Linking.Clear();
        m_repServer.Reset();

        if (scene.name == menuSceneName)
        {
            EndSession("");
            return;
        }
        NetWorld.RegisterSceneObjects();
    }

    // ------------------------------------------------------------ End of game ---
    void OnLinkClosed(string reason)
    {
        EndSession(reason);
        SceneManager.LoadScene(menuSceneName);
    }

    void EndSession(string reason)
    {
        if (m_link == null) return;

        NetLink link = m_link;
        m_link = null;
        link.GameMessage -= OnGameMessage;
        link.LinkClosed -= OnLinkClosed;
        link.Shutdown();
        Destroy(link.gameObject);

        NetSession.Reset();
        m_reliable.Clear();

        if (!string.IsNullOrEmpty(reason))
        {
            m_banner = reason;
            m_bannerUntil = Time.unscaledTime + 5f;
            NetSession.LastMessage = reason;
        }
        Destroy(gameObject, 5f);
    }

    void OnGUI()
    {
        if (m_banner.Length > 0 && Time.unscaledTime < m_bannerUntil) StatusBox.Draw(m_banner);

        // F3 toggles the replication stats
        if (m_showStats && m_link != null)
        {
            GUI.Label(new Rect(10, 10, 420, 24),
                (IsHost ? "HOST" : "CLIENT") + "  replication: " + m_bytesLastSecond + " B/s, "
                + m_packetsLastSecond + " pkt/s, " + Linking.Count + " objects");
        }
    }
}
