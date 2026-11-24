using System.IO;
using UnityEngine;

// Base class of every replicated object. Read must read exactly what Write wrote.
public class NetObject : MonoBehaviour
{
    // Type of objects already in the scene: the client finds them, never creates them.
    public const byte SceneObject = 255;

    // Bytes written by the base Write: x, y, angle.
    public const int BasePayloadSize = 12;

    [HideInInspector] public int id;
    public byte type;                    // index in NetPrefabTable

    // Host, once per tick: is there anything new to send?
    public virtual bool ShouldSend() { return true; }

    public virtual void Write(BinaryWriter w)
    {
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.eulerAngles.z);
    }

    public virtual void Read(BinaryReader r)
    {
        float x = r.ReadSingle();
        float y = r.ReadSingle();
        float angle = r.ReadSingle();
        transform.position = new Vector3(x, y, transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Client: called after the manager creates the object, before the first Read.
    public virtual void OnReplicaCreated() { }

    protected virtual void OnDestroy()
    {
        if (id != 0 && GameNet.Instance != null) GameNet.Instance.NetObjectDestroyed(this);
    }
}
