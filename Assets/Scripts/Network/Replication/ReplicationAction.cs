// What the server says about an object in one entry of a replication packet.
public enum ReplicationAction : byte
{
    Create = 1,
    Update = 2,
    Destroy = 3
}
