using UnityEngine;

// The host owns the moving world (platforms, doors): it sends where they are about 15 times per
// second and the client moves its copy there. The script that normally drives the object
// (MovingPlatform, DoorBehaviour...) is switched off on the client so it does not fight the network.
//
// NetWorld adds this component by code when a level loads: nothing to set up in the scenes.
public class NetObjectSync : MonoBehaviour
{
    const float SendRate = 15f;
    const float KeepAliveSeconds = 1f;      // an object that does not move is still sent once per second
    const float MoveEpsilon = 0.001f;
    const float Smoothing = 20f;

    uint m_id;
    Rigidbody2D m_body;
    bool m_isHost;

    // host
    float m_nextSend;
    float m_lastSentTime;
    Vector2 m_lastSentPosition;
    float m_lastSentAngle;

    // client
    ObjectStateDTO m_target;
    bool m_hasTarget;
    ushort m_lastSequence;

    public static NetObjectSync Attach(GameObject target, params Behaviour[] driversToDisableOnClient)
    {
        if (!NetSession.IsOnline || GameNet.Instance == null) return null;

        NetObjectSync sync = target.AddComponent<NetObjectSync>();
        sync.Setup(driversToDisableOnClient);
        return sync;
    }

    // The same object has the same id on both PCs: it is made of the position of the object in the
    // hierarchy (sibling index of the object, of its parent, of its grandparent...), and both PCs
    // load the same scene file.
    public static uint ComputeId(Transform t)
    {
        uint hash = 2166136261;
        while (t != null)
        {
            hash = unchecked((hash ^ (uint)(t.GetSiblingIndex() + 1)) * 16777619);
            t = t.parent;
        }
        return hash;
    }

    void Setup(Behaviour[] drivers)
    {
        m_isHost = NetSession.IsHost;
        m_id = ComputeId(transform);
        m_body = GetComponent<Rigidbody2D>();

        if (m_isHost) return;

        foreach (Behaviour driver in drivers)
        {
            if (driver != null) driver.enabled = false;
        }
        if (m_body != null) m_body.bodyType = RigidbodyType2D.Kinematic;

        GameNet.Instance.ObjectStateReceived += OnObjectState;

        ObjectStateDTO cached;
        if (GameNet.Instance.TryGetObjectState(m_id, out cached)) OnObjectState(cached);
    }

    void OnDestroy()
    {
        if (!m_isHost && GameNet.Instance != null) GameNet.Instance.ObjectStateReceived -= OnObjectState;
    }

    // ------------------------------------------------------------------ Host ---
    void Update()
    {
        if (!m_isHost || GameNet.Instance == null || Time.unscaledTime < m_nextSend) return;
        m_nextSend = Time.unscaledTime + 1f / SendRate;

        Vector2 position = transform.position;
        float angle = transform.eulerAngles.z;

        bool moved = (position - m_lastSentPosition).sqrMagnitude > MoveEpsilon * MoveEpsilon
                     || Mathf.Abs(Mathf.DeltaAngle(angle, m_lastSentAngle)) > 0.1f;
        bool keepAlive = Time.unscaledTime - m_lastSentTime > KeepAliveSeconds;
        if (!moved && !keepAlive) return;

        m_lastSentPosition = position;
        m_lastSentAngle = angle;
        m_lastSentTime = Time.unscaledTime;

        ObjectStateDTO dto = new ObjectStateDTO();
        dto.id = m_id;
        dto.sequence = GameNet.Instance.NextSequence();
        dto.position = position;
        dto.angle = angle;
        GameNet.Instance.SendObjectState(dto);
    }

    // ---------------------------------------------------------------- Client ---
    void OnObjectState(ObjectStateDTO dto)
    {
        if (dto.id != m_id) return;
        if (m_hasTarget && !NetUtil.IsNewer(dto.sequence, m_lastSequence)) return;

        m_lastSequence = dto.sequence;
        m_target = dto;
        m_hasTarget = true;
    }

    void FixedUpdate()
    {
        if (m_isHost || !m_hasTarget) return;

        float t = 1f - Mathf.Exp(-Smoothing * Time.fixedDeltaTime);

        if (m_body != null)
        {
            // Kinematic + MovePosition: a player standing on the platform is carried with it.
            m_body.MovePosition(Vector2.Lerp(m_body.position, m_target.position, t));
        }
        else
        {
            transform.position = Vector2.Lerp(transform.position, m_target.position, t);
        }
    }
}
