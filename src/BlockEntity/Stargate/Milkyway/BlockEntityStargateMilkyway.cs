using AstriaPorta.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AstriaPorta.Content;

public class BlockEntityStargateMilkyway : StargateBase
{
    public BlockEntityStargateMilkyway() : base()
    {
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api is ICoreClientAPI capi)
        {
            SoundManager = new StargateSoundManager(capi, EnumStargateType.Milkyway, Pos);
            VisualManager = new MilkywayVisualManager(this);
            StateManager ??= new MilkywayStateManagerClient();

            VisualManager.Initialize();
        }
        else
        {
            StateManager ??= new MilkywayStateManagerServer();
        }

        StateManager.Initialize(this);

        StateManager.RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        if (worldAccessForResolve is IClientWorldAccessor)
        {
            StateManager ??= new MilkywayStateManagerClient();
        }
        else
        {
            StateManager ??= new MilkywayStateManagerServer();
        }

        base.FromTreeAttributes(tree, worldAccessForResolve);
    }
}