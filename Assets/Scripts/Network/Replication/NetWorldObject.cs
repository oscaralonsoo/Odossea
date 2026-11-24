using System.IO;
using UnityEngine;

// Platform or door already in the scene: the host moves it, the client copies it.
// Added by NetWorld. The ID comes from the position in the hierarchy.
public class NetWorldObject : NetObject
{
    const float KeepAliveSeconds = 1f;
    const float MoveEpsilon = 0.001f;
    const float Smoothing = 20f;

    Rigidbody2D m_body;
    bool m_isHost;

    // host
    Vector2 m_lastSentPosition;
    float m_lastSentAngle;
    float m_lastSentTime = -10f;

    // client
    Vector2 m_target;
    bool m_hasTarget;

    public static NetWorldObject Attach(GameObject target, params Behaviour[] driversToDisableOnClient)
    {
        GameNet net = GameNet.Instance;
        if (net == null) return null;

        NetWorldObject obj = target.AddComponent<NetWorldObject>();
        obj.type = SceneObject;
        obj.m_isHost = net.IsHost;
        obj.m_body = target.GetComponent<Rigidbody2D>();

        if (!obj.m_isHost)
        {
            foreach (Behaviour driver in driversToDisableOnClient)
                if (driver != null) driver.enabled = false;
            if (obj.m_body != null) obj.m_body.bodyType = RigidbodyType2D.Kinematic;
        }

        net.RegisterWithId(obj, ComputeId(target.transform));
        return obj;
    }

    // FNV hash of the sibling indices. Always negative: no clash with spawned object IDs.
    public static int ComputeId(Transform t)
    {
        uint hash = 2166136261;
        while (t != null)
        {
            hash = unchecked((hash ^ (uint)(t.GetSiblingIndex() + 1)) * 16777619);
            t = t.parent;
        }
        return unchecked((int)(hash | 0x80000000u));
    }

    // ------------------------------------------------------------------ Host ---
    public override bool ShouldSend()
    {
        Vector2 position = transform.position;
        float angle = transform.eulerAngles.z;

        bool moved = (position - m_lastSentPosition).sqrMagnitude > MoveEpsilon * MoveEpsilon
                     || Mathf.Abs(Mathf.DeltaAngle(angle, m_lastSentAngle)) > 0.1f;
        bool keepAlive = Time.unscaledTime - m_lastSentTime > KeepAliveSeconds;
        if (!moved && !keepAlive) return false;

        m_lastSentPosition = position;
        m_lastSentAngle = angle;
        m_lastSentTime = Time.unscaledTime;
        return true;
    }

    // ---------------------------------------------------------------- Client ---
    public override void Read(BinaryReader r)
    {
        float x = r.ReadSingle();
        float y = r.ReadSingle();
        r.ReadSingle();                      // angle, unused
        m_target = new Vector2(x, y);
        m_hasTarget = true;
    }

    void FixedUpdate()
    {
        if (m_isHost || !m_hasTarget) return;

        float t = 1f - Mathf.Exp(-Smoothing * Time.fixedDeltaTime);
        if (m_body != null)
            m_body.MovePosition(Vector2.Lerp(m_body.position, m_target, t));
        else
            transform.position = Vector2.Lerp(transform.position, m_target, t);
    }
}
