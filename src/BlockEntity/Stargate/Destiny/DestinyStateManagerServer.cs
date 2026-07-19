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

    /// <inheritdoc/>
    protected override float NextAngle(float delta)
    {
        byte targetGlyph = DialingAddress.AddressCoordinates.Glyphs[CurrentAddressIndex];
        // We need to account for the 9 empty glyph locations in the destiny gate
        targetGlyph += (byte)(1 + (targetGlyph) / 4);
        if (targetGlyph < 0) targetGlyph += (byte)Gate.GlyphLength;
        if (targetGlyph >= Gate.GlyphLength) targetGlyph %= (byte)Gate.GlyphLength;
        PreviousAngle = CurrentAngle;

        if (CurrentGlyph != targetGlyph)
        {
            CurrentAngle += delta * RotationDegPerSecond * (RotateCW ? -1 : 1);
            if (CurrentAngle < 0) CurrentAngle += 360;
            else if (CurrentAngle >= 360) CurrentAngle %= 360;

            NextGlyph = (byte)(CurrentGlyph + (RotateCW ? -1 : 1));
            if (NextGlyph > 250) NextGlyph = (byte)(Gate.GlyphLength - 1);
            else if (NextGlyph >= Gate.GlyphLength) NextGlyph = 0;

            float nextAngle = (NextGlyph * GlyphAngle + 360f) % 360;
            float tPreviousAngle;
            float tCurrentAngle;
            float tNextAngle;

            if (RotateCW)
            {
                tPreviousAngle = 360f;
                tCurrentAngle = (CurrentAngle - PreviousAngle + 360) % 360;
                tNextAngle = (nextAngle - PreviousAngle + 360) % 360;
            }
            else
            {
                tPreviousAngle = 0f;
                tCurrentAngle = (CurrentAngle + (360 - PreviousAngle)) % 360;
                tNextAngle = (nextAngle + (360 - PreviousAngle)) % 360;
            }

            if ((tCurrentAngle >= tNextAngle && tNextAngle > tPreviousAngle) || (tCurrentAngle <= tNextAngle && tNextAngle < tPreviousAngle))
            {
                CurrentGlyph = NextGlyph;

                if (CurrentGlyph == targetGlyph)
                {
                    CurrentAngle = NextGlyph * GlyphAngle;
                    OnGlyphReached();
                }
            }
        }
        else
        {
            OnGlyphReached();
        }

        return CurrentAngle;
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
            SyncStateToClients();
        }, 1000);

        SyncStateToClients();
        return true;
    }
}
