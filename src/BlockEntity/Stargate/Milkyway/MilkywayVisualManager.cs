using AstriaPorta.Content;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.src.BlockEntity.Stargate.Milkyway;

public class MilkywayVisualManager : StargateVisualManager
{
    public MilkywayVisualManager(StargateBase gate) : base(gate)
    {
    }

    /// <summary>
    /// Initializes and registers the main gate renderer. Should also call the
    /// event horizon renderer initializer
    /// </summary>
    public override void Initialize()
    {
        InitializeGateRenderer();

        EventHorizonLight = new LightiningPointLight(new Vec3f(0.9f, 0.5f, 0.5f), Gate.Pos.AddCopy(0, 3, 0).ToVec3d());

        AnimUtil.InitializeAnimator("milkyway_chevron_animation", null, null, new Vec3f(0, Gate.Block.Shape.rotateY, 0));

        UpdateRendererState(Gate.StateManager.CurrentAngle);
        UpdateChevronGlow(Gate.StateManager.ActiveChevrons);

        InitializeHorizonRenderer();
    }

    protected override void InitializeGateRenderer()
    {
        if (GateRendererInitialized && GateRenderer != null) return;

        GateRenderer = new MilkywayGateRenderer(Capi, Gate.Pos);
        GateRenderer.orientation = Gate.Block.Shape.rotateY;
        Capi.Event.RegisterRenderer(GateRenderer, EnumRenderStage.Opaque);

        GateRendererInitialized = true;
        GateRendererRegistered = true;
    }
}