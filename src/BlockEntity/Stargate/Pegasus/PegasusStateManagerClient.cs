using AstriaPorta.Util;

namespace AstriaPorta.Content;

public class PegasusStateManagerClient : StargateStateManagerClient
{
    private bool CurrentlyOnTargetGlyph = false;
    private bool EncounteredTargetGlyph = false;

    public PegasusStateManagerClient() : base()
    {
    }

    protected override void ContinueToNextIndex()
    {
        EncounteredTargetGlyph = false;
    }

    new protected float NextAngle(float delta)
    {
        int targetGlyph = 0;

        switch (DialingAddress.AddressLength)
        {
            case EnumAddressLength.Short:
                targetGlyph = (CurrentAddressIndex < 3) ? CurrentAddressIndex : CurrentAddressIndex + 2;
                break;
            case EnumAddressLength.Medium:
                targetGlyph = (CurrentAddressIndex < 4) ? CurrentAddressIndex : CurrentAddressIndex + 1;
                break;
            case EnumAddressLength.Long:
                targetGlyph = CurrentAddressIndex;
                break;
        }
        targetGlyph = (targetGlyph + 1) % 9;
        targetGlyph *= 4;

        PreviousAngle = CurrentAngle;

        if (true || CurrentGlyph != targetGlyph || CurrentlyOnTargetGlyph)
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

    protected override void OnGlyphDown(float _)
    {
        UnregisterTickListener();
        AwaitingChevronAnimation = false;

        if (CurrentDialSpeed == EnumDialSpeed.Slow)
        {
            Gate.VisualManager.UpdateChevronGlow(ActiveChevrons + 1);
        }
        Gate.VisualManager.DeactivateLockChevron();
    }

    protected override void OnTick(float delta)
    {
        CurrentlyOnTargetGlyph = CurrentGlyph == GetTrueGlyph(CurrentAddressIndex);
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

    new protected void OnGlyphReached()
    {
        if (CurrentAddressIndex > 0)
        {
            if (!EncounteredTargetGlyph && !RotateCW)
            {
                EncounteredTargetGlyph = true;
                return;
            }
            if (CurrentlyOnTargetGlyph) return;
        }

        UnregisterTickListener();
        if (State == EnumStargateState.Idle) return;

        AwaitingChevronAnimation = true;

        CallbackId = Gate.RegisterDelayedCallback(OnGlyphDown, 1000);

        Gate.SoundManager.PauseRotateSound();
        AwaitingChevronAnimation = true;

        Gate.SoundManager.Play(EnumGateSoundLocation.Lock);
        if (CurrentDialSpeed == EnumDialSpeed.Fast) return;

        Gate.VisualManager.ActivateLockChevron();
        Gate.VisualManager.UpdateChevronGlow(ActiveChevrons + 1);
        Gate.RegisterDelayedCallback((t) =>
        {
            Gate.SoundManager.Play(EnumGateSoundLocation.Release);
        }, 1500);

        ContinueToNextIndex();
    }

    private int GetTrueGlyph(int index)
    {
        int glyph = 0;

        switch (DialingAddress.AddressLength)
        {
            case EnumAddressLength.Short:
                glyph = (index < 3) ? index : index + 2;
                break;
            case EnumAddressLength.Medium:
                glyph = (index < 4) ? index : index + 1;
                break;
            case EnumAddressLength.Long:
                glyph = index;
                break;
        }

        glyph = (glyph + 1) % 9;
        glyph *= 4;

        return glyph;
    }

    protected override void TickDialingOutgoing(float delta)
    {
        if (CurrentDialSpeed == EnumDialSpeed.Slow)
        {
            Gate.SoundManager.StartRotateSound();
            NextAngle(delta);
        }
        else
        {
            OnGlyphReached();
        }
        Gate.VisualManager.UpdateRendererState(CurrentAngle);
    }

    protected override void TransitionToDialingOutgoing()
    {
        if (Gate.VisualManager is PegasusVisualManager pvm)
        {
            if (State != EnumStargateState.DialingOutgoing)
            {
                CurrentAngle = 0;
                CurrentGlyph = 0;
                EncounteredTargetGlyph = false;
                pvm.StartDialing();
            }

            pvm.ActivateDialingGlyph();
        }

        base.TransitionToDialingOutgoing();
    }
}
