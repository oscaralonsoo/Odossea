using System.IO;
using UnityEngine;

// The mass cube shot by a player. Add it to the cube prefab (the one MassCubeInstantiator shoots)
// and set  type = 0  (index of the prefab in NetPrefabTable).
//
//   host:    the cube is a normal physics object. It has an ID, it is sent every tick.
//   client:  the cube is a copy: kinematic, it only goes where the host says.
public class NetCubeObject : NetObject
{
    const float Smoothing = 20f;

    public byte owner;                       // who shot it: it gets that player's colour

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
        if (!m_hasTarget) MoveTo(m_target);          // the first time it appears where it is
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

    // The bones hang from this object: moving it moves the whole cube.
    void MoveTo(Vector2 cubePosition)
    {
        Vector2 delta = cubePosition - (Vector2)m_cube.transform.position;
        transform.position += (Vector3)delta;
    }

    void ApplyOwnerLook()
    {
        NetPlayerSync shooter = NetPlayerSync.Find(owner);
        if (shooter == null) return;                 // the player is not there yet: try on the next Read

        SpriteRenderer from = shooter.Player.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer to = GetComponentInChildren<SpriteRenderer>();
        if (from != null && to != null) to.material = new Material(from.material);
        m_looksApplied = true;
    }

    // Client: true the FIRST time it is called, so the request to the host is sent only once.
    // The cube disappears when the host's Destroy arrives.
    public bool TryRequestGone()
    {
        if (m_goneRequested) return false;
        m_goneRequested = true;
        return true;
    }
}
