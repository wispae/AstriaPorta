using AstriaPorta.Util;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content;

public class PegasusGateRenderer : GateRenderer
{
    private const int GLYPH_COUNT = 9;

    private ICoreClientAPI _api;
    private IRenderAPI _rapi;
    private BlockPos _pos;
    private Vec3d _camPos;

    private MultiTextureMeshRef _chevronMeshRef;
    private MultiTextureMeshRef[] _glyphMeshRefs;

    public Matrixf ModelMat = new Matrixf();

    private LoadedTexture _tex;

    public bool[] VisibleGlyphs = new bool[GLYPH_COUNT];
    public int[] VisibleGlyphIndices =
    [
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0
    ];

    public bool DialingGlyphVisible = false;
    public int DialingGlyphIndex = 0;
    private MultiTextureMeshRef _dialingGlyphMeshRef;

    public PegasusGateRenderer(ICoreClientAPI api, BlockPos pos)
    {
        _api = api;
        _pos = pos;
        _tex = new(_api);
        _rapi = _api.Render;
        _camPos = _api.World.Player.Entity.CameraPos;

        _chevronMeshRef = api.Render.UploadMultiTextureMesh(StargateMeshHelper.GenChevronMesh(_api, "pegasus"));
        _glyphMeshRefs = new MultiTextureMeshRef[GLYPH_COUNT];

        glyphAngle = 360f / glyphCount;
        glowColor = new Vec4f(0, 128, 255, 0);

        UpdateVisibleGlyphs();
    }

    /// <summary>
    /// Call after updating the active glyph indices<br/>
    /// Will automatically disable visibility of invalid (-1) indices
    /// </summary>
    public void UpdateVisibleGlyphs()
    {
        for (int i = 0; i < VisibleGlyphIndices.Length; i++)
        {
            if (VisibleGlyphIndices[i] == -1)
            {
                VisibleGlyphs[i] = false;
            }
            else
            {
                _glyphMeshRefs[i] = StargateMeshHelper.GetPegasusGlyphMeshRef(_api, VisibleGlyphIndices[i]);
            }
        }

        _dialingGlyphMeshRef = StargateMeshHelper.GetPegasusGlyphMeshRef(_api, DialingGlyphIndex);
    }

    public override void OnRenderFrame(float delta, EnumRenderStage stage)
    {
        _rapi.GlDisableCullFace();
        _rapi.GlToggleBlend(true);

        IStandardShaderProgram prog = _rapi.PreparedStandardShader(_pos.X, _pos.Y, _pos.Z);
        prog.Use();

        prog.ViewMatrix = _rapi.CameraMatrixOriginf;
        prog.ProjectionMatrix = _rapi.CurrentProjectionMatrix;
        prog.RgbaGlowIn = glowColor;
        prog.ExtraGlow = 64;

        // Chevrons
        for (var i = 1; i <= 9; i++)
        {
            prog.ModelMatrix = ModelMat.Identity()
                    .Translate(_pos.X - _camPos.X, _pos.Y - _camPos.Y, _pos.Z - _camPos.Z)
                    .Translate(.5f, 3.325f, .5f)
                    .RotateYDeg(orientation)
                    .Translate(0f, 0f, -.5f)
                    .RotateZDeg((i - 1) * 40f)
                    .Translate(-.5f, -.5f, 0f)
                    .Values;

            prog.ExtraGlow = chevronGlow[9 - i];
            _rapi.RenderMultiTextureMesh(_chevronMeshRef, "tex");
        }

        // glyphs
        prog.ExtraGlow = 96;
        for (var i = 0; i < GLYPH_COUNT; i++)
        {
            if (VisibleGlyphs[i])
            {
                prog.ModelMatrix = ModelMat.Identity()
                    .Translate(_pos.X - _camPos.X, _pos.Y - _camPos.Y, _pos.Z - _camPos.Z)
                    .Translate(.5f, 3.325f, .5f)
                    .RotateYDeg(orientation)
                    .Translate(0f, 0f, -.5f)
                    .RotateZDeg((i + 1) * -40f)
                    .Translate(-.5f, -.5f, 0f)
                    .Values;

                _rapi.RenderMultiTextureMesh(_glyphMeshRefs[i], "tex");
            }
        }

        if (DialingGlyphVisible)
        {
            prog.ModelMatrix = ModelMat.Identity()
                .Translate(_pos.X - _camPos.X, _pos.Y - _camPos.Y, _pos.Z - _camPos.Z)
                .Translate(.5f, 3.325f, .5f)
                .RotateYDeg(orientation)
                .Translate(0f, 0f, -.5f)
                .RotateZDeg(-ringRotation)
                .Translate(-.5f, -.5f, 0f)
                .Values;

            _rapi.RenderMultiTextureMesh(_dialingGlyphMeshRef, "tex");
        }

        prog.ExtraGlow = 0;
        prog.Stop();
    }

    public override void Dispose()
    {
        _api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

        _chevronMeshRef.Dispose();
    }
}
