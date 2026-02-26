using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstriaPorta.Content;

public class DestinyStateManagerServer : StargateStateManagerServer
{
    public DestinyStateManagerServer() : base()
    {
    }

    protected override void ContinueToNextIndex()
    {
        CurrentAddressIndex++;
        RotateCW = (CurrentAddressIndex == 0) ? true : !RotateCW;
        NextGlyph = DialingAddress.AddressCoordinates.Glyphs[CurrentAddressIndex];

        SyncStateToClients();

        TryRegisterTickListener(OnTick, 20);
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

        Gate.ReleaseRemoteGate();
    }
}
