using AstriaPorta.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace AstriaPorta.Content;

public class BlockEntityStargateDestiny : StargateBase
{
    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api is ICoreClientAPI capi)
        {
            SoundManager = new StargateSoundManagerClient(capi, EnumStargateType.Destiny, Pos);
            VisualManager = new DestinyVisualManager(this);
            StateManager ??= new DestinyStateManagerClient();

            VisualManager.Initialize();
        }
        else
        {
            SoundManager = new StargateSoundManagerServer(api as ICoreServerAPI, EnumStargateType.Destiny, Pos);
            StateManager ??= new DestinyStateManagerServer();
        }

        StateManager.Initialize(this);

        StateManager.RotationDegPerSecond = StargateConfig.Loaded.DialSpeedDegreesPerSecondDestiny;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        if (worldAccessForResolve is IClientWorldAccessor)
        {
            StateManager ??= new DestinyStateManagerClient();
        }
        else
        {
            StateManager ??= new DestinyStateManagerServer();
        }

        base.FromTreeAttributes(tree, worldAccessForResolve);
    }
}
