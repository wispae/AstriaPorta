using System;
using Vintagestory.API.Client;
using System.Runtime.CompilerServices;

#nullable enable
namespace AstriaPorta.Gui;

public static class GuiComposerExtensions
{
    public static GuiComposer AddPhysicalTextInput(this GuiComposer self, ElementBounds bounds, Action<string> onTextChanged, CairoFont? font = null, string? key = null)
    {
        if (font == null)
        {
            font = CairoFont.TextInput();
        }

        if (!self.Composed)
        {
            self.AddInteractiveElement(new GuiElementPhysicalTextInput(self.Api, bounds, onTextChanged, font), key);
        }

        return self;
    }

    public static GuiElementPhysicalTextInput GetPhysicalTextInput(this GuiComposer self, string key)
    {
        return (GuiElementPhysicalTextInput)self.GetElement(key);
    }

    public static LoadedTexture GetStaticTexture(this GuiComposer self)
    {
        return GetStaticTextureAccessor(self);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "staticElementsTexture")]
    private extern static ref LoadedTexture GetStaticTextureAccessor(GuiComposer composer);
}
