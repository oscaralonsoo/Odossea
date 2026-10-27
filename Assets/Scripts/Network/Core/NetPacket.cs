using System.Net;

// One datagram as the receiving thread leaves it in the queue.
public class NetPacket
{
    public EndPoint from;
    public byte[] data;
    public long ms;     // when it arrived, measured in the network thread (NetUtil.NowMs)
}
