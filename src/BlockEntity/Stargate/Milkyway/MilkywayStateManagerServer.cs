namespace AstriaPorta.Content;

public class MilkywayStateManagerServer : StargateStateManagerServer
{
    public MilkywayStateManagerServer() : base()
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

        // We should preferably not keep a permanent reference to the remote gate
        // should be fine to keep it during a single tick, but best to yeet it here
        Gate.ReleaseRemoteGate();
    }
}
