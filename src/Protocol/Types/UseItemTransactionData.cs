using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Types;

public sealed class UseItemTransactionData : DataType
{
    /// <summary>
    /// Legacy request id from older inventory flow.
    /// </summary>
    public int LegacyRequestId;

    /// <summary>
    /// Legacy slot updates tied to the legacy request.
    /// </summary>
    public List<LegacySetItemSlot> LegacySetItemSlots = [];

    /// <summary>
    /// Inventory actions included in this transaction.
    /// </summary>
    public List<InventoryAction> Actions = [];

    /// <summary>
    /// Use-item action type.
    /// </summary>
    public int ActionType;

    /// <summary>
    /// Trigger source for this transaction.
    /// </summary>
    public byte TriggerType;

    /// <summary>
    /// Target block position.
    /// </summary>
    public BlockPos BlockPosition;

    /// <summary>
    /// Block face used for the action.
    /// </summary>
    public byte BlockFace;

    /// <summary>
    /// Hotbar slot used by the client.
    /// </summary>
    public int HotBarSlot;

    /// <summary>
    /// Item held by the player.
    /// </summary>
    public ItemInstance HeldItem = new();

    /// <summary>
    /// Player position at action time.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Clicked position relative to target.
    /// </summary>
    public Vec3f ClickedPosition;

    /// <summary>
    /// Block runtime id seen by the client.
    /// </summary>
    public uint BlockRuntimeId;

    /// <summary>
    /// Client-side prediction state.
    /// </summary>
    public byte ClientPrediction;

    /// <summary>
    /// Client cooldown state value.
    /// </summary>
    public byte ClientCooldownState;

    public void Read(BinaryReader reader)
    {
        LegacyRequestId = reader.ReadZigZag();
        LegacySetItemSlots = [];
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            int legacyCount = checked((int)reader.ReadVarUInt());
            LegacySetItemSlots = new List<LegacySetItemSlot>(legacyCount);
            for (int i = 0; i < legacyCount; i++)
            {
                LegacySetItemSlot slot = new();
                slot.Read(reader);
                LegacySetItemSlots.Add(slot);
            }
        }

        int actionCount = checked((int)reader.ReadVarUInt());
        Actions = new List<InventoryAction>(actionCount);
        for (int i = 0; i < actionCount; i++)
        {
            InventoryAction action = new();
            action.Read(reader);
            Actions.Add(action);
        }

        ActionType = reader.ReadZigZag();
        TriggerType = reader.ReadUInt8();
        BlockPos blockPosition = BlockPosition;
        blockPosition.Read(reader);
        BlockPosition = blockPosition;
        BlockFace = reader.ReadUInt8();
        HotBarSlot = reader.ReadZigZag();
        HeldItem.Read(reader);
        Position.Read(reader);
        ClickedPosition.Read(reader);
        BlockRuntimeId = reader.ReadVarUInt();
        ClientPrediction = reader.ReadUInt8();
        ClientCooldownState = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(LegacyRequestId);
        if (LegacyRequestId < -1 && (LegacyRequestId & 1) == 0)
        {
            writer.WriteVarUInt((uint)LegacySetItemSlots.Count);
            for (int i = 0; i < LegacySetItemSlots.Count; i++)
            {
                LegacySetItemSlots[i].Write(writer);
            }
        }

        writer.WriteVarUInt((uint)Actions.Count);
        for (int i = 0; i < Actions.Count; i++)
        {
            Actions[i].Write(writer);
        }

        writer.WriteZigZag(ActionType);
        writer.WriteUInt8(TriggerType);
        BlockPosition.Write(writer);
        writer.WriteUInt8(BlockFace);
        writer.WriteZigZag(HotBarSlot);
        HeldItem.Write(writer);
        Position.Write(writer);
        ClickedPosition.Write(writer);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteUInt8(ClientPrediction);
        writer.WriteUInt8(ClientCooldownState);
    }
}
