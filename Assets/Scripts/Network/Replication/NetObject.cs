using System.IO;
using UnityEngine;

// Every networked object writes and reads itself. The replication manager does not know what a
// cube or a platform is, it only calls Write and Read.
// Read must read EXACTLY what Write wrote, in the same order.
public class NetObject : MonoBehaviour
{
    // 'type' of the objects that already exist in the scene (platforms, doors): the client never
    // creates them, it only finds them.
    public const byte SceneObject = 255;

    // size in bytes of what the base Write puts: x, y, angle
    public const int BasePayloadSize = 12;

    [HideInInspector] public int id;     // network ID, from the linking context
    public byte type;                    // which prefab (index in NetPrefabTable)

    // Host, once per tick: does this object have anything new to say?
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

    // Client only: right after the manager created this object from its prefab, before the first Read.
    public virtual void OnReplicaCreated() { }

    protected virtual void OnDestroy()
    {
        if (id != 0 && GameNet.Instance != null) GameNet.Instance.NetObjectDestroyed(this);
    }
}
