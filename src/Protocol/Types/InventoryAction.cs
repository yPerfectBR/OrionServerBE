using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Types;

public sealed class InventoryAction : DataType
{
    public const uint SourceContainer = 0;
    public const uint SourceWorld = 2;
    public const uint SourceCreative = 3;
    public const uint SourceTodo = 99999;

    /// <summary>
    /// Source type of this action.
    /// </summary>
    public uint SourceType;

    /// <summary>
    /// Window id for container sources.
    /// </summary>
    public sbyte WindowId;

    /// <summary>
    /// Source flags for world sources.
    /// </summary>
    public uint SourceFlags;

    /// <summary>
    /// Slot index affected by the action.
    /// </summary>
    public uint InventorySlot;

    /// <summary>
    /// Item state before the action.
    /// </summary>
    public ItemInstance OldItem = new();

    /// <summary>
    /// Item state after the action.
    /// </summary>
    public ItemInstance NewItem = new();

    public void Read(BinaryReader reader)
    {
        SourceType = reader.ReadVarUInt();
        _ = reader.ReadBool();
        bool hasContainerId = SourceType is SourceContainer or SourceTodo;
        if (hasContainerId)
        {
            WindowId = unchecked((sbyte)reader.ReadUInt8());
        }

        _ = reader.ReadBool();
        bool hasFlags = SourceType == SourceWorld;
        if (hasFlags)
        {
            SourceFlags = reader.ReadVarUInt();
        }

        InventorySlot = reader.ReadVarUInt();
        OldItem.Read(reader);
        NewItem.Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarUInt(SourceType);
        writer.WriteBool(true);
        bool hasContainerId = SourceType is SourceContainer or SourceTodo;
        writer.WriteBool(hasContainerId);
        if (hasContainerId)
        {
            writer.WriteUInt8(unchecked((byte)WindowId));
        }

        writer.WriteBool(true);
        bool hasFlags = SourceType == SourceWorld;
        writer.WriteBool(hasFlags);
        if (hasFlags)
        {
            writer.WriteVarUInt(SourceFlags);
        }

        writer.WriteVarUInt(InventorySlot);
        OldItem.Write(writer);
        NewItem.Write(writer);
    }
}
