using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AstriaPorta.Content
{
	public class EventHorizonRenderer : IRenderer
	{
		private ICoreClientAPI api;
		private BlockPos pos;
		private MeshRef horizonMeshRef;
		private MultiTextureMeshRef horizonMultiMeshRef;
		public bool shouldRender = true;
		public bool activating = true;
		public BlockFacing blockFacing = BlockFacing.SOUTH;
		public IShaderProgram eventHorizonShaderProgram;
		public float t = 0f;
		public float noiseOffset = 0f;

		private TextureAtlasPosition texPos;

		public float orientation = 0f;
		public Matrixf ModelMat = new Matrixf();
		private Matrixf precalc;

		public EventHorizonRenderer(ICoreClientAPI api, BlockPos pos, TextureAtlasPosition texPos, bool activating)
		{
			this.api = api;
			this.pos = pos;
			this.activating = activating;
			horizonMeshRef = api.Render.UploadMesh(StargateMeshHelper.GenDefaultHorizonMesh(api));
			this.texPos = texPos;

			eventHorizonShaderProgram = api.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonShaderProgram;

			precalc = new Matrixf()
				.Identity()
				.Translate(0.5f, 0f, 0.5f)
				.RotateYDeg(orientation)
				.Translate(-0.5f, 0f, -0.5f);
		}

		public float Orientation
		{
			get
			{
				return orientation;
			}
			set
			{
				orientation = value;
				blockFacing = BlockFacing.SOUTH.FaceWhenRotatedBy(0f, orientation * GameMath.DEG2RAD, 0f);

				precalc = new Matrixf()
				.Identity()
				.Translate(0.5f, 0f, 0.5f)
				.RotateYDeg(orientation)
				.Translate(-0.5f, 0f, -0.5f);
			}
		}

		public double RenderOrder
		{
			get { return 0.9; }
		}

		public int RenderRange => 24;

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if (!shouldRender || horizonMeshRef == null) return;
			if (activating)
			{
				t += deltaTime;
				if (t > GameMath.PI)
				{
					t = 0;
					activating = false;
				}
			}
			noiseOffset += deltaTime;

			IRenderAPI rpi = api.Render;
			Vec3d camPos = api.World.Player.Entity.CameraPos;
			rpi.GlDisableCullFace();
			rpi.GlToggleBlend(true);

			Matrixf baseModel = ModelMat.Identity()
				.Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
				.Mul(precalc);

			IShaderProgram prog = eventHorizonShaderProgram;
			prog.Use();
			prog.BindTexture2D("tex0", texPos.atlasTextureId, 0);

			prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
			prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
			prog.UniformMatrix("modelMatrix", baseModel.Values);

			prog.Uniform("rgbaTint", new Vec4f(0.2f, 0.2f, 0.8f, 0.5f));
			prog.Uniform("tIn", t);
			prog.Uniform("noiseOffset", noiseOffset);
			prog.Uniform("normalIn", blockFacing.Normalf);

			rpi.RenderMesh(horizonMeshRef);

			prog.Stop();
		}

		public void Dispose()
		{
			api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

			horizonMeshRef?.Dispose();
		}
	}
}
