using AstriaPorta.Config;
using AstriaPorta.Systems;
using AstriaPorta.Util;

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

    public override bool TryDial(IStargateAddress address, EnumDialSpeed speed)
    {
        GateLogger.LogAudit(LogLevel.Info, $"Started dial to {address} with coordinates ({address.AddressCoordinates.X},{address.AddressCoordinates.Y},{address.AddressCoordinates.Z})");

        if (State != EnumStargateState.Idle)
        {
            return false;
        }

        if (speed == EnumDialSpeed.Default)
        {
            speed = (UseQuickDial && StargateConfig.Loaded.AllowQuickDial) ? EnumDialSpeed.Fast : EnumDialSpeed.Slow;
        }
        CurrentDialSpeed = speed;

        Gate.ReleaseRemoteGate();
        DialingAddress = address;

        if (Gate.WillDialSucceed(address))
        {
            StargateManagerSystem.GetInstance(Api).LoadRemoteGate(address, Gate);
            if (!IsForceLoaded)
            {
                StargateManagerSystem.GetInstance(Api).ForceLoadChunk(Gate.Pos);
                IsForceLoaded = true;
            }
        }

        ConfigureDialingSettings();
        UnregisterTickListener();

        TryRegisterDelayedCallback((_) =>
        {
            TryRegisterTickListener(OnTick, 20);
        }, 1000);

        SyncStateToClients();
        return true;
    }
}
