using System.IO;
using UnityEngine;

// Client side. Copies what the host says: it never decides anything.
public class ReplicationManagerClient
{
    readonly NetLinkingContext m_linking;
    readonly NetPrefabTable m_prefabs;
    int m_lastSequence = -1;

    public ReplicationManagerClient(NetLinkingContext linking, NetPrefabTable prefabs)
    {
        m_linking = linking;
        m_prefabs = prefabs;
    }

    // The MsgType byte is already read.
    public void ReadPacket(BinaryReader reader)
    {
        int sequence = reader.ReadInt32();
        if (sequence <= m_lastSequence) return;          // older than what we already have
        m_lastSequence = sequence;

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            ReplicationAction action = (ReplicationAction)reader.ReadByte();
            GameObject go = m_linking.Find(id);          // null if this PC does not know it

            if (action == ReplicationAction.Destroy)
            {
                if (go != null)
                {
                    m_linking.Unregister(id);
                    Object.Destroy(go);
                }
                continue;                                // unknown ID: already gone
            }

            byte type = reader.ReadByte();

            if (go == null)
            {
                if (type == NetObject.SceneObject)
                {
                    // an object of a scene this PC has not loaded yet: skip its bytes
                    reader.ReadBytes(NetObject.BasePayloadSize);
                    continue;
                }

                // Create, or an Update whose Create was lost
                GameObject prefab = m_prefabs != null ? m_prefabs.Get(type) : null;
                if (prefab == null)
                {
                    Debug.LogWarning("[Replication] tipo desconocido " + type + ": paquete descartado");
                    return;                              // we cannot know how many bytes it has
                }

                go = Object.Instantiate(prefab);
                NetObject created = go.GetComponent<NetObject>();
                created.id = id;
                m_linking.Register(go, id);
                created.OnReplicaCreated();
            }

            go.GetComponent<NetObject>().Read(reader);
        }
    }
}
