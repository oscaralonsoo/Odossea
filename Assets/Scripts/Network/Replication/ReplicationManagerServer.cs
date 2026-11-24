using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Host side: the game marks changes, WritePackets() turns them into bytes once per tick.
// Packet: [MsgType][int sequence][int count] then entries
//   Destroy:          [int id][byte action]
//   Create / Update:  [int id][byte action][byte type][object fields]
public class ReplicationManagerServer
{
    const int MaxPacketBytes = 1100;

    readonly NetLinkingContext m_linking;
    readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();
    readonly Dictionary<int, (ReplicationAction action, float until)> m_repeat =
        new Dictionary<int, (ReplicationAction, float)>();
    int m_sequence;

    public ReplicationManagerServer(NetLinkingContext linking) { m_linking = linking; }

    public void MarkCreate(int id)
    {
        m_pending[id] = ReplicationAction.Create;
        m_repeat[id] = (ReplicationAction.Create, Time.unscaledTime + 1f);
    }

    public void MarkDestroy(int id)
    {
        m_pending[id] = ReplicationAction.Destroy;
        m_repeat[id] = (ReplicationAction.Destroy, Time.unscaledTime + 1f);
    }

    public void MarkUpdate(int id)
    {
        if (!m_pending.ContainsKey(id))
            m_pending[id] = ReplicationAction.Update;
    }

    public void Reset()
    {
        m_pending.Clear();
        m_repeat.Clear();
    }

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
                if (go == null) continue;
                obj = go.GetComponent<NetObject>();
                if (obj == null) continue;
            }

            writer.Write(entry.Key);
            writer.Write((byte)entry.Value);
            if (obj != null)
            {
                writer.Write(obj.type);
                obj.Write(writer);
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
        writer.Write((byte)MsgType.Replication);
        writer.Write(m_sequence++);
        writer.Write(count);
        writer.Write(body.ToArray());
        return stream.ToArray();
    }
}
