using AstriaPorta.Config;
using AstriaPorta.Content;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace AstriaPorta.src.BlockEntity.Stargate.Milkyway;

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
            StateManager ??= new StargateStateManagerClient();

            VisualManager.Initialize();
        }
        else
        {
            StateManager ??= new StargateStateManagerServer();
        }

        StateManager.Initialize(this);

        StateManager.RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondMilkyway;
    }
}