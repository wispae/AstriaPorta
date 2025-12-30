using AstriaPorta.Util;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AstriaPorta.Content;

public class PegasusVisualManager : StargateVisualManager
{
    private PegasusGateRenderer _pegasusRenderer;

    public PegasusVisualManager(StargateBase gate) : base(gate)
    {
    }

    public override void Initialize()
    {
        InitializeGateRenderer();

        EventHorizonLight = new LightiningPointLight(new(0.9f, 0.5f, 0.5f), Gate.Pos.AddCopy(0, 3, 0).ToVec3d());

        UpdateRendererState(Gate.StateManager.CurrentAngle);
        UpdateChevronGlow(Gate.StateManager.ActiveChevrons);

        InitializeHorizonRenderer();
    }

    public override void ActivateLockChevron()
    {
        _pegasusRenderer.DialingGlyphVisible = false;
    }

    public void ActivateDialingGlyph()
    {
        _pegasusRenderer.DialingGlyphIndex = Gate.DialingAddress.AddressCoordinates.Glyphs[Gate.StateManager.CurrentAddressIndex];
        _pegasusRenderer.DialingGlyphVisible = true;
        _pegasusRenderer.UpdateVisibleGlyphs();
    }

    protected override void DisposeRenderers()
    {
        base.DisposeRenderers();

        _pegasusRenderer = null;
    }

    protected override void InitializeGateRenderer()
    {
        if (GateRendererInitialized && GateRenderer != null) return;

        _pegasusRenderer = new PegasusGateRenderer(Capi, Gate.Pos);
        GateRenderer = _pegasusRenderer;
        GateRenderer.orientation = Gate.Block.Shape.rotateY;
        Capi.Event.RegisterRenderer(GateRenderer, EnumRenderStage.Opaque);

        GateRendererInitialized = true;
    }

    protected override void UpdateChevronGlow(int activeChevrons, EnumAddressLength length)
    {
        if (_pegasusRenderer == null) return;

        if (activeChevrons == 0 || Gate.StateManager.CurrentDialSpeed == EnumDialSpeed.Fast)
        {
            _pegasusRenderer.DialingGlyphVisible = false;
        }

        byte padding = 0;

        for (int i = 1; i < 10; i++)
        {
            _pegasusRenderer.chevronGlow[i - 1] = 0;
            _pegasusRenderer.VisibleGlyphs[i - 1] = false;

            switch (length)
            {
                case EnumAddressLength.Short:
                    if (i == 4 || i == 5)
                    {
                        padding++;
                        continue;
                    }

                    if (i <= (activeChevrons + padding))
                    {
                        _pegasusRenderer.chevronGlow[i - 1] = 127;
                        _pegasusRenderer.VisibleGlyphs[i - 1] = true;
                        _pegasusRenderer.VisibleGlyphIndices[i - 1] = Gate.DialingAddress.AddressCoordinates.Glyphs[i - 1 - padding];
                    }

                    break;

                case EnumAddressLength.Medium:
                    if (i == 5)
                    {
                        padding++;
                        continue;
                    }

                    if (i <= (activeChevrons + padding))
                    {
                        _pegasusRenderer.chevronGlow[i - 1] = 127;
                        _pegasusRenderer.VisibleGlyphs[i - 1] = true;
                        _pegasusRenderer.VisibleGlyphIndices[i - 1] = Gate.DialingAddress.AddressCoordinates.Glyphs[i - 1 - padding];
                    }

                    break;

                case EnumAddressLength.Long:
                    if (i <= activeChevrons)
                    {
                        _pegasusRenderer.chevronGlow[i - 1] = 127;
                        _pegasusRenderer.VisibleGlyphs[i - 1] = true;
                        _pegasusRenderer.VisibleGlyphIndices[i - 1] = Gate.DialingAddress.AddressCoordinates.Glyphs[i - 1];
                    }

                    break;
            }
        }

        _pegasusRenderer.UpdateVisibleGlyphs();
    }

    public override void StartDialing()
    {
        for (int i = 0; i < _pegasusRenderer.VisibleGlyphIndices.Length; i++)
        {
            _pegasusRenderer.VisibleGlyphIndices[i] = Gate.DialingAddress.AddressCoordinates.Glyphs[i];
        }
        _pegasusRenderer.DialingGlyphIndex = 0;
        _pegasusRenderer.UpdateVisibleGlyphs();
    }

    public override void UpdateRendererState(float currentAngle)
    {
        if (GateRenderer == null) return;

        var visualRotation = currentAngle - (currentAngle % 10);
        GateRenderer.ringRotation = visualRotation;

        int glyphRemainder = (int)visualRotation % 40;
        int glyphPosition = (int)visualRotation / 40;
        _pegasusRenderer.DialingGlyphVisible = (glyphRemainder != 0 || !_pegasusRenderer.VisibleGlyphs[(glyphPosition + 8) % 9]) && Gate.State != EnumStargateState.Idle && Gate.StateManager.CurrentDialSpeed == EnumDialSpeed.Slow;
    }
}
