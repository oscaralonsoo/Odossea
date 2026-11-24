using UnityEngine;

// The part of the network that knows the game: which objects of a level belong to the world
// the host replicates. GameNet calls it every time a level finishes loading.
//
// Not replicated on purpose: the crushers. They run the same cycle on both PCs and only
// kill the player of their own PC (see the README, known bugs).
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
