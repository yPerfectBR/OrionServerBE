using Orion.Api.Blocks;
using Orion.Api.Events;
using Orion.Api.Math;
using Orion.Api.Network;

namespace Orion.Api;

public interface IServer
{
    IReadOnlyCollection<IPlayer> OnlinePlayers { get; }
    IWorld? DefaultWorld { get; }
    IWorld? GetWorld(string name);
    IReadOnlyCollection<IWorld> Worlds { get; }

    /// <summary>Dispatches a signal to all subscribed plugin handlers (same bus as host Emit).</summary>
    void Emit(ISignal signal);

    double Tps { get; }
    int SessionCount { get; }
    int SimulationDistance { get; }
}

public interface IWorld
{
    string Name { get; }
    IDimension? GetDimension(string name);
    IReadOnlyCollection<IDimension> Dimensions { get; }
    double TickWork { get; }
    int? AttachedWorkerId { get; }
    IServer? Server { get; }
}

public interface IDimension
{
    string Name { get; }
    IWorld World { get; }
    ulong CurrentTick { get; }

    /// <summary>Bedrock difficulty ordinal: 0 Peaceful, 1 Easy, 2 Normal, 3 Hard.</summary>
    int Difficulty { get; }

    /// <summary>Gamerule: whether entities in this dimension take drowning/suffocation damage.</summary>
    bool DrowningDamage { get; }

    int ChunkCount { get; }

    /// <summary>Bedrock dimension network id for LevelChunkPacket.</summary>
    int DimensionNetworkId { get; }

    bool IsSessionThreadingEnabled { get; }

    IBlock? GetBlock(int x, int y, int z, int layer = 0);
    void SetBlock(int x, int y, int z, IBlock block, int layer = 0, bool dirty = true);
    IBlockPermutation GetPermutation(int x, int y, int z, int layer = 0);
    void SetPermutation(int x, int y, int z, IBlockPermutation permutation, int layer = 0, bool dirty = true);
    IEntity SpawnEntity(string typeIdentifier, Vec3f position, EntitySpawnOptions? options = null);
    IReadOnlyCollection<IEntity> GetEntities();
    void Broadcast(IOutboundPacket packet, PacketBroadcastOptions? options = null);

    /// <param name="onReady">Chunk X, Z, serialized payload, SubChunkCount for LevelChunk.</param>
    void RequestChunkPayloads(ReadOnlySpan<(int X, int Z)> chunks, Action<int, int, byte[], uint> onReady);
    void AddChunkViewer(int x, int z);
    bool RemoveChunkViewer(int x, int z);
    bool HasChunkViewers(int x, int z);
    bool UnloadChunk(int x, int z);
    void SetPlayerChunkIndex(IPlayer player, int chunkX, int chunkZ);
    void RemovePlayerChunkIndex(IPlayer player);
    void UpdateSimulationChunksFor(IPlayer player, int centerChunkX, int centerChunkZ, int simulationDistance);
    void SyncPlayerViewHalo(IPlayer player, int viewDistanceChunks, int simulationDistanceChunks);
    void NotifyChunkPresented(IPlayer player, int chunkX, int chunkZ);
    bool IsEntityTransferInFlight(ulong runtimeId);
}
