// First byte of every datagram: it says what follows.
public enum MsgType : byte
{
    Text = 1,          // lobby and chat: "JOIN:Anna", "CHAT:hello", "START:1"...
    PlayerState = 2,   // unreliable, ~30 per second: the bones of one player (each PC sends its own)
    ObjectState = 3,   // OLD (Deliverable 3): replaced by Replication. Unused, can stay
    Event = 4,         // reliable (acknowledged and resent): shoot request, cube gone, scene change
    Ack = 5,           // confirms that one Event arrived
    Replication = 6    // unreliable, 20 per second, host to client: world state (Create/Update/Destroy)
}
