namespace Orion.Player.Traits;

using System.Text;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.Traits;

using Entity = Orion.Entity.Entity;
using Orion.Entity.Traits.Types;
using Orion.Entity.Traits;

public sealed class DebugTrait : PlayerTrait
{
    private const double TargetTps = 20.0;
    private const ulong SendIntervalTicks = 20;

    public new static string Identifier => "debug";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private ulong _lastSentTick;
    private double _averageMspt;

    public DebugTrait(Entity entity) : base(entity)
    {
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        _lastSentTick = Player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;
        _averageMspt = 0;
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Player.IsAlive || details.CurrentTick - _lastSentTick < SendIntervalTicks)
        {
            return;
        }

        try
        {
            global::Orion.Server? server = Player.Dimension?.World?.Server as global::Orion.Server;
            double tps = server?.Tps ?? TargetTps;
            double mspt = Player.Dimension?.World is Tickable tickable ? tickable.TickWork : 0;
            _averageMspt = _averageMspt == 0 ? mspt : _averageMspt + ((mspt - _averageMspt) * 0.2);
            double workingSetMb = Environment.WorkingSet / (1024.0 * 1024.0);
            int chunksLoaded = Player.Dimension?.ChunkCount ?? 0;

            StringBuilder builder = new();
            builder.AppendLine($"TPS {tps:0.0}/{TargetTps:0.0}");
            builder.AppendLine($"MSPT {mspt:0.00} avg {_averageMspt:0.00}");
            builder.AppendLine($"RAM {workingSetMb:0.0}MB chunks {chunksLoaded}");

            if (Player.Dimension?.World?.AttachedWorkerId is int worldWorkerId)
            {
                builder.AppendLine($"sim ww{worldWorkerId}");
            }

            TextPacket packet = new()
            {
                NeedsTranslation = false,
                VariantType = TextVariantType.MessageOnly,
                Variant = new TextVariant
                {
                    Type = TextType.Tip,
                    Message = builder.ToString().TrimEnd('\n', '\r')
                },
                Xuid = string.Empty,
                PlatformChatId = string.Empty,
                FilteredMessage = null
            };

            Player.Send(packet);
            _lastSentTick = details.CurrentTick;
        }
        catch (Exception exception)
        {
            Warn($"[{Player.Username}] DebugTrait exception: {exception}");
        }
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new DebugTrait(entity);
    }
}
