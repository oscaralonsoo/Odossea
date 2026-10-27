using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public static class NetUtil
{
    public const int MaxPacketSize = 64 * 1024;

    static readonly Stopwatch s_clock = Stopwatch.StartNew();

    // Milliseconds since the program started. Unlike Time.time it works in any thread and
    // it does not stop when the game is paused (Time.timeScale = 0).
    public static long NowMs { get { return s_clock.ElapsedMilliseconds; } }

    public static Thread StartThread(ThreadStart work)
    {
        Thread thread = new Thread(work);
        thread.IsBackground = true;
        thread.Start();
        return thread;
    }

    public static void CloseSocket(Socket socket)
    {
        if (socket == null) return;
        try { socket.Close(); } catch (Exception) { }
    }

    // Windows: sending UDP to a closed port makes the NEXT ReceiveFrom throw "ConnectionReset"
    // (10054), and a receive loop that stops on any exception would die. This turns it off.
    public static void DisableUdpConnReset(Socket socket)
    {
        try { socket.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null); }
        catch (Exception) { }
    }

    // True when sequence number 'a' is newer than 'b', also after the ushort wraps around.
    public static bool IsNewer(ushort a, ushort b)
    {
        return unchecked((short)(a - b)) > 0;
    }

    // Takes out what would break the text protocol (commas, '=', line breaks) and cuts the length.
    public static string CleanName(string raw, int maxLen)
    {
        if (raw == null) raw = "";
        StringBuilder sb = new StringBuilder();
        foreach (char ch in raw.Trim())
        {
            if (ch == ',' || ch == '=' || char.IsControl(ch)) continue;
            sb.Append(ch);
            if (sb.Length >= maxLen) break;
        }
        string result = sb.ToString().Trim();
        return result.Length == 0 ? "Player" : result;
    }

    // The IPv4 addresses of this PC, to show the host which one to give to the other player.
    public static string LocalIps()
    {
        try
        {
            List<string> ips = new List<string>();
            foreach (IPAddress a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                    ips.Add(a.ToString());
            }
            return ips.Count > 0 ? string.Join(", ", ips.ToArray()) : "?";
        }
        catch (Exception) { return "?"; }
    }
}
