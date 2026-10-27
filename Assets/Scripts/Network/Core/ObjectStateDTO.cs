using UnityEngine;

// The position of a world object that the host owns (moving platform, door).
// Sent about 15 times per second, unreliable, like the player state.
public struct ObjectStateDTO
{
    public uint id;          // same on both PCs: see NetObjectSync.ComputeId
    public ushort sequence;
    public Vector2 position;
    public float angle;      // degrees
}
