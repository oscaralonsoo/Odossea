using UnityEngine;

// Registers the platforms and doors of a level for replication. Crushers are not replicated.
public static class NetWorld
{
    public static void RegisterSceneObjects()
    {
        foreach (MovingPlatform platform in Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
            NetWorldObject.Attach(platform.gameObject, platform);

        foreach (PlataformaMovil platform in Object.FindObjectsByType<PlataformaMovil>(FindObjectsSortMode.None))
            NetWorldObject.Attach(platform.gameObject, platform);

        foreach (DoorBehaviour door in Object.FindObjectsByType<DoorBehaviour>(FindObjectsSortMode.None))
            NetWorldObject.Attach(door.gameObject, door);
    }
}
