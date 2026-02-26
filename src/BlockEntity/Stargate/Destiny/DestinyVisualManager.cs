using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AstriaPorta.Content;

public class DestinyVisualManager : StargateVisualManager
{
    private DestinyGateRenderer _destinyRenderer;

    public DestinyVisualManager(StargateBase gate) : base(gate)
    {
    }

    /// <summary>
    /// Initializes and registers the main gate renderer. Should also call the
    /// event horizon renderer initializer
    /// </summary>
    public override void Initialize()
    {
        InitializeGateRenderer();

        EventHorizonLight = new LightiningPointLight(new Vec3f(0.8f, 0.8f, 0.8f), Gate.Pos.AddCopy(0, 3, 0).ToVec3d());

        UpdateRendererState(Gate.StateManager.CurrentAngle);
        UpdateChevronGlow(Gate.StateManager.ActiveChevrons);

        InitializeHorizonRenderer();
    }

    protected override void InitializeGateRenderer()
    {
        if (GateRendererInitialized && GateRenderer != null) return;

        _destinyRenderer = new DestinyGateRenderer(Capi, Gate.Pos);
        _destinyRenderer.IsDialing = false;
        GateRenderer = _destinyRenderer;
        GateRenderer.orientation = Gate.Block.Shape.rotateY;
        Capi.Event.RegisterRenderer(GateRenderer, EnumRenderStage.Opaque);

        GateRendererInitialized = true;
        GateRendererRegistered = true;
    }

    protected override void UpdateChevronGlow(int activeChevrons, EnumAddressLength length)
    {
        if (_destinyRenderer == null) return;

        for (int i = 0; i < _destinyRenderer.ActiveGlyphIndices.Length; i++)
        {
            if (i < activeChevrons)
            {
                _destinyRenderer.ActiveGlyphIndices[i] = Gate.DialingAddress.AddressCoordinates.Glyphs[i];
            }
            else
            {
                _destinyRenderer.ActiveGlyphIndices[i] = -1;
            }
        }

        _destinyRenderer.IsDialing = Gate.State != EnumStargateState.Idle;
        _destinyRenderer.MeshDirty = true;
    }

    public override void StartDialing()
    {
    }
}
