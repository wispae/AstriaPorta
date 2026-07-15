using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

#nullable enable

namespace AstriaPorta.Util;

public class DynamicTextureSource : ITexPositionSource
{
    public DynamicTextureSource(ICoreClientAPI capi)
    {
        _capi = capi;
        _guiTexturePosition = new()
        {
            atlasNumber = 0,
            atlasTextureId = capi.ItemTextureAtlas.UnknownTexturePosition.atlasTextureId,
            x1 = 0,
            y1 = 0,
            x2 = 1,
            y2 = 1
        };
    }

    private ICoreClientAPI _capi;
    private TextureAtlasPosition _guiTexturePosition;
    private Size2i _textureSize = new();

    public TextureAtlasPosition? this[string textureCode]
    {
        get
        {
            return _guiTexturePosition;
        }
    }

    public Size2i? AtlasSize
    {
        get
        {
            return _textureSize;
        }
    }

    public void UpdateTexture(LoadedTexture? texture)
    {
        if (texture == null)
        {
            var unknownPosition = _capi.ItemTextureAtlas.UnknownTexturePosition;
            _guiTexturePosition.atlasTextureId = unknownPosition.atlasTextureId;
            _guiTexturePosition.x1 = unknownPosition.x1;
            _guiTexturePosition.y1 = unknownPosition.y1;
            _guiTexturePosition.x2 = unknownPosition.x2;
            _guiTexturePosition.y2 = unknownPosition.y2;
            return;
        }

        _textureSize.Width = texture.Width;
        _textureSize.Height = texture.Height;
        _guiTexturePosition.atlasTextureId = texture.TextureId;
        _guiTexturePosition.x1 = 0;
        _guiTexturePosition.y1 = 0;
        _guiTexturePosition.x2 = 1;
        _guiTexturePosition.y2 = 1;
    }
}
