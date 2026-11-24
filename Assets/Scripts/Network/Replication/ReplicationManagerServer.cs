using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Host side. The game says WHAT happened (MarkCreate, MarkUpdate, MarkDestroy) and once per tick
// WritePackets() turns it into bytes. Packet:
//
//   [MsgType.Replication][int sequence][int count]  then 'count' entries:
//   [int id][byte action]                      Destroy: nothing else
//   [int id][byte action][byte type][fields]   Create / Update: the object writes its own fields
public class ReplicationManagerServer
{
    const int MaxPacketBytes = 1100;     // under ~1200: bigger and the network fragments it

    readonly NetLinkingContext m_linking;
    readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();
    readonly Dictionary<int, (ReplicationAction action, float until)> m_repeat =
        new Dictionary<int, (ReplicationAction, float)>();
    int m_sequence;

    public ReplicationManagerServer(NetLinkingContext linking) { m_linking = linking; }

    public void MarkCreate(int id)
    {
        m_pending[id] = ReplicationAction.Create;
        m_repeat[id] = (ReplicationAction.Create, Time.unscaledTime + 1f);   // repeated for 1 s
    }

    public void MarkDestroy(int id)
    {
        m_pending[id] = ReplicationAction.Destroy;
        m_repeat[id] = (ReplicationAction.Destroy, Time.unscaledTime + 1f);
    }

    public void MarkUpdate(int id)
    {
        if (!m_pending.ContainsKey(id))                  // a Create or a Destroy wins
            m_pending[id] = ReplicationAction.Update;
    }

    // New level: forget everything that was waiting. The sequence keeps growing.
    public void Reset()
    {
        m_pending.Clear();
        m_repeat.Clear();
    }

    // Once per tick. Usually one packet, several if there are too many objects.
    public List<byte[]> WritePackets()
    {
        foreach (int id in m_repeat.Keys.ToList())
        {
            if (Time.unscaledTime > m_repeat[id].until) m_repeat.Remove(id);
            else m_pending[id] = m_repeat[id].action;
        }

        List<byte[]> packets = new List<byte[]>();
        MemoryStream body = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(body);
        int count = 0;

        foreach (KeyValuePair<int, ReplicationAction> entry in m_pending)
        {
            NetObject obj = null;
            if (entry.Value != ReplicationAction.Destroy)
            {
                GameObject go = m_linking.Find(entry.Key);
                if (go == null) continue;                // already gone
                obj = go.GetComponent<NetObject>();
                if (obj == null) continue;
            }

            writer.Write(entry.Key);
            writer.Write((byte)entry.Value);
            if (obj != null)
            {
                writer.Write(obj.type);                  // its class, which prefab
                obj.Write(writer);                       // its fields
            }
            count++;

            if (body.Length >= MaxPacketBytes)
            {
                packets.Add(Finish(body, count));
                body = new MemoryStream();
                writer = new BinaryWriter(body);
                count = 0;
            }
        }
        if (count > 0) packets.Add(Finish(body, count));

        m_pending.Clear();
        return packets;
    }

    byte[] Finish(MemoryStream body, int count)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.Replication);         // header, 9 bytes
        writer.Write(m_sequence++);
        writer.Write(count);
        writer.Write(body.ToArray());
        return stream.ToArray();
    }
}
