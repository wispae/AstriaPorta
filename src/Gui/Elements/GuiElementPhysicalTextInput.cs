using Cairo;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace AstriaPorta.Gui;

public class GuiElementPhysicalTextInput : GuiElementTextInput
{
    public GuiElementPhysicalTextInput(ICoreClientAPI capi, ElementBounds bounds, Action<string> onTextChanged, CairoFont font) : base(capi, bounds, onTextChanged, font)
    {
    }

    public override void ComposeTextElements(Context ctx, ImageSurface surface)
    {
        EmbossRoundRectangleElement(ctx, Bounds, true, 2, 1);
        ctx.SetSourceRGBA(0, 0, 0, 0.2);
        ElementRoundRectangle(ctx, Bounds, false, 1);
        ctx.Fill();


        ImageSurface surfaceHighlight = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
        Context ctxHighlight = genContext(surfaceHighlight);

        ctxHighlight.SetSourceRGBA(1, 1, 1, 0.1);
        ctxHighlight.Paint();

        if (!enabled) Font.Color[3] = 0.35f;

        generateTexture(surfaceHighlight, ref highlightTexture);

        ctxHighlight.Dispose();
        surfaceHighlight.Dispose();

        highlightBounds = Bounds.CopyOffsetedSibling().WithFixedPadding(0, 0).FixedGrow(2 * Bounds.absPaddingX, 2 * Bounds.absPaddingY);
        highlightBounds.CalcWorldBounds();

        CallRecomposeText(this);
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        var placeHolderTextTexture = GetPlaceholderTextTexture(this);
        var textTexture = GetTextTexture(this);

        var clientMain = api.World as ClientMain;

        if (HasFocus)
        {
            api.Render.GlToggleBlend(true);
            api.Render.Render2DTexture(highlightTexture.TextureId, highlightBounds);
        }
        else
        {
            if (placeHolderTextTexture != null && (text == null || text.Length == 0) && (lines == null || lines.Count == 0 || lines[0] == null || lines[0] == ""))
            {
                api.Render.GlToggleBlend(true);
                api.Render.Render2DTexturePremultipliedAlpha(
                    placeHolderTextTexture.TextureId,
                    (int)(highlightBounds.renderX + highlightBounds.absPaddingX + 3),
                    (int)(highlightBounds.renderY + highlightBounds.absPaddingY + (highlightBounds.OuterHeight - placeHolderTextTexture.Height) / 2),
                    placeHolderTextTexture.Width,
                    placeHolderTextTexture.Height
                );

            }
        }

        var rightSpacing = GetRightSpacing(this);
        var bottomSpacing = GetBottomSpacing(this);

        api.Render.GlScissor(
            (int)(Bounds.renderX),
            (int)(Bounds.renderY),
            Math.Max(0, Bounds.OuterWidthInt + 1 - (int)rightSpacing),
            Math.Max(0, Bounds.OuterHeightInt + 1 - (int)bottomSpacing)
        );

        var renderLeftOffset = GetRenderLeftOffset(this);
        var textSize = GetTextSize(this);

        api.Render.GlScissorFlag(true);
        RenderTextSelection();
        api.Render.Render2DTexturePremultipliedAlpha(textTexture.TextureId, Bounds.renderX - renderLeftOffset, Bounds.renderY, textSize.X, textSize.Y);
        api.Render.GlScissorFlag(false);

        base.RenderInteractiveElements(deltaTime);
    }


    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RecomposeText")]
    private extern static void CallRecomposeText(GuiElementEditableTextBase element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bottomSpacing")]
    private extern static ref double GetBottomSpacing(GuiElementEditableTextBase element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "placeHolderTextTexture")]
    private extern static ref LoadedTexture GetPlaceholderTextTexture(GuiElementTextInput element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "renderLeftOffset")]
    private extern static ref double GetRenderLeftOffset(GuiElementEditableTextBase element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rightSpacing")]
    private extern static ref double GetRightSpacing(GuiElementEditableTextBase element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "textSize")]
    private extern static ref Vec2i GetTextSize(GuiElementEditableTextBase element);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "textTexture")]
    private extern static ref LoadedTexture GetTextTexture(GuiElementEditableTextBase element);
}
