// Something that happens once and must arrive (reliable). The meaning of each field
// depends on 'kind': see GameEventKind.
public struct GameEventDTO
{
    public GameEventKind kind;
    public uint id;
    public float x, y;
    public float dx, dy;
    public string text;
}
