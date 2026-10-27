using UnityEngine;

// The state of one soft-body player, sent about 30 times per second (unreliable).
// A lost packet does not matter: the next one replaces it. 'sequence' lets the receiver
// ignore a packet that arrives late and older than the one it already has.
public struct PlayerStateDTO
{
    public ushort sequence;
    public byte playerIndex;        // 0 = Player1 (host), 1 = Player2 (client)
    public byte growNum;            // how many cubes of mass the player has
    public bool isDead;
    public bool isGrounded;         // for the animator of the remote copy
    public float verticalVelocity;  // for the animator of the remote copy
    public bool aimVisible;
    public Vector2 aimDirection;
    public Vector2[] bonePositions;
    public float[] boneAngles;      // degrees
}
