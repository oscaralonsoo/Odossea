using System.Collections.Generic;

// A small "reliable messages over UDP" layer, only for the few things that must not be lost.
//
//   sender:    gives the event a number, keeps a copy and resends it every 150 ms
//              until the receiver answers with an Ack carrying the same number
//   receiver:  always answers with an Ack, but only processes a number the first time
//              (the Ack itself can be lost, so the sender may send the same event twice)
//
// Positions do NOT use this: they are sent unreliably, because a lost position is
// replaced by the next one 33 ms later and resending an old position would only add lag.
public class ReliableChannel
{
    public const long ResendAfterMs = 150;
    public const int MaxAttempts = 60;          // about 9 seconds: then the other side is gone
    const int RememberReceived = 512;

    class Pending
    {
        public byte[] message;
        public long lastSentMs;
        public int attempts = 1;
    }

    readonly Dictionary<ushort, Pending> m_pending = new Dictionary<ushort, Pending>();
    readonly HashSet<ushort> m_received = new HashSet<ushort>();
    readonly Queue<ushort> m_receivedOrder = new Queue<ushort>();
    readonly List<ushort> m_expired = new List<ushort>();
    ushort m_nextId;

    public int PendingCount { get { return m_pending.Count; } }

    // 0 is never used, so a default(ushort) can not be mistaken for a real event.
    public ushort NextId()
    {
        m_nextId++;
        if (m_nextId == 0) m_nextId = 1;
        return m_nextId;
    }

    public void Track(ushort id, byte[] message, long nowMs)
    {
        Pending pending = new Pending();
        pending.message = message;
        pending.lastSentMs = nowMs;
        m_pending[id] = pending;
    }

    public void Acknowledge(ushort id)
    {
        m_pending.Remove(id);
    }

    // Fills 'output' with the messages that have waited too long for their Ack.
    public void CollectResends(long nowMs, List<byte[]> output)
    {
        output.Clear();
        m_expired.Clear();

        foreach (KeyValuePair<ushort, Pending> pair in m_pending)
        {
            Pending pending = pair.Value;
            if (nowMs - pending.lastSentMs < ResendAfterMs) continue;

            if (pending.attempts >= MaxAttempts)
            {
                m_expired.Add(pair.Key);
                continue;
            }

            pending.attempts++;
            pending.lastSentMs = nowMs;
            output.Add(pending.message);
        }

        foreach (ushort id in m_expired) m_pending.Remove(id);
    }

    // True the first time an id is seen, false for a repeated one.
    public bool IsNew(ushort id)
    {
        if (!m_received.Add(id)) return false;

        m_receivedOrder.Enqueue(id);
        while (m_receivedOrder.Count > RememberReceived) m_received.Remove(m_receivedOrder.Dequeue());
        return true;
    }

    public void Clear()
    {
        m_pending.Clear();
        m_received.Clear();
        m_receivedOrder.Clear();
    }
}
