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

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeGateRenderer();

        EventHorizonLight = new LightiningPointLight(new Vec3f(0.8f, 0.8f, 0.8f), Gate.Pos.AddCopy(0, 3, 0).ToVec3d());

        UpdateRendererState(Gate.StateManager.CurrentAngle);
        UpdateChevronGlow(Gate.StateManager.ActiveChevrons, false);

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

    protected override void InitializeHorizonRenderer()
    {
        if (HorizonRendererInitialized) return;

        TextureAtlasPosition texPos = Capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonTexPos;

        var horizonColor = new Vec4f(.6f, .6f, .6f, .8f);
        EventHorizonRenderer = new EventHorizonRenderer(Capi, Gate.Pos, texPos, false, horizonColor);
        EventHorizonRenderer.shouldRender = false;
        EventHorizonRenderer.Orientation = Gate.Block.Shape.rotateY;

        HorizonRendererInitialized = true;

        if (Gate.State == EnumStargateState.ConnectedIncoming || Gate.State == EnumStargateState.ConnectedOutgoing)
        {
            ActivateHorizon(false);
        }
    }

    protected override void UpdateChevronGlow(int activeChevrons, EnumAddressLength length, bool activeDialingState = false)
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

        _destinyRenderer.IsDialing = activeDialingState;
        _destinyRenderer.MeshDirty = true;
    }

    public override void StartDialing()
    {
    }
}
