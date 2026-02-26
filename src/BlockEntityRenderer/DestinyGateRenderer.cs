using AstriaPorta.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content;

public class DestinyGateRenderer : GateRenderer
{
    private const int GLYPH_COUNT = 9;

    private ICoreClientAPI _api;
    private BlockPos _pos;

    MultiTextureMeshRef glyphMeshRef;
    MultiTextureMeshRef ringMeshRef;
    public Matrixf ModelMat = new Matrixf();

    public int[] ActiveGlyphIndices =
    [
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1
    ];
    public bool IsDialing = false;
    public bool MeshDirty = false;

    public DestinyGateRenderer(ICoreClientAPI api, BlockPos pos)
    {
        _api = api;
        _pos = pos;

        // chevronMeshRef = api.Render.UploadMultiTextureMesh(StargateMeshHelper.GenChevronMesh(api, "destiny"));
        // ringMeshRef = api.Render.UploadMultiTextureMesh(StargateMeshHelper.GenRingMesh(api, "destiny"));

        MeshDirty = true;
        UpdateGateMesh();

        glyphAngle = 360f / glyphCount;
        glowColor = new Vec4f(200, 200, 200, 128);
    }

    public void UpdateGateMesh()
    {
        if (!MeshDirty) return;

        ringMeshRef = StargateMeshHelper.GetDestinyGateMeshRef(_api, IsDialing, false);
        var glyphMeshData = StargateMeshHelper.GenDestinyGlyphMesh(_api, ActiveGlyphIndices);

        if (glyphMeshRef != null && !glyphMeshRef.Disposed && glyphMeshRef.meshrefs != null && glyphMeshRef.meshrefs.Length > 0 && !glyphMeshRef.meshrefs[0].Disposed)
        {
            // only contains 1 mesh
            _api.Render.UpdateMesh(glyphMeshRef.meshrefs[0], glyphMeshData);
        }
        else
        {
            glyphMeshRef?.Dispose();
            glyphMeshRef = _api.Render.UploadMultiTextureMesh(glyphMeshData);
        }

        MeshDirty = false;
    }

    public override void OnRenderFrame(float delta, EnumRenderStage stage)
    {
        if (ringMeshRef == null || glyphMeshRef == null) return;
        if (MeshDirty) UpdateGateMesh();

        IRenderAPI rpi = _api.Render;
        Vec3d camPos = _api.World.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        rpi.GlToggleBlend(true);

        IStandardShaderProgram prog = rpi.PreparedStandardShader(_pos.X, _pos.Y, _pos.Z);
        prog.Use();

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        // prog.RgbaGlowIn = glowColor;

        prog.ModelMatrix = ModelMat.Identity()
            .Translate(_pos.X - camPos.X, _pos.Y - camPos.Y, _pos.Z - camPos.Z)
            .Translate(.5f, 3.325f, .5f)
            .RotateYDeg(orientation)
            .Translate(0f, 0f, -.5f)
            .RotateZDeg(ringRotation)
            .Translate(-.5f, -.5f, 0f)
            .Values;
        // rpi.RenderMultiTextureMesh(ringMeshRef, "tex");

        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        rpi.RenderMultiTextureMesh(ringMeshRef, "tex");
        rpi.RenderMultiTextureMesh(glyphMeshRef, "tex");
        prog.Stop();
    }

    public override void Dispose()
    {
        _api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

        // DO NOT dispose of ring mesh here, as it's
        // managed by the StargateMeshHelper
        // ringMeshRef.Dispose();
        glyphMeshRef.Dispose();
    }
}
