using System.Runtime.CompilerServices;
using Orion.Api;
using Orion.Api.Blocks;
using Orion.Api.Network;
using Orion.Block;
using Orion.Scheduling;
using Orion.World;
using Orion.World.Block;
using Orion.World.Coordinates;
using GameplayBlock = Orion.Block.Block;
using GameplayEntity = Orion.Entity.Entity;
using ApiVec3f = Orion.Api.Math.Vec3f;
using ProtocolVec3f = Orion.Protocol.Types.Vec3f;
using ApiSpawnOptions = Orion.Api.EntitySpawnOptions;
using CoreSpawnOptions = Orion.Entity.Traits.Types.EntitySpawnOptions;
using WorldInstance = Orion.World.World;
using Tickable = Orion.World.Tickable;
using ChunkColumn = Orion.World.Chunk.Chunk;
using BlockPos = Orion.Protocol.Types.BlockPos;
using BlockPermutation = Orion.Block.BlockPermutation;

namespace Orion.Plugins.Api;

internal static class WorldApi
{
    private static readonly ConditionalWeakTable<WorldInstance, WorldFacade> Worlds = new();

    public static IWorld For(WorldInstance world) => Worlds.GetValue(world, static w => new WorldFacade(w));
}

internal sealed class WorldFacade(WorldInstance world) : IWorld
{
    internal WorldInstance Inner => world;

    public string Name => world.Name;

    public double TickWork => world.TickWork;

    public int? AttachedWorkerId => world.AttachedWorkerId;

    public IServer? Server => world.Server as IServer;

    public IDimension? GetDimension(string name)
    {
        Dimension? dimension = world.GetDimension(name);
        return dimension is null ? null : DimensionApi.For(dimension);
    }

    public IReadOnlyCollection<IDimension> Dimensions
    {
        get
        {
            List<IDimension> list = [];
            foreach (Dimension dimension in world.Dimensions)
            {
                list.Add(DimensionApi.For(dimension));
            }

            return list;
        }
    }
}

internal static class DimensionApi
{
    private static readonly ConditionalWeakTable<Dimension, DimensionFacade> Dimensions = new();

    public static IDimension For(Dimension dimension) =>
        Dimensions.GetValue(dimension, static d => new DimensionFacade(d));

    public static Dimension? TryUnwrap(IDimension? dimension) =>
        dimension is DimensionFacade facade ? facade.Inner : null;
}

internal sealed class DimensionFacade(Dimension dimension) : IDimension
{
    internal Dimension Inner => dimension;

    public string Name => dimension.Identifier;

    public IWorld World =>
        dimension.World is null
            ? throw new InvalidOperationException("Dimension is not attached to a world.")
            : WorldApi.For(dimension.World);

    public ulong CurrentTick =>
        dimension.World is Tickable tickable ? tickable.TickValue : 0UL;

    public int Difficulty => (int)dimension.GetDifficulty();

    public bool DrowningDamage => dimension.Gamerules.DrowningDamage;

    public IBlock? GetBlock(int x, int y, int z, int layer = 0) =>
        dimension.GetBlock(x, y, z, layer);

    public void SetBlock(int x, int y, int z, IBlock block, int layer = 0, bool dirty = true)
    {
        if (block is not GameplayBlock gameplay)
        {
            throw new ArgumentException("Block must be an Orion.Block.Block instance.", nameof(block));
        }

        dimension.SetBlock(x, y, z, gameplay, layer, dirty);
    }

    public IBlockPermutation GetPermutation(int x, int y, int z, int layer = 0) =>
        dimension.GetGameplayPermutation(x, y, z, layer);

    public void SetPermutation(int x, int y, int z, IBlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        if (permutation is not BlockPermutation gameplay)
        {
            throw new ArgumentException("Permutation must be an Orion.Block.BlockPermutation instance.", nameof(permutation));
        }

        dimension.SetGameplayPermutation(x, y, z, gameplay, layer, dirty);
    }

