using System.IO;
using UnityEngine;

// Client side: copies what the host says.
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
        if (sequence <= m_lastSequence) return;
        m_lastSequence = sequence;

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            ReplicationAction action = (ReplicationAction)reader.ReadByte();
            GameObject go = m_linking.Find(id);

            if (action == ReplicationAction.Destroy)
            {
                if (go != null)
                {
                    m_linking.Unregister(id);
                    Object.Destroy(go);
                }
                continue;
            }

            byte type = reader.ReadByte();

            if (go == null)
            {
                if (type == NetObject.SceneObject)
                {
                    // scene not loaded yet: skip its bytes
                    reader.ReadBytes(NetObject.BasePayloadSize);
                    continue;
                }

                // Create, or an Update whose Create was lost
                GameObject prefab = m_prefabs != null ? m_prefabs.Get(type) : null;
                if (prefab == null)
                {
                    Debug.LogWarning("[Replication] tipo desconocido " + type + ": paquete descartado");
                    return;
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
