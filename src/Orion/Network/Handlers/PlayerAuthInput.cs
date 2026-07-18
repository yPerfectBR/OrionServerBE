using Orion.RakNet;
namespace Orion.Network.Handlers;

using System.Collections.Concurrent;
using System.Linq;
using Orion;
using Orion.Scheduling;
using Orion.Player;
using Orion.Block.Traits.Types;
using Orion.Entity.Traits;
using Orion.Entity.Traits.Types;
using Orion.Events;
using Orion.Item;
using Orion.Item.Traits;
using Orion.Item.Traits.Types;
using Orion.Player.Traits;

using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.RakNet;
using Orion.World;

public static class PlayerAuthInput
{
    private const float MaxHorizontalMovePerTick = 2.0f;
    private const float MaxBlockReachDistance = 6.5f;
    private const ulong DefaultFoodUseTicks = 32UL;
    private const ulong MovementGraceTicks = 5UL;

    private static readonly ConcurrentDictionary<ulong, ulong> LastInputTickByRuntimeId = new();
    private static readonly ConcurrentDictionary<ulong, ulong> MovementGraceUntilTickByRuntimeId = new();
    private static readonly ConcurrentDictionary<ulong, PendingItemUse> PendingItemUses = new();

    private readonly record struct PendingItemUse(int Slot, int StackNetworkId, ulong FinishTick);

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerAuthInputPacket packet = new();
        try
        {
            int offset = 0;
            BinaryReader reader = new(packetBuffer, ref offset);
            packet = (PlayerAuthInputPacket)Protocol.Io.Packet.Deserialize(reader);
        }
        catch (Exception exception)
        {
            Warn("PlayerAuthInput deserialize failed: {0}", exception);
            return;
        }

