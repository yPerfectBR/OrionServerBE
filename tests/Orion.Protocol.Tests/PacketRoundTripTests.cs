using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;

namespace Orion.Protocol.Tests;

public sealed class PacketRoundTripTests
{
    [Fact]
    public void Login_RoundTrip_PreservesFields()
    {
        LoginPacket original = new()
        {
            Protocol = 1001,
            Identity = "eyJ0ZXN0IjoidGVzdCJ9",
            Client = "{\"ThirdPartyName\":\"Test\"}"
        };

        LoginPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.Protocol, decoded.Protocol);
        Assert.Equal(original.Identity, decoded.Identity);
        Assert.Equal(original.Client, decoded.Client);
    }

    [Fact]
    public void NetworkSettings_RoundTrip_PreservesFields()
    {
        NetworkSettingsPacket original = new()
        {
            CompressionThreshold = 256,
            CompressionMethod = CompressionMethod.Zlib,
            ClientThrottle = true,
            ClientThrottleThreshold = 7,
            ClientThrottleScalar = 0.5f
        };

        NetworkSettingsPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.CompressionThreshold, decoded.CompressionThreshold);
        Assert.Equal(original.CompressionMethod, decoded.CompressionMethod);
        Assert.Equal(original.ClientThrottle, decoded.ClientThrottle);
        Assert.Equal(original.ClientThrottleThreshold, decoded.ClientThrottleThreshold);
        Assert.Equal(original.ClientThrottleScalar, decoded.ClientThrottleScalar);
    }

    [Fact]
    public void StartGame_RoundTrip_PreservesCoreFields()
    {
        StartGamePacket original = new()
        {
            EntityUniqueId = 42,
            EntityRuntimeId = 99,
            PlayerGameMode = 0,
            PlayerPosition = new Vec3f { X = 1f, Y = 64f, Z = -2f },
            Pitch = 10f,
            Yaw = 20f,
            WorldName = "Orion",
            LevelId = "OrionWorld",
            BaseGameVersion = "1.26.30",
            GameVersion = "1.26.30",
            ServerEditorConnectionPolicy = 0,
            AllowAnonymousBlockDropsInEditorWorlds = false,
            IsLoggingChat = false
        };

        StartGamePacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.EntityUniqueId, decoded.EntityUniqueId);
        Assert.Equal(original.EntityRuntimeId, decoded.EntityRuntimeId);
        Assert.Equal(original.PlayerPosition.X, decoded.PlayerPosition.X);
        Assert.Equal(original.WorldName, decoded.WorldName);
        Assert.Equal(original.LevelId, decoded.LevelId);
        Assert.Equal(original.IsLoggingChat, decoded.IsLoggingChat);
    }


    [Fact]
    public void LevelSoundEvent_RoundTrip_PreservesStringIdentifier()
    {
        LevelSoundEventPacket original = new()
        {
            Event = LevelSoundEvent.BreakBlock,
            Position = new Vec3f { X = 1f, Y = 64f, Z = 2f },
            Data = 3,
            ActorIdentifier = "minecraft:player",
            IsBabyMob = false,
            IsGlobal = false,
            UniqueActorId = 42
        };

        LevelSoundEventPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal("break.block", decoded.SoundEvent);
        Assert.Equal(original.Position.Y, decoded.Position.Y);
        Assert.Equal(original.Data, decoded.Data);
        Assert.Equal(original.ActorIdentifier, decoded.ActorIdentifier);
    }


    [Fact]
    public void InventoryContent_RoundTrip_PreservesFields()
    {
        InventoryContentPacket original = new()
        {
            WindowId = 7,
            Content =
            [
                new NetworkItemStackDescriptor
                {
                    NetworkId = 1,
                    Count = 16,
                    Metadata = 0,
                    BlockRuntimeId = 0
                }
            ],
            Container = new FullContainerName { ContainerId = (byte)ContainerName.Inventory },
            StorageItem = new NetworkItemStackDescriptor()
        };

        InventoryContentPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.WindowId, decoded.WindowId);
        Assert.Single(decoded.Content);
        Assert.Equal(1, decoded.Content[0].NetworkId);
        Assert.Equal((ushort)16, decoded.Content[0].Count);
    }


    [Fact]
    public void InventorySlot_RoundTrip_PreservesFields()
    {
        InventorySlotPacket original = new()
        {
            WindowId = 12,
            Slot = 3,
            NewItem = new NetworkItemStackDescriptor { NetworkId = 2, Count = 4 }
        };

        InventorySlotPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.WindowId, decoded.WindowId);
        Assert.Equal(original.Slot, decoded.Slot);
        Assert.Equal(2, decoded.NewItem.NetworkId);
        Assert.Equal((ushort)4, decoded.NewItem.Count);
    }

    [Fact]
    public void PlayerAuthInput_RoundTrip_PreservesFields()
    {
        PlayerAuthInputData inputData = new();
        inputData.SetFlag(PlayerAuthInputFlag.Up, true);
        inputData.SetFlag(PlayerAuthInputFlag.Sneaking, true);

        PlayerAuthInputPacket original = new()
        {
            Pitch = 5f,
            Yaw = 15f,
            Position = new Vec3f { X = 0f, Y = 70f, Z = 0f },
            MoveVector = new Vec2f { X = 0.5f, Y = -0.25f },
            InputData = inputData,
            InputMode = InputMode.Mouse,
            PlayMode = PlayMode.Normal,
            InteractionModel = InteractionModel.Crosshair
        };

        PlayerAuthInputPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.Pitch, decoded.Pitch);
        Assert.Equal(original.Yaw, decoded.Yaw);
        Assert.Equal(original.Position.Z, decoded.Position.Z);
        Assert.True(decoded.InputData.HasFlag(PlayerAuthInputFlag.Up));
        Assert.True(decoded.InputData.HasFlag(PlayerAuthInputFlag.Sneaking));
        Assert.Equal(original.InputMode, decoded.InputMode);
    }

    [Fact]
    public void LevelChunk_RoundTrip_PreservesFields()
    {
        LevelChunkPacket original = new()
        {
            ChunkX = 3,
            ChunkZ = -4,
            Dimension = 0,
            SubChunkCount = 2,
            CacheEnabled = false,
            RawPayload = [0x01, 0x02, 0x03, 0x04]
        };

        LevelChunkPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.ChunkX, decoded.ChunkX);
        Assert.Equal(original.ChunkZ, decoded.ChunkZ);
        Assert.Equal(original.SubChunkCount, decoded.SubChunkCount);
        Assert.Equal(original.RawPayload, decoded.RawPayload);
    }

    [Fact]
    public void UpdateBlock_RoundTrip_PreservesNegativeRuntimeId()
    {
        UpdateBlockPacket original = new()
        {
            Position = new BlockPos { X = 1, Y = -60, Z = 2 },
            NetworkBlockId = BedrockBlockStates.GrassBlock,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        };

        UpdateBlockPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal(original.Position.X, decoded.Position.X);
        Assert.Equal(original.Position.Y, decoded.Position.Y);
        Assert.Equal(original.Position.Z, decoded.Position.Z);
        Assert.Equal(BedrockBlockStates.GrassBlock, decoded.NetworkBlockId);
        Assert.Equal(UpdateBlockFlagsType.Network, decoded.Flags);
        Assert.Equal(UpdateBlockLayerType.Normal, decoded.Layer);
    }
}
