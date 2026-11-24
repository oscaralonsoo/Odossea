using System.IO;
using UnityEngine;

// Replicated mass cube. Host: normal physics object. Client: kinematic copy. Prefab type = 0.
public class NetCubeObject : NetObject
{
    const float Smoothing = 20f;

    public byte owner;                       // player that shot it, gives the colour

    DropableCube m_cube;
    Vector2 m_target;
    bool m_hasTarget;
    bool m_looksApplied;
    bool m_goneRequested;

    public bool IsReplica { get; private set; }

    void Awake() { m_cube = GetComponentInChildren<DropableCube>(true); }

    // ------------------------------------------------------------------ Host ---
    public override void Write(BinaryWriter w)
    {
        Vector2 p = m_cube.transform.position;
        w.Write(owner);
        w.Write((byte)m_cube.growNum);
        w.Write(p.x);
        w.Write(p.y);
    }

    // ---------------------------------------------------------------- Client ---
    public override void OnReplicaCreated()
    {
        IsReplica = true;
        foreach (Rigidbody2D rb in GetComponentsInChildren<Rigidbody2D>(true))
            rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public override void Read(BinaryReader r)
    {
        owner = r.ReadByte();
        int grow = r.ReadByte();
        float x = r.ReadSingle();
        float y = r.ReadSingle();

        m_target = new Vector2(x, y);
        if (!m_hasTarget) MoveTo(m_target);
        m_hasTarget = true;

        while (m_cube.growNum < grow) m_cube.Grow(1);
        if (!m_looksApplied) ApplyOwnerLook();
    }

    void Update()
    {
        if (!IsReplica || !m_hasTarget) return;
        float t = 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
        MoveTo(Vector2.Lerp(m_cube.transform.position, m_target, t));
    }

    void MoveTo(Vector2 cubePosition)
    {
        Vector2 delta = cubePosition - (Vector2)m_cube.transform.position;
        transform.position += (Vector3)delta;
    }

    void ApplyOwnerLook()
    {
        NetPlayerSync shooter = NetPlayerSync.Find(owner);
        if (shooter == null) return;

        SpriteRenderer from = shooter.Player.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer to = GetComponentInChildren<SpriteRenderer>();
        if (from != null && to != null) to.material = new Material(from.material);
        m_looksApplied = true;
    }

    // True only the first time, so the host is asked once.
    public bool TryRequestGone()
    {
        if (m_goneRequested) return false;
        m_goneRequested = true;
        return true;
    }
}
