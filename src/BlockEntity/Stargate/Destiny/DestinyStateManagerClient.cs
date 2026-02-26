using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content;

public class DestinyStateManagerClient : StargateStateManagerClient
{
    public DestinyStateManagerClient() : base()
    {
    }

    protected override void ContinueToNextIndex()
    {
    }

    protected override void OnGlyphDown(float _)
    {
        UnregisterTickListener();
        AwaitingChevronAnimation = false;

        Gate.VisualManager.DeactivateLockChevron();
    }

    protected override void OnTick(float delta)
    {
        switch (State)
        {
            case EnumStargateState.DialingOutgoing:
                TickDialingOutgoing(delta);
                break;
            case EnumStargateState.ConnectedOutgoing:
                TickConnectedOutgoing(delta);
                break;
            default:
                UnregisterTickListener();
                break;
        }
    }
}
