using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AstriaPorta.Content
{
	public class MilkywayGateRenderer : GateRenderer
	{
		private ICoreClientAPI api;
		private BlockPos pos;

		MultiTextureMeshRef chevronMeshRef;
		MultiTextureMeshRef ringMeshRef;
		public Matrixf ModelMat = new Matrixf();

		private int ringTextureId;

		private int textureSubId;
		private TextureAtlasPosition texPos;
		private LoadedTexture tex;
		
		public MilkywayGateRenderer(ICoreClientAPI api, BlockPos pos)
		{
			this.api = api;
			this.pos = pos;

			tex = new LoadedTexture(api);

			chevronMeshRef = api.Render.UploadMultiTextureMesh(StargateMeshHelper.GenChevronMesh(api, "milkyway"));
			ringMeshRef = api.Render.UploadMultiTextureMesh(StargateMeshHelper.GenRingMesh(api, "milkyway"));

			glyphAngle = 360f / glyphCount;
			glowColor = new Vec4f(255, 102, 0, 0);

			AssetLocation loc = new AssetLocation("astriaporta", "block/gates/milkyway_sheet");
			api.Render.GetOrLoadTexture(loc, ref tex);
		}

		public MeshData ChevronMesh
		{
			set
			{
				chevronMeshRef = api.Render.UploadMultiTextureMesh(value);
			}
		}

		public override void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if (chevronMeshRef == null || ringMeshRef == null) return;

			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;

			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(true);

			IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			prog.Use();

			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			prog.RgbaGlowIn = glowColor;

			// TODO: refactor to use instanced rendering???
			for (int i = 1; i <= 9; i++)
			{
				prog.ModelMatrix = ModelMat.Identity()
				.Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
				// .Translate(.5f, 1.8125f, .5f)
				.Translate(.5f, 3.325f, .5f)
					.RotateYDeg(orientation)
					.Translate(0f, 0f, -.5f)
					.RotateZDeg((i - 1) * 40f)
					.Translate(-.5f, -.5f, 0f)
					.Values;

				prog.ExtraGlow = chevronGlow[9 - i];

				rpi.RenderMultiTextureMesh(chevronMeshRef, "tex");
			}

			prog.ExtraGlow = 0;
			prog.ModelMatrix = ModelMat.Identity()
				.Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
				.Translate(.5f, 3.325f, .5f)
				.RotateYDeg(orientation)
				.Translate(.0f, .0f, -.5f)
				.RotateZDeg(ringRotation)
				.Translate(-.5f, -.5f, .0f)
				.Values;

			prog.ViewMatrix = rpi.CameraMatrixOriginf;
			prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
			rpi.RenderMultiTextureMesh(ringMeshRef, "tex");
			prog.Stop();
		}

		public override void Dispose()
		{
			api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

			chevronMeshRef.Dispose();
			ringMeshRef.Dispose();
		}
	}
}
