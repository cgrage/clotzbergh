using UnityEngine;

public readonly struct PlayerId
{
    private readonly int _value;

    public PlayerId(int value)
    {
        _value = value;
    }

    public int Value => _value;

    public override string ToString() => _value.ToString();

    // public static implicit operator int(PlayerId id) => id._value;
    public static explicit operator PlayerId(int value) => new(value);
}

public class PlayerData
{
    // public IClientOps Handler { get; set; }
}

public class WorldChunkUpdate
{
    public Vector3Int Coords { get; set; }
    public ulong Version { get; set; }
    public WorldChunk Chunk { get; set; }
}
