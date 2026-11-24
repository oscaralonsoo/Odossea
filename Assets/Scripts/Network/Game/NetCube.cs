using UnityEngine;

// The mass cubes. With replication the HOST is the only one that simulates them:
//
//   host shoots     the cube is created and simulated here, the replication sends it to the client
//   client shoots   it sends a Shoot event ("I shoot from here, in this direction"), the host
//                   creates the cube and it comes back as a Create
//   client absorbs  it sends a CubeGone event, the host removes the cube and the Destroy comes back
//
// The two events are the one-shot things that are not "state" (Deliverable 4: events are not replicated).
public static class NetCube
{
    static bool Online { get { return NetSession.IsOnline && GameNet.Instance != null; } }

    // HOST. Right after a cube was shot here (by the host's player or on behalf of the client).
    public static void OnHostShoot(GameObject cube, int ownerIndex)
    {
        if (!Online || !NetSession.IsHost || cube == null) return;

        NetCubeObject obj = cube.GetComponent<NetCubeObject>();
        if (obj == null) return;

        obj.owner = (byte)ownerIndex;
        GameNet.Instance.RegisterNew(obj);          // ID + MarkCreate
    }

    // CLIENT. The local player pressed shoot: the host will create the cube.
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

    // Called by DropableCube.Collided() first. true = "I took care of it, do not destroy it here".
    // On the client a replicated cube is not removed by the client: the host decides.
    public static bool InterceptCollided(DropableCube cube)
    {
        if (!Online || NetSession.IsHost) return false;

        NetCubeObject obj = cube.GetComponentInParent<NetCubeObject>();
        if (obj == null || !obj.IsReplica) return false;      // a cube of this PC only (dispersed ones)

        if (obj.TryRequestGone())
        {
            GameEventDTO ev = new GameEventDTO();
            ev.kind = GameEventKind.CubeGone;
            ev.id = (uint)obj.id;
            GameNet.Instance.SendEvent(ev);
        }
        return true;
    }

    // GameNet forwards here the events it does not handle itself. Only the host acts on them.
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
        if (go == null) return;                     // already gone

        DropableCube cube = go.GetComponentInChildren<DropableCube>();
        if (cube != null) cube.Collided();          // the host destroys it, the Destroy goes to the client
    }
}
