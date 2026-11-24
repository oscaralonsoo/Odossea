using UnityEngine;

// Mass cubes: only the host simulates them.
//   client shoots:   Shoot event, the host creates the cube and replicates it
//   client absorbs:  CubeGone event, the host destroys the cube and replicates the Destroy
public static class NetCube
{
    static bool Online { get { return NetSession.IsOnline && GameNet.Instance != null; } }

    // Host: after a cube was shot (by its own player or for the client).
    public static void OnHostShoot(GameObject cube, int ownerIndex)
    {
        if (!Online || !NetSession.IsHost || cube == null) return;

        NetCubeObject obj = cube.GetComponent<NetCubeObject>();
        if (obj == null) return;

        obj.owner = (byte)ownerIndex;
        GameNet.Instance.RegisterNew(obj);
    }

    // Client: asks the host to create the cube.
    public static void RequestShoot(Vector2 origin, Vector2 direction)
    {
        if (!Online) return;

        GameEventDTO ev = new GameEventDTO();
        ev.kind = GameEventKind.Shoot;
        ev.x = origin.x;
        ev.y = origin.y;
        ev.dx = direction.x;
        ev.dy = direction.y;
        GameNet.Instance.SendEvent(ev);
    }

    // True when the client asked the host instead of destroying the cube itself.
    public static bool InterceptCollided(DropableCube cube)
    {
        if (!Online || NetSession.IsHost) return false;

        NetCubeObject obj = cube.GetComponentInParent<NetCubeObject>();
        if (obj == null || !obj.IsReplica) return false;

        if (obj.TryRequestGone())
        {
            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.CubeGone;
            ev.id = (uint)obj.id;
            GameNet.Instance.SendEvent(ev);
        }
        return true;
    }

    // Only the host acts on these events.
    public static void OnEvent(GameEventDTO ev, int sender)
    {
        if (!Online || !NetSession.IsHost) return;

        if (ev.kind == GameEventKind.Shoot) SpawnForClient(ev, sender);
        else if (ev.kind == GameEventKind.CubeGone) RemoveCube((int)ev.id);
    }

    static void SpawnForClient(GameEventDTO ev, int sender)
    {
        NetPlayerSync shooter = NetPlayerSync.Find(sender);
        if (shooter == null) return;

        MassCubeInstantiator gun = shooter.Player.GetComponent<MassCubeInstantiator>();
        if (gun == null) return;

        GameObject cube = gun.ShootFromNetwork(new Vector2(ev.x, ev.y), new Vector2(ev.dx, ev.dy));
        OnHostShoot(cube, sender);
    }

    static void RemoveCube(int id)
    {
        GameObject go = GameNet.Instance.Linking.Find(id);
        if (go == null) return;

        DropableCube cube = go.GetComponentInChildren<DropableCube>();
        if (cube != null) cube.Collided();
    }
}
