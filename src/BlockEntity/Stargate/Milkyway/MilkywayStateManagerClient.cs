namespace AstriaPorta.Content;

public class MilkywayStateManagerClient : StargateStateManagerClient
{
    public MilkywayStateManagerClient() : base()
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