        try
        {
            if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
            {
                return;
            }

            player.FlushClientWorldStateSyncIfPending();

#if DEBUG
            if (player.Dimension is Dimension authDimension
                && authDimension.UsesRegionThreading()
                && player.Dimension.World is global::Orion.World.World authWorld)
            {
                ThreadGuard.AssertSimulationThread(authDimension, authWorld);
            }
#endif

            bool movementRejected = MovedTooFar(player, packet, out ulong tickDelta);
            if (movementRejected)
            {
                Warn($"Player {player.Username} moved too fast ({packet.Position.X}, {packet.Position.Y}, {packet.Position.Z}) tickDelta={tickDelta}");

                CorrectPlayerMovePredictionPacket correction = new()
                {
                    PredictionType = PredictionType.Player,
                    Position = player.Position,
                    PositionDelta = new Vec3f { X = 0f, Y = 0f, Z = 0f },
                    Rotation = new Vec2f { X = packet.Pitch, Y = packet.Yaw },
                    VehicleAngularVelocity = new OptionalValue<float> { HasValue = false },
                    OnGround = packet.InputData.HasFlag(PlayerAuthInputFlag.VerticalCollision),
                    InputTick = packet.Tick
                };

                if (player.Session is not null)
                {
                    player.Session.Send(correction);
                }
                else
                {
                    server.Network.SendPacket(connection, correction);
                }
            }
            else if (player.Session?.TransferState != TransferState.Transferring)
            {
                MovePlayer(server, player, packet);
            }

            ProcessGameplayInput(server, connection, player, packet);
            LastInputTickByRuntimeId[player.RuntimeId] = packet.Tick;
        }
        catch (Exception exception)
        {
            Warn("PlayerAuthInput handler failed: {0}", exception);
        }
    }

    private static void StartUsingItem(global::Orion.Player.Player player)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();
        ItemStackFoodTrait? food = heldItem?.GetTrait<ItemStackFoodTrait>();
        if (inventory is null || heldItem is null || food is null)
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (hunger is null || (!food.CanAlwaysEat && hunger.CurrentValue >= hunger.MaximumValue))
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        ulong currentTick = GetCurrentTick(player);
        ulong useTicks = GetUseDurationTicks(heldItem);
        PendingItemUses[player.RuntimeId] = new PendingItemUse(
            inventory.SelectedSlot,
            heldItem.NetworkStackId,
            currentTick + Math.Max(1UL, useTicks));

        player.Flags.SetActorFlag(ActorFlag.UsingItem, true);
    }

    private static void TickPendingItemUse(global::Orion.Player.Player player)
    {
        if (!PendingItemUses.TryGetValue(player.RuntimeId, out PendingItemUse pending))
        {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.Container.GetItem(pending.Slot);
        if (inventory is null || heldItem is null || heldItem.NetworkStackId != pending.StackNetworkId)
        {
            PendingItemUses.TryRemove(player.RuntimeId, out _);
            player.Flags.SetActorFlag(ActorFlag.UsingItem, false);
            return;
        }

        if (GetCurrentTick(player) < pending.FinishTick)
        {
            return;
        }

        PendingItemUses.TryRemove(player.RuntimeId, out _);
        player.Flags.SetActorFlag(ActorFlag.UsingItem, false);

        ItemStackFoodTrait? food = heldItem.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (food is null || hunger is null || !hunger.Eat(food.Nutrition, food.SaturationModifier, food.CanAlwaysEat))
        {
            return;
        }

        heldItem.DecrementStack();
        if (heldItem.StackSize == 0)
        {
            inventory.Container.ClearSlot(pending.Slot);
        }
        else
        {
            inventory.Container.UpdateSlot(pending.Slot);
        }

        if (!string.IsNullOrWhiteSpace(food.UsingConvertsTo) && ItemType.Get(food.UsingConvertsTo) is ItemType convertedType)
        {
            ItemStack converted = new(convertedType);
            if (!inventory.Container.AddItem(converted))
            {
                _ = player.DropItem(converted);
            }
        }

        player.SendAttributes();
    }

    private static ulong GetCurrentTick(global::Orion.Player.Player player)
    {
        return player.Dimension?.World is Orion.World.Tickable tickable ? tickable.TickValue : 0UL;
    }

    private static ulong GetUseDurationTicks(ItemStack item)
    {
        if (item.Type.TryGetComponentProperties("minecraft:use_duration", out Orion.Protocol.Nbt.CompoundTag tag))
        {
            return (ulong)Math.Max(1, tag.Get<Orion.Protocol.Nbt.IntTag>("value")?.Value ?? (int)DefaultFoodUseTicks);
        }

        return DefaultFoodUseTicks;
    }

    private static ItemStackResponse ProcessItemStackRequest(global::Orion.Player.Player player, Protocol.Types.ItemStackRequest request)
    {
        List<StackResponseContainerInfo> containers = [];

        for (int i = 0; i < request.Actions.Count; i++)
        {
            if (request.Actions[i] is not MineBlockStackRequestAction mineBlock)
            {
                continue;
            }

            EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? item = inventory?.Container.GetItem(mineBlock.HotbarSlot);

            containers.Add(new StackResponseContainerInfo
            {
                Container = new FullContainerName { ContainerId = 29 },
                SlotInfo =
                [
                    new StackResponseSlotInfo
                    {
                        Slot = (byte)mineBlock.HotbarSlot,
                        HotbarSlot = (byte)mineBlock.HotbarSlot,
                        Count = (byte)(item?.StackSize ?? 0),
                        StackNetworkId = item?.NetworkStackId ?? 0,
                        CustomName = string.Empty,
                        FilteredCustomName = string.Empty,
                        DurabilityCorrection = 0
                    }
                ]
            });
        }

        return new ItemStackResponse
        {
            Status = ItemStackResponseStatus.Ok,
            RequestId = request.RequestId,
            ContainerInfo = containers
        };
    }

    private static MineBlockStackRequestAction? GetMineBlockRequest(Protocol.Types.ItemStackRequest request)
    {
        for (int i = 0; i < request.Actions.Count; i++)
        {
            if (request.Actions[i] is MineBlockStackRequestAction mineBlock)
            {
                return mineBlock;
            }
        }

        return null;
    }

    public static void ResetMovementValidation(ulong runtimeId)
    {
        LastInputTickByRuntimeId.TryRemove(runtimeId, out _);
        MovementGraceUntilTickByRuntimeId.TryRemove(runtimeId, out _);
    }

    public static void BeginMovementGrace(ulong runtimeId, ulong currentTick)
    {
        MovementGraceUntilTickByRuntimeId[runtimeId] = currentTick + MovementGraceTicks;
    }

    private static void ProcessGameplayInput(
        Server server,
        NetworkConnection connection,
        global::Orion.Player.Player player,
        PlayerAuthInputPacket packet)
    {
        TickPendingItemUse(player);

        if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemInteraction))
        {
            InventoryTransaction.HandleUseItemFromAuthInput(
                player,
                packet.ItemInteractionData,
                packet.InteractPitch,
                packet.InteractYaw);
        }

        MineBlockStackRequestAction? mineBlockRequest = null;
        if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformItemStackRequest))
        {
            mineBlockRequest = GetMineBlockRequest(packet.ItemStackRequest);
            bool onlyMineBlock = packet.ItemStackRequest.Actions.All(static action =>
                action is MineBlockStackRequestAction
                    or EmptyStackRequestAction
                    or CraftResultsDeprecatedStackRequestAction);

            Warn(
                "PlayerAuthInput item stack request player={0} request={1} actions={2} mineBlock={3}",
                player.Username,
                packet.ItemStackRequest.RequestId,
                packet.ItemStackRequest.Actions.Count,
                mineBlockRequest is not null);

            ItemStackResponse response = onlyMineBlock
                ? ProcessItemStackRequest(player, packet.ItemStackRequest)
                : ItemStackRequest.Process(player, packet.ItemStackRequest);

            ItemStackResponsePacket stackResponse = new()
            {
                Responses = [response]
            };

            if (player.Session is not null)
            {
                player.Session.Send(stackResponse);
            }
            else
            {
                server.Network.SendPacket(connection, stackResponse);
            }
        }

        if (packet.InputData.HasFlag(PlayerAuthInputFlag.PerformBlockActions))
        {
            foreach (PlayerBlockAction action in packet.BlockActions)
            {
                HandleBlockAction(player, action);
            }
        }

        if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartUsingItem))
        {
            StartUsingItem(player);
        }

        if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSprinting))
        {
            player.IsSprinting = true;
        }
        else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSprinting))
        {
            player.IsSprinting = false;
        }

        if (packet.InputData.HasFlag(PlayerAuthInputFlag.StartSneaking))
        {
            player.IsSneaking = true;
        }
        else if (packet.InputData.HasFlag(PlayerAuthInputFlag.StopSneaking))
        {
            player.IsSneaking = false;
        }
        else if (mineBlockRequest is not null && player.LastActionBlockPosition.HasValue)
        {
            BlockPos position = player.LastActionBlockPosition.Value;
            Warn(
                "PlayerAuthInput mine fallback player={0} pos={1},{2},{3} face={4}",
                player.Username,
                position.X,
                position.Y,
                position.Z,
                player.LastActionFace);

            DestroyBlock(player, new PlayerBlockAction
            {
                Action = PlayerActionType.PredictDestroyBlock,
                BlockPos = position,
                Face = player.LastActionFace
            });
        }
        else if (mineBlockRequest is not null)
        {
            Warn("PlayerAuthInput mine request had no block actions and no last PlayerAction target player={0}", player.Username);
        }
    }

    private static bool MovedTooFar(global::Orion.Player.Player player, PlayerAuthInputPacket packet, out ulong rawTickDelta)
    {
        float deltaX = packet.Position.X - player.Position.X;
        float deltaZ = packet.Position.Z - player.Position.Z;
        float movedDistanceSquared = deltaX * deltaX + deltaZ * deltaZ;

        ulong previousTick = LastInputTickByRuntimeId.GetOrAdd(player.RuntimeId, packet.Tick);
        rawTickDelta = packet.Tick > previousTick ? packet.Tick - previousTick : 1UL;

        float tickDelta = Math.Clamp((float)rawTickDelta, 1f, 20f);
        float allowedDistance = MaxHorizontalMovePerTick * tickDelta;

        ulong currentTick = GetCurrentTick(player);
        if (MovementGraceUntilTickByRuntimeId.TryGetValue(player.RuntimeId, out ulong graceUntil)
            && currentTick <= graceUntil)
        {
            allowedDistance *= 2f;
        }

        return movedDistanceSquared > allowedDistance * allowedDistance;
    }

    private static void MovePlayer(Server server, global::Orion.Player.Player player, PlayerAuthInputPacket packet)
    {
        Vec3f previousPosition = player.Position;

        MovementRotation fromRotation = new MovementRotation()
        {
            HeadYaw = player.HeadYaw,
            Pitch = player.Pitch,
            Yaw = player.Yaw,
        };

        MovementRotation toRotation = new MovementRotation()
        {
            HeadYaw = packet.Yaw,
            Pitch = packet.Pitch,
            Yaw = packet.Yaw,
        };

        player.Pitch = packet.Pitch;
        player.Yaw = packet.Yaw;
        player.HeadYaw = packet.Yaw;

        bool missingPosition =
            packet.Position.X == 0f &&
            packet.Position.Y == 0f &&
            packet.Position.Z == 0f;

        bool hasDelta =
            packet.Delta.X != 0f ||
            packet.Delta.Y != 0f ||
            packet.Delta.Z != 0f;

        player.Position = missingPosition && hasDelta
            ? new Vec3f
            {
                X = previousPosition.X + packet.Delta.X,
                Y = previousPosition.Y + packet.Delta.Y,
                Z = previousPosition.Z + packet.Delta.Z
            }
            : packet.Position;

        player.OnMove(new EntityMoveOptions(previousPosition, player.Position, fromRotation, toRotation));

        if (AreaBorderTransfer.TryAfterMove(server, player, previousPosition))
        {
            return;
        }

        if (player.Dimension is not null)
        {
            bool positionChanged = previousPosition.X != player.Position.X
                || previousPosition.Y != player.Position.Y
                || previousPosition.Z != player.Position.Z;
            bool rotationChanged = fromRotation.Pitch != toRotation.Pitch
                || fromRotation.Yaw != toRotation.Yaw
                || fromRotation.HeadYaw != toRotation.HeadYaw;

            if (positionChanged || rotationChanged)
            {
                int viewDistance = player.GetTrait<PlayerChunkRenderingTrait>()?.ViewDistance ?? 16;
                int simulationDistance = Math.Clamp(server.Properties.SimulationDistance, 0, 120);
                float broadcastRadius = Math.Max(viewDistance, simulationDistance) * 16f;

                player.Dimension.Broadcast(
                    new MoveActorDeltaPacket
                    {
                        EntityRuntimeId = player.RuntimeId,
                        Flags = (ushort)MoveDeltaFlags.All,
                        Position = player.Position,
                        Rotation = new Vec3f
                        {
                            X = toRotation.Pitch,
                            Y = toRotation.Yaw,
                            Z = toRotation.HeadYaw
                        }
                    },
                    new BroadcastOptions
                    {
                        Except = [player],
                        Center = player.Position,
                        Radius = broadcastRadius
                    });
            }
        }
    }

    private static void HandleBlockAction(global::Orion.Player.Player player, PlayerBlockAction action)
    {
        // Warn(
        //     "PlayerAuthInput block action player={0} action={1} pos={2},{3},{4} face={5}",
        //     player.Username,
        //     action.Action,
        //     action.BlockPos.X,
        //     action.BlockPos.Y,
        //     action.BlockPos.Z,
        //     action.Face);

        switch (action.Action)
        {
            case PlayerActionType.StartDestroyBlock:
                CrackBlock(player, action.BlockPos);
                break;

            case PlayerActionType.CrackBlock:
            case PlayerActionType.ContinueDestroyBlock:
                CrackBlock(player, action.BlockPos);
                break;

            case PlayerActionType.AbortDestroyBlock:
                StopCrackBlock(player, player.BreakingBlock ?? action.BlockPos);
                player.BreakingBlock = null;
                break;

            case PlayerActionType.StopDestroyBlock:
            case PlayerActionType.PredictDestroyBlock:
            case PlayerActionType.CreativeDestroyBlock:
                DestroyBlock(player, action);
                break;
        }
    }

    private static void CrackBlock(global::Orion.Player.Player player, BlockPos blockPosition)
    {
        if (!IsBlockInReach(player, blockPosition))
        {
            return;
        }

        if (player.BreakingBlock.HasValue && !SameBlock(player.BreakingBlock.Value, blockPosition))
        {
            StopCrackBlock(player, player.BreakingBlock.Value);
        }

        player.BreakingBlock = blockPosition;
        int breakTimeTicks = GetBreakTimeTicksForAnimation(player, blockPosition);
        int crackSpeed = Math.Max(1, 65535 / breakTimeTicks);

        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StartBlockCracking,
            Position = CenterOf(blockPosition),
            Data = crackSpeed
        });
    }

    private static bool IsAirBlock(Orion.Block.BlockPermutation block) =>
        block.Type.Identifier is "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air";

    private static void DestroyBlock(global::Orion.Player.Player player, PlayerBlockAction action)
    {
        if (IsZero(action.BlockPos) && !player.BreakingBlock.HasValue)
        {
            // Warn("PlayerAuthInput destroy skipped player={0} reason=zero-position-no-target action={1}", player.Username, action.Action);
            return;
        }

        BlockPos blockPosition = IsZero(action.BlockPos)
            ? player.BreakingBlock!.Value
            : action.BlockPos;

        if (!IsBlockInReach(player, blockPosition))
        {
            return;
        }

        StopCrackBlock(player, blockPosition);
        player.BreakingBlock = null;

        if (player.Dimension is null)
        {
            return;
        }

        Orion.Block.BlockPermutation? block =
            player.Dimension.GetGameplayPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            return;
        }

        if (IsAirBlock(block) && player.Gamemode == Gamemode.Creative)
        {
            EntityInventoryTrait? creativeInventory = player.GetTrait<EntityInventoryTrait>();
            ItemStack? creativeHeldItem = creativeInventory?.GetHeldItem();
            int effectRuntime = creativeHeldItem is not null ? ItemBlockRuntimeIds.Resolve(creativeHeldItem.Type) : 0;

            if (effectRuntime == 0)
            {
                return;
            }

            Vec3f creativeBlockCenter = CenterOf(blockPosition);

            player.Dimension.Broadcast(new LevelEventPacket
            {
                Event = LevelEvent.ParticlesDestroyBlock,
                Position = creativeBlockCenter,
                Data = effectRuntime
            });

            player.Dimension.Broadcast(new LevelSoundEventPacket
            {
                Event = LevelSoundEvent.BreakBlock,
                Position = creativeBlockCenter,
                Data = effectRuntime,
                ActorIdentifier = string.Empty,
                BabyMob = false,
                DisableRelativeVolume = false,
                UniqueActorId = 0,
                FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
            });
            return;
        }

        Server? server = player.Dimension.World?.Server as Server;
        if (server is not null)
        {
            PlayerBreakBlockSignal signal = new(player, blockPosition, action.Face);
            server.Emit(signal);
            if (!signal.Emit())
            {
                player.Send(new UpdateBlockPacket
                {
                    Position = blockPosition,
                    NetworkBlockId = block.NetworkId,
                    Flags = UpdateBlockFlagsType.Network,
                    Layer = UpdateBlockLayerType.Normal
                });

                EntityInventoryTrait? cancelInventory = player.GetTrait<EntityInventoryTrait>();
                if (cancelInventory is not null)
                {
                    ItemStack? rollbackItem = cancelInventory.GetHeldItem();
                    if (rollbackItem is not null)
                    {
                        cancelInventory.Container.SetItem(cancelInventory.SelectedSlot, rollbackItem.Clone());
                    }
                    cancelInventory.Container.UpdateSlot(cancelInventory.SelectedSlot);
                    cancelInventory.Container.Update();
                    cancelInventory.SyncToPlayer(player);
                }
                return;
            }
        }

        Vec3f blockCenter = CenterOf(blockPosition);

        player.Dimension.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.ParticlesDestroyBlock,
            Position = blockCenter,
            Data = block.NetworkId
        });

        player.Dimension.Broadcast(new LevelSoundEventPacket
        {
            Event = LevelSoundEvent.BreakBlock,
            Position = blockCenter,
            Data = block.NetworkId,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        });

        Orion.Block.BlockPermutation air = Orion.Block.BlockType
            .GetOrAir("minecraft:air")
            .GetPermutation();

        Orion.Block.Block breakingBlock =
            player.Dimension.GetBlock(blockPosition.X, blockPosition.Y, blockPosition.Z) ??
            new Orion.Block.Block(block);

        breakingBlock.OnBreak(new BlockBreakDetails(player, blockPosition));

        player.Dimension.SetGameplayPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z, air);

        player.Dimension.Broadcast(new UpdateBlockPacket
        {
            Position = blockPosition,
            NetworkBlockId = air.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStack? heldItem = inventory?.GetHeldItem();

        if (inventory is not null && heldItem is not null)
        {
            heldItem.OnBreakBlock(new ItemBreakBlockDetails(
                player,
                inventory.SelectedSlot,
                blockPosition,
                action.Face));
        }
    }

    private static void StopCrackBlock(global::Orion.Player.Player player, BlockPos blockPosition)
    {
        player.Dimension?.Broadcast(new LevelEventPacket
        {
            Event = LevelEvent.StopBlockCracking,
            Position = CenterOf(blockPosition),
            Data = 0
        });
    }

    private static int GetBreakTimeTicksForAnimation(global::Orion.Player.Player player, BlockPos blockPosition)
    {
        Orion.Block.BlockPermutation? block =
            player.Dimension?.GetGameplayPermutation(blockPosition.X, blockPosition.Y, blockPosition.Z);

        if (block is null)
        {
            return 20;
        }

        float hardness = block.Type.Hardness;
        if (hardness < 0f)
        {
            return 9999;
        }

        if (hardness == 0f)
        {
            return 1;
        }

        return Math.Max(1, (int)(hardness * 1.5f * 20f));
    }

    private static Vec3f CenterOf(BlockPos position)
    {
        return new Vec3f
        {
            X = position.X + 0.5f,
            Y = position.Y + 0.5f,
            Z = position.Z + 0.5f
        };
    }

    private static bool SameBlock(BlockPos a, BlockPos b)
    {
        return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    }

    private static bool IsZero(BlockPos position)
    {
        return position.X == 0 && position.Y == 0 && position.Z == 0;
    }

    private static bool IsBlockInReach(global::Orion.Player.Player player, BlockPos blockPosition)
    {
        Vec3f blockCenter = CenterOf(blockPosition);
        float deltaX = blockCenter.X - player.Position.X;
        float deltaY = blockCenter.Y - player.Position.Y;
        float deltaZ = blockCenter.Z - player.Position.Z;
        float distanceSquared = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
        return distanceSquared <= MaxBlockReachDistance * MaxBlockReachDistance;
    }
}










