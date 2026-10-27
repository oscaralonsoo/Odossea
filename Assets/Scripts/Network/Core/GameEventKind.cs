// What a reliable GameEventDTO means. Only things that must not be lost go here.
public enum GameEventKind : byte
{
    Shoot = 1,          // a player fired a mass cube: id, origin (x, y), direction (dx, dy)
    CubeGone = 2,       // a cube was absorbed or merged: id
    SceneRequest = 3,   // client to host: "please load this scene" (text = scene name)
    SceneLoad = 4       // host to client: "load this scene now" (text = scene name)
}
