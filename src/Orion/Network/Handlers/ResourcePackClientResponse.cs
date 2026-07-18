namespace Orion.Network.Handlers;

using Orion;
using Orion.Entity;
using Orion.Item;
using Orion.Protocol.Io;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.RakNet;
using Orion.Player;
using Orion.Scheduling;
using Orion.Config;
using Dimension = Orion.World.Dimension;
using Orion.World;
using Log = Orion.Logger.Logger;


public static class ResourcePackClientResponse
{
    private static bool _loggedCatalogInit;

    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ResourcePackClientResponsePacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ResourcePackClientResponsePacket)Protocol.Io.Packet.Deserialize(reader);

        switch (packet.Response)
        {
            case ResourcePackResponse.Refused:
                DisconnectPacket disconnect = new()
                {
                    Reason = DisconnectReason.ResourcePackProblem,
                    HideDisconnectionScreen = false,
                    Message = "Required resource packs were refused.",
                    FilteredMessage = "Required resource packs were refused."
                };
                server.Network.SendPacket(connection, disconnect);
                return;

            case ResourcePackResponse.SendPacks:
                Console.WriteLine($"Client requested packs ({packet.PacksToDownload.Count}). Pack transfer is not implemented yet.");
                return;

            case ResourcePackResponse.AllPacksDownloaded:
                ResourcePackStackPacket stack = new()
                {
                    MustAccept = false,
                    Packs =
                    [
                        new ResourcePackStackEntry
                        {
                            Uuid = Guid.Parse("0fba4063-dba1-4281-9b89-ff9390653530"),
                            Version = "1.0.0",
                            SubPackName = string.Empty
                        }
                    ],
                    BaseGameVersion = Constants.MinecraftVersion,
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    IncludeEditorPacks = true
                };
                server.Network.SendPacket(connection, stack);
                return;

            case ResourcePackResponse.Completed:
                if (!SessionLookup.TryGetSession(server, connection, out PlayerSession? session)
                    || session.ActiveEntity is not global::Orion.Player.Player player)
                {
                    Console.WriteLine("Resource pack flow completed, but no player session was found.");
                    DisconnectPacket missingSessionDisconnect = new()
                    {
                        Reason = DisconnectReason.Disconnected,
                        HideDisconnectionScreen = false,
                        Message = "Server force closed the connection.",
                        FilteredMessage = "Server force closed the connection."
                    };
                    server.Network.SendPacket(connection, missingSessionDisconnect);
                    connection.Disconnect();
                    return;
                }

                PlayerListPacket playerList = new()
                {
                    ActionType = PlayerListActionType.Add,
                    Entries = server.Sessions.Values
                        .Select(static session => session.ActiveEntity)
                        .OfType<global::Orion.Player.Player>()
                        .Select(static online => online.CreatePlayerListEntry())
                        .ToList()
                };
                server.Network.SendPacket(connection, playerList);
                server.Broadcast(new PlayerListPacket
                {
                    ActionType = PlayerListActionType.Add,
                    Entries = [player.CreatePlayerListEntry()]
                }, player);

                GamerulesConfig gamerules = player.Dimension?.World?.Gamerules
                    ?? OrionInfo.WorldDefaultSettings.Gamerules;

                StartGamePacket startGame = new()
                {
                    EntityUniqueId = player.UniqueId,
                    EntityRuntimeId = player.RuntimeId,
                    PlayerGameMode = (int)player.GetGamemode(),
                    PlayerPosition = new Vec3f { X = 0f, Y = -57f, Z = 0f },
                    Pitch = 0f,
                    Yaw = 0f,
                    WorldSeed = 0,
                    SpawnBiomeType = SpawnBiomeType.Default,
                    UserDefinedBiomeName = "plains",
                    Dimension = 0,
                    Generator = 1,
                    WorldGameMode = 0,
                    Hardcore = false,
                    Difficulty = 1,
                    WorldSpawn = new BlockPos { X = 0, Y = -58, Z = 0 },
                    AchievementsDisabled = false,
                    EditorWorldType = EditorWorldType.NotEditor,
                    CreatedInEditor = false,
                    ExportedFromEditor = false,
                    DayCycleLockTime = gamerules.DoDayLightCycle ? 0 : 6000,
                    EducationEditionOffer = 0,
                    EducationFeaturesEnabled = false,
                    EducationProductId = string.Empty,
                    RainLevel = 0f,
                    LightningLevel = 0f,
                    ConfirmedPlatformLockedContent = false,
                    MultiPlayerGame = true,
                    LanBroadcastEnabled = false,
                    XblBroadcastMode = XblBroadcastMode.Public,
                    PlatformBroadcastMode = (int)XblBroadcastMode.Public,
                    CommandsEnabled = true,
                    TexturePackRequired = false,
                    GameRules = GameRulesFactory.CreateNetworkRules(gamerules),
                    Experiments = [],
                    ExperimentsPreviouslyToggled = false,
                    BonusChestEnabled = false,
                    StartWithMapEnabled = false,
                    PlayerPermissions = player.IsOperator ? 2 : 1,
                    ServerChunkTickRadius = 4,
                    HasLockedBehaviourPack = false,
                    HasLockedTexturePack = false,
                    FromLockedWorldTemplate = false,
                    MsaGamerTagsOnly = false,
                    FromWorldTemplate = false,
                    WorldTemplateSettingsLocked = false,
                    OnlySpawnV1Villagers = false,
                    PersonaDisabled = false,
                    CustomSkinsDisabled = false,
                    EmoteChatMuted = false,
                    BaseGameVersion = Constants.MinecraftVersion,
                    LimitedWorldWidth = 0,
                    LimitedWorldDepth = 0,
                    NewNether = true,
                    EducationSharedResourceUri = new EducationSharedResourceUri
                    {
                        ButtonName = string.Empty,
                        LinkUri = string.Empty
                    },
                    ForceExperimentalGameplay = new Optional<BoolType> { HasValue = false },
                    ChatRestrictionLevel = ChatRestrictionLevel.None,
                    DisablePlayerInteractions = false,
                    LevelId = "BasaltWorld",
                    WorldName = "Basalt",
                    TemplateContentIdentity = string.Empty,
                    Trial = false,
                    PlayerMovementSettings = new PlayerMovementSettings
                    {
                        // Client needs rewind history for CorrectPlayerMovePrediction / teleport ticks.
                        RewindHistorySize = 100,
                        ServerAuthoritativeBlockBreaking = true
                    },
                    Time = 0,
                    EnchantmentSeed = 0,
                    Blocks = [],
                    MultiPlayerCorrelationId = Guid.NewGuid().ToString(),
                    ServerAuthoritativeInventory = true,
                    GameVersion = Constants.MinecraftVersion,
                    PropertyData = new Orion.Protocol.Nbt.CompoundTag(),
                    ServerBlockStateChecksum = 0,
                    WorldTemplateId = Guid.Empty,
                    ClientSideGeneration = false,
                    UseBlockNetworkIdHashes = true,
                    ServerAuthoritativeSound = true,
                    ServerJoinInformation = new OptionalValue<ServerJoinInformation> { HasValue = false },
                    ServerId = string.Empty,
                    ScenarioId = string.Empty,
                    WorldId = string.Empty,
                    OwnerId = player.Xuid
                };
                player.Position = startGame.PlayerPosition;
                Dimension? dimension = server.GetWorld().GetDimension(DimensionType.Overworld);
                if (dimension is not null)
                {
                    if (!AreaPlayerSpawnPipeline.TryCompleteSpawn(server, player, dimension, out _))
                    {
                        DisconnectPacket forcedDisconnect = new()
                        {
                            Reason = DisconnectReason.Disconnected,
                            HideDisconnectionScreen = false,
                            Message = "Server force closed the connection.",
                            FilteredMessage = "Server force closed the connection."
                        };
                        server.Network.SendPacket(connection, forcedDisconnect);
                        connection.Disconnect();
                        return;
                    }
                }

                byte[] itemRegistryPayload = Orion.Protocol.Registry.CuratedItemCatalog.GetItemRegistryPayload();
                byte[] creativeContentPayload = Orion.Protocol.Registry.CuratedItemCatalog.GetCreativeContentPayload();

                if (!_loggedCatalogInit)
                {
                    _loggedCatalogInit = true;
                    CreativeInventoryLog.LogCatalogInit();
                    CreativeInventoryLog.LogRegistryCreativeItems();
                }

                AvailableActorIdentifiersPacket actorIdentifiers = new()
                {
                    Data = EntityRegistry.BuildAvailableActorIdentifiersTag()
                };

                PlayStatusPacket spawnStatus = new(PlayStatus.PlayerSpawn);

                void SendSpawnPackets()
                {
                    SessionSendCoordinator.SendDirect(session, startGame);
                    player.SyncPermissions();
                    server.Network.SendSerializedPacket(connection, PacketId.ItemRegistry, itemRegistryPayload);
                    server.Network.SendPackets(connection, [actorIdentifiers, spawnStatus]);
                    server.Network.SendSerializedPacket(connection, PacketId.CreativeContent, creativeContentPayload);

                    CreativeInventoryLog.LogItemRegistrySent("spawn", player.Username, itemRegistryPayload);
                    CreativeInventoryLog.LogCreativeContentSent("spawn", player.Username, creativeContentPayload);
                    CreativeInventoryLog.LogSpawnSequence(
                        player.Username,
                        player.GetGamemode(),
                        startGame.PlayerGameMode,
                        itemRegistryPayload.Length,
                        creativeContentPayload.Length);
                }

                if (server.ConnectionCoordinator is Scheduling.ConnectionCoordinator coordinator && coordinator.IsActive)
                {
                    coordinator.RunOnSessionThread(session, SendSpawnPackets);
                }
                else
                {
                    SendSpawnPackets();
                }

                return;

            default:
                Console.WriteLine($"Unknown resource pack response: {(byte)packet.Response}");
                return;
        }
    }

}










