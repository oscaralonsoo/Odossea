using System.Collections.Generic;
using UnityEngine;

// The mass cubes a player shoots. Every PC simulates the cube by itself (it is just a physics
// object), and two reliable events keep both PCs in agreement:
//
//   Shoot      the shooter's PC tells the other one "a cube left from here in this direction",
//              and the other PC spawns the same cube
//   CubeGone   whoever absorbs or merges a cube tells the other PC, which removes its copy
//
// The cube gets the same id on both PCs: the first byte is who shot it, the rest a counter.
public static class NetCube
{
    static readonly Dictionary<uint, DropableCube> s_cubes = new Dictionary<uint, DropableCube>();
    static uint s_counter;

    // Called by MassCubeInstantiator right after the LOCAL player shot.
    public static void OnLocalShoot(GameObject cube, Vector2 origin, Vector2 direction)
    {
        if (!NetSession.IsOnline || GameNet.Instance == null || cube == null) return;

        uint id = ((uint)(NetSession.LocalIndex + 1) << 24) | (++s_counter & 0xFFFFFF);
        Register(id, cube);

        GameEventDTO ev = new GameEventDTO();
        ev.kind = GameEventKind.Shoot;
        ev.id = id;
        ev.x = origin.x;
        ev.y = origin.y;
        ev.dx = direction.x;
        ev.dy = direction.y;
        GameNet.Instance.SendEvent(ev);
    }

    // Called by DropableCube.Collided() when a cube disappears on this PC.
    public static void NotifyGone(DropableCube cube)
    {
        if (!NetSession.IsOnline || GameNet.Instance == null) return;

        NetCubeId tag = cube.GetComponent<NetCubeId>();
        if (tag == null) return;
        s_cubes.Remove(tag.id);

        GameEventDTO ev = new GameEventDTO();
        ev.kind = GameEventKind.CubeGone;
        ev.id = tag.id;
        GameNet.Instance.SendEvent(ev);
    }

    public static void Clear() { s_cubes.Clear(); }

    // GameNet forwards here the events it does not handle itself.
    public static void OnEvent(GameEventDTO ev, int sender)
    {
        if (ev.kind == GameEventKind.Shoot) SpawnRemoteCube(ev, sender);
        else if (ev.kind == GameEventKind.CubeGone) RemoveCube(ev.id);
    }

    static void SpawnRemoteCube(GameEventDTO ev, int sender)
    {
        NetPlayerSync shooter = NetPlayerSync.Find(sender);
        if (shooter == null) return;

        MassCubeInstantiator gun = shooter.Player.GetComponent<MassCubeInstantiator>();
        if (gun == null) return;

        GameObject cube = gun.ShootFromNetwork(new Vector2(ev.x, ev.y), new Vector2(ev.dx, ev.dy));
        Register(ev.id, cube);
    }

    static void RemoveCube(uint id)
    {
        DropableCube cube;
        if (!s_cubes.TryGetValue(id, out cube)) return;

        s_cubes.Remove(id);
        if (cube != null) cube.CollidedFromNetwork();      // already gone on the other PC: no new event
    }

    static void Register(uint id, GameObject cube)
    {
        if (cube == null) return;

        DropableCube script = cube.GetComponent<DropableCube>();
        if (script == null) return;

        NetCubeId tag = cube.AddComponent<NetCubeId>();
        tag.id = id;
        s_cubes[id] = script;
    }
}
