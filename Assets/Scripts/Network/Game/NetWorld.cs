using UnityEngine;

// The part of the network that knows the game: which objects of a level belong to the host.
// GameNet calls it every time a level finishes loading.
//
// Not synchronized on purpose: the crushers. They run the same cycle on both PCs and only
// kill the player of their own PC (see the README, known bugs).
public static class NetWorld
{
    public static void AttachObjectSync()
    {
        foreach (MovingPlatform platform in Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
            NetObjectSync.Attach(platform.gameObject, platform);

        foreach (PlataformaMovil platform in Object.FindObjectsByType<PlataformaMovil>(FindObjectsSortMode.None))
            NetObjectSync.Attach(platform.gameObject, platform);

        foreach (DoorBehaviour door in Object.FindObjectsByType<DoorBehaviour>(FindObjectsSortMode.None))
            NetObjectSync.Attach(door.gameObject, door);
    }
}