    public IEntity SpawnEntity(string typeIdentifier, ApiVec3f position, ApiSpawnOptions? options = null)
    {
        GameplayEntity entity = new(typeIdentifier)
        {
            Position = new ProtocolVec3f(position.X, position.Y, position.Z)
        };
        entity.Spawn(dimension, new CoreSpawnOptions(InitialSpawn: options?.InitialSpawn ?? true));
        return entity;
    }

    public IReadOnlyCollection<IEntity> GetEntities()
    {
        List<IEntity> list = [];
        foreach (GameplayEntity entity in dimension.GetEntities())
        {
            list.Add(entity);
        }

        return list;
    }

    public void Broadcast(IOutboundPacket packet, PacketBroadcastOptions? options = null)
    {
        BroadcastOptions? broadcastOptions = null;
        if (options?.MaxDistance is double maxDistance)
        {
            broadcastOptions = new BroadcastOptions { Radius = (float)maxDistance };
        }

        dimension.Broadcast(OutboundPacketAdapter.ToDataPacket(packet), broadcastOptions);
    }

    public int ChunkCount => dimension.ChunkCount;

    public int DimensionNetworkId => (int)dimension.Type;

    public bool IsSessionThreadingEnabled =>
        dimension.World?.Server is Server server && server.Properties.SessionThreadingEnabled;

    public void RequestChunkPayloads(ReadOnlySpan<(int X, int Z)> chunks, Action<int, int, byte[], uint> onReady)
    {
        dimension.RequestChunks(chunks, chunk =>
        {
            byte[] payload = ChunkColumn.Serialize(chunk);
            onReady(chunk.X, chunk.Z, payload, (uint)chunk.GetSubChunkSendCount());
        });
    }

    public void AddChunkViewer(int x, int z) => dimension.AddChunkViewer(x, z);

    public bool RemoveChunkViewer(int x, int z) => dimension.RemoveChunkViewer(x, z);

    public bool HasChunkViewers(int x, int z) => dimension.HasChunkViewers(x, z);

    public bool UnloadChunk(int x, int z) => dimension.UnloadChunk(x, z);

    public void SetPlayerChunkIndex(IPlayer player, int chunkX, int chunkZ)
    {
        if (player is not global::Orion.Player.Player host || host.Session is null)
        {
            return;
        }

        dimension.GetSpatialIndex().SetPlayerChunk(host.Session, new ChunkCoord(chunkX, chunkZ));
    }

    public void RemovePlayerChunkIndex(IPlayer player)
    {
        if (player is not global::Orion.Player.Player host || host.Session is null)
        {
            return;
        }

        dimension.GetSpatialIndex().RemovePlayer(host.Session);
    }

    public void UpdateSimulationChunksFor(IPlayer player, int centerChunkX, int centerChunkZ, int simulationDistance)
    {
        _ = player;
        int distance = Math.Clamp(simulationDistance, 0, 120);
        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dz = -distance; dz <= distance; dz++)
            {
                ChunkColumn? chunk = dimension.GetChunk(centerChunkX + dx, centerChunkZ + dz);
                if (chunk is not null)
                {
                    chunk.Simulated = true;
                }
            }
        }
    }

    public void SyncPlayerViewHalo(IPlayer player, int viewDistanceChunks, int simulationDistanceChunks)
    {
        if (player is not global::Orion.Player.Player host || dimension.World?.Server is not Server server)
        {
            return;
        }

        AreaPlayerPresence.SyncViewHalo(server, dimension, host, viewDistanceChunks, simulationDistanceChunks);
    }

    public void NotifyChunkPresented(IPlayer player, int chunkX, int chunkZ)
    {
        if (player is not global::Orion.Player.Player host)
        {
            return;
        }

        ChunkColumn? chunk = dimension.GetChunk(chunkX, chunkZ);
        if (chunk is null)
        {
            return;
        }

        foreach (BlockLevelStorage storage in chunk.GetAllBlockStorages())
        {
            BlockPos position = storage.GetPosition();
            var block = dimension.GetBlock(position.X, position.Y, position.Z);
            block?.OnRender(host, position.X, position.Y, position.Z);
        }
    }

    public bool IsEntityTransferInFlight(ulong runtimeId) =>
        CrossAreaTransferHandler.IsTransferInFlight(runtimeId);
}
