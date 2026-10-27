// What the lobby decided, read by the game scenes. Static so it survives the scene change.
// While IsOnline is false the game behaves exactly as the local co-op version.
public static class NetSession
{
    public static bool IsOnline;
    public static bool IsHost;
    public static int LocalIndex;                       // 0 = host = Player1, 1 = client = Player2
    public static string LocalName = "";
    public static string[] PlayerNames = new string[0];
    public static string LastMessage = "";              // why we came back to the menu (kicked, room closed...)

    public static void Reset()
    {
        IsOnline = false;
        IsHost = false;
        LocalIndex = 0;
        LocalName = "";
        PlayerNames = new string[0];
    }
}
