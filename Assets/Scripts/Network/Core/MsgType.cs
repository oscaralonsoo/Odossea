// First byte of every datagram: it says what follows.
public enum MsgType : byte
{
    Text = 1,          // lobby and chat: "JOIN:Anna", "CHAT:hello", "START:1"...
    PlayerState = 2,   // unreliable, ~30 per second: the bones of one player
    ObjectState = 3,   // unreliable, ~15 per second: platforms and doors (host to client)
    Event = 4,         // reliable (acknowledged and resent): shoot, cube gone, scene change
    Ack = 5            // confirms that one Event arrived
}
