using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Nbt;

namespace Orion.Protocol.Types;

public sealed class ItemInstance : DataType
{
    /// <summary>
    /// Item stack payload.
    /// </summary>
    public LegacyItem Stack = new();

    /// <summary>
    /// Stack network id value.
    /// </summary>
    public int StackNetworkId;

    public void Read(BinaryReader reader)
    {
        NetworkItemStackDescriptor descriptor = new();
        descriptor.Read(reader);
        ApplyDescriptor(descriptor);
    }

    public void Write(BinaryWriter writer)
    {
        ToDescriptor().Write(writer);
    }

    public NetworkItemStackDescriptor ToDescriptor()
    {
        if (Stack.NetworkId == 0 || Stack.StackSize == 0)
        {
            return new NetworkItemStackDescriptor();
        }

        return new NetworkItemStackDescriptor
        {
            NetworkId = Stack.NetworkId,
            Count = Stack.StackSize,
            Metadata = unchecked((uint)Stack.Metadata),
            StackNetworkId = StackNetworkId,
            BlockRuntimeId = Stack.NetworkBlockId,
            Nbt = Stack.ExtraData?.Nbt,
            CanPlaceOn = Stack.ExtraData?.CanPlaceOn ?? [],
            CanDestroy = Stack.ExtraData?.CanDestroy ?? [],
            BlockingTick = Stack.ExtraData?.Ticking ?? 0
        };
    }

    public void ApplyDescriptor(NetworkItemStackDescriptor descriptor)
    {
        if (descriptor.NetworkId == 0 || descriptor.Count == 0)
        {
            Stack = new LegacyItem();
            StackNetworkId = 0;
            return;
        }

        Stack = new LegacyItem
        {
            NetworkId = descriptor.NetworkId,
            StackSize = descriptor.Count,
            Metadata = unchecked((int)descriptor.Metadata),
            ItemStackId = descriptor.StackNetworkId != 0 ? descriptor.StackNetworkId : null,
            NetworkBlockId = descriptor.BlockRuntimeId,
            ExtraData = descriptor.Nbt is null && descriptor.CanPlaceOn.Count == 0 && descriptor.CanDestroy.Count == 0
                ? null
                : new ItemInstanceUserData
                {
                    Nbt = descriptor.Nbt,
                    CanPlaceOn = descriptor.CanPlaceOn,
                    CanDestroy = descriptor.CanDestroy,
                    Ticking = descriptor.BlockingTick
                }
        };
        StackNetworkId = descriptor.StackNetworkId;
    }
}
