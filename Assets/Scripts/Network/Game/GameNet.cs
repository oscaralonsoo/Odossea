using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The network of the game scenes. The lobby creates it when the host presses Start and it lives
// until the players go back to the menu.
//
//   unreliable (positions)  SendPlayerState / SendObjectState: fire and forget, ~30 and ~15 per second
//   reliable (events)       SendEvent: kept and resent until the other PC acknowledges it
//
// It also decides who is allowed to change the scene: only the host. A client that asks
// (restart, end of level...) sends a SceneRequest, and the host answers with a SceneLoad that
// both PCs obey, so the two always end up in the same level.
public class GameNet : MonoBehaviour
{
    public static GameNet Instance { get; private set; }

    public string menuSceneName = "MainMenu";

    public event Action<PlayerStateDTO> PlayerStateReceived;
    public event Action<ObjectStateDTO> ObjectStateReceived;

    NetLink m_link;
    readonly ReliableChannel m_reliable = new ReliableChannel();
    readonly Dictionary<int, PlayerStateDTO> m_playerStates = new Dictionary<int, PlayerStateDTO>();
    readonly Dictionary<uint, ObjectStateDTO> m_objectStates = new Dictionary<uint, ObjectStateDTO>();
    readonly List<byte[]> m_resends = new List<byte[]>();
    ushort m_sequence;
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
    }

    // ---------------------------------------------------------------- Sending ---
    public ushort NextSequence() { return ++m_sequence; }

    public void SendPlayerState(PlayerStateDTO dto)
    {
        if (m_link != null) m_link.SendGame(Serializer.Serialize(dto));
    }

    public void SendObjectState(ObjectStateDTO dto)
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
    // Latest state received for a player / object: a scene that loads after the packet arrived
    // (the other PC was faster) still finds it.
    public bool TryGetPlayerState(int index, out PlayerStateDTO dto)
    {
        return m_playerStates.TryGetValue(index, out dto);
    }

    public bool TryGetObjectState(uint id, out ObjectStateDTO dto)
    {
        return m_objectStates.TryGetValue(id, out dto);
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
                case MsgType.ObjectState:
                    HandleObjectState(Serializer.DeserializeObjectState(data));
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
            // A message that cannot be read is dropped. It never takes the game down.
            Debug.LogWarning("[GameNet] mensaje ilegible: " + e.Message);
        }
    }

    void HandlePlayerState(PlayerStateDTO dto, int sender)
    {
        // The host knows who really sent it: it does not trust the index inside the packet.
        if (IsHost) dto.playerIndex = (byte)sender;

        PlayerStateDTO old;
        if (m_playerStates.TryGetValue(dto.playerIndex, out old) && !NetUtil.IsNewer(dto.sequence, old.sequence)) return;

        m_playerStates[dto.playerIndex] = dto;
        if (PlayerStateReceived != null) PlayerStateReceived(dto);
    }

    void HandleObjectState(ObjectStateDTO dto)
    {
        ObjectStateDTO old;
        if (m_objectStates.TryGetValue(dto.id, out old) && !NetUtil.IsNewer(dto.sequence, old.sequence)) return;

        m_objectStates[dto.id] = dto;
        if (ObjectStateReceived != null) ObjectStateReceived(dto);
    }

    void HandleEvent(byte[] data, int sender)
    {
        ushort id;
        GameEventDTO ev = Serializer.DeserializeEvent(data, out id);

        // Always acknowledge, even a repeated event: the previous Ack may have been lost.
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
                NetCube.OnEvent(ev, sender);        // shoot, cube gone
                break;
        }
    }

    // ---------------------------------------------------------- Scene changes ---
    // SceneTransitionManager.LoadScene calls this first. Returns true when the call was
    // handled here and the caller must NOT load the scene by itself.
    public bool InterceptSceneLoad(string sceneName)
    {
        if (m_link == null) return false;

        if (m_link.IsHost)
        {
            // Five bones enter the same trigger: only the first one counts.
            if (sceneName == m_requestedScene) return true;
            m_requestedScene = sceneName;

            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.SceneLoad;
            ev.text = sceneName;
            SendEvent(ev);
            return false;                       // the host loads it normally
        }

        if (sceneName != m_requestedScene)
        {
            m_requestedScene = sceneName;
            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.SceneRequest;
            ev.text = sceneName;
            SendEvent(ev);
        }
        return true;                            // wait for the host's order
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
        if (m_link == null) return;         // the session already ended
        m_requestedScene = "";
        NetCube.Clear();                    // the cubes of the old level are gone

        if (scene.name == menuSceneName)
        {
            EndSession("");
            return;
        }
        NetWorld.AttachObjectSync();
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
        Destroy(gameObject, 5f);        // stays 5 seconds to show the reason
    }

    void OnGUI()
    {
        if (m_banner.Length > 0 && Time.unscaledTime < m_bannerUntil) StatusBox.Draw(m_banner);
    }
}
