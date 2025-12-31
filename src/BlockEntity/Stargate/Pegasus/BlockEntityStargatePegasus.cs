using AstriaPorta.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AstriaPorta.Content;

public class BlockEntityStargatePegasus : StargateBase
{
    public BlockEntityStargatePegasus() : base()
    {
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api is ICoreClientAPI capi)
        {
            SoundManager = new StargateSoundManager(capi, EnumStargateType.Pegasus, Pos);
            VisualManager = new PegasusVisualManager(this);
            StateManager ??= new PegasusStateManagerClient();

            VisualManager.Initialize();
        }
        else
        {
            StateManager ??= new PegasusStateManagerServer();
        }

        StateManager.Initialize(this);
        StateManager.RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondPegasus;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        if (worldAccessForResolve is IClientWorldAccessor)
        {
            StateManager ??= new PegasusStateManagerClient();
        }
        else
        {
            StateManager ??= new PegasusStateManagerServer();
        }

        base.FromTreeAttributes(tree, worldAccessForResolve);
    }
}
