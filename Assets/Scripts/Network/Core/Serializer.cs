using System;
using System.IO;
using System.Text;
using UnityEngine;

// Turns messages into bytes and back. Every message starts with its MsgType byte, and then
// the fields are written one after another in a fixed order: the reader must read the same
// fields, in the same order and with the same types, because the bytes do not say what they are.
public static class Serializer
{
    const int MaxBones = 32;

    public static MsgType TypeOf(byte[] data)
    {
        if (data == null || data.Length == 0) throw new InvalidDataException("empty message");
        return (MsgType)data[0];
    }

    // ---------------------------------------------------------------------------- Text ---
    public static byte[] Text(string text)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.Text);
        writer.Write(text);
        return stream.ToArray();
    }

    public static string ReadText(byte[] data)
    {
        return Open(data, MsgType.Text).ReadString();
    }

    // --------------------------------------------------------------------- PlayerState ---
    public static byte[] Serialize(PlayerStateDTO dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.PlayerState);
        writer.Write(dto.sequence);
        writer.Write(dto.playerIndex);
        writer.Write(dto.growNum);

        byte flags = 0;
        if (dto.isDead) flags |= 1;
        if (dto.isGrounded) flags |= 2;
        if (dto.aimVisible) flags |= 4;
        writer.Write(flags);

        writer.Write(dto.verticalVelocity);
        writer.Write(dto.aimDirection.x);
        writer.Write(dto.aimDirection.y);

        writer.Write((byte)dto.bonePositions.Length);
        for (int i = 0; i < dto.bonePositions.Length; i++)
        {
            writer.Write(dto.bonePositions[i].x);
            writer.Write(dto.bonePositions[i].y);
            writer.Write(dto.boneAngles[i]);
        }
        return stream.ToArray();
    }

    public static PlayerStateDTO DeserializePlayerState(byte[] data)
    {
        BinaryReader reader = Open(data, MsgType.PlayerState);
        PlayerStateDTO dto = new PlayerStateDTO();
        dto.sequence = reader.ReadUInt16();
        dto.playerIndex = reader.ReadByte();
        dto.growNum = reader.ReadByte();

        byte flags = reader.ReadByte();
        dto.isDead = (flags & 1) != 0;
        dto.isGrounded = (flags & 2) != 0;
        dto.aimVisible = (flags & 4) != 0;

        dto.verticalVelocity = reader.ReadSingle();
        dto.aimDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());

        int count = reader.ReadByte();
        if (count > MaxBones) throw new InvalidDataException("too many bones: " + count);

        dto.bonePositions = new Vector2[count];
        dto.boneAngles = new float[count];
        for (int i = 0; i < count; i++)
        {
            dto.bonePositions[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dto.boneAngles[i] = reader.ReadSingle();
        }
        return dto;
    }

    // ---------------------------------------------------------------------- ObjectState ---
    public static byte[] Serialize(ObjectStateDTO dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.ObjectState);
        writer.Write(dto.id);
        writer.Write(dto.sequence);
        writer.Write(dto.position.x);
        writer.Write(dto.position.y);
        writer.Write(dto.angle);
        return stream.ToArray();
    }

    public static ObjectStateDTO DeserializeObjectState(byte[] data)
    {
        BinaryReader reader = Open(data, MsgType.ObjectState);
        ObjectStateDTO dto = new ObjectStateDTO();
        dto.id = reader.ReadUInt32();
        dto.sequence = reader.ReadUInt16();
        dto.position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        dto.angle = reader.ReadSingle();
        return dto;
    }

    // ---------------------------------------------------------------------------- Event ---
    // 'eventId' is the number the receiver sends back in the Ack.
    public static byte[] Serialize(GameEventDTO ev, ushort eventId)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.Event);
        writer.Write(eventId);
        writer.Write((byte)ev.kind);
        writer.Write(ev.id);
        writer.Write(ev.x);
        writer.Write(ev.y);
        writer.Write(ev.dx);
        writer.Write(ev.dy);
        writer.Write(ev.text ?? "");
        return stream.ToArray();
    }

    public static GameEventDTO DeserializeEvent(byte[] data, out ushort eventId)
    {
        BinaryReader reader = Open(data, MsgType.Event);
        eventId = reader.ReadUInt16();
        GameEventDTO ev = new GameEventDTO();
        ev.kind = (GameEventKind)reader.ReadByte();
        ev.id = reader.ReadUInt32();
        ev.x = reader.ReadSingle();
        ev.y = reader.ReadSingle();
        ev.dx = reader.ReadSingle();
        ev.dy = reader.ReadSingle();
        ev.text = reader.ReadString();
        return ev;
    }

    // ------------------------------------------------------------------------------ Ack ---
    public static byte[] Ack(ushort eventId)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write((byte)MsgType.Ack);
        writer.Write(eventId);
        return stream.ToArray();
    }

    public static ushort ReadAck(byte[] data)
    {
        return Open(data, MsgType.Ack).ReadUInt16();
    }

    // "04 01 00 02 ...": the bytes as hexadecimal, to show them in the README or in the console.
    public static string ToHex(byte[] data)
    {
        if (data == null) return "(null)";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(data[i].ToString("X2"));
        }
        return sb.ToString();
    }

    // Reads the type byte and checks it is the one we expect.
    static BinaryReader Open(byte[] data, MsgType expected)
    {
        BinaryReader reader = new BinaryReader(new MemoryStream(data));
        MsgType type = (MsgType)reader.ReadByte();
        if (type != expected) throw new InvalidDataException("expected " + expected + " but got " + type);
        return reader;
    }
}
