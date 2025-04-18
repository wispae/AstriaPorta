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
			// thanks to ImNuts42 for solving the MultiTextureMeshRef problem
			MeshData mesh = StargateMeshHelper.GenDefaultHorizonMesh(api);
			horizonMeshRef = api.Render.UploadMesh(mesh);
			horizonMultiMeshRef = new MultiTextureMeshRef(new MeshRef[] { horizonMeshRef }, mesh.TextureIds);
			this.texPos = texPos;

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

            IShaderProgram prog = api.Shader.GetProgramByName("eventhorizon");
			prog.Use();

			prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
			prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
			prog.UniformMatrix("modelMatrix", baseModel.Values);

			prog.Uniform("rgbaTint", new Vec4f(0.2f, 0.2f, 0.8f, 0.5f));
			prog.Uniform("tIn", t);
			prog.Uniform("noiseOffset", noiseOffset);
			prog.Uniform("normalIn", blockFacing.Normalf);

			rpi.RenderMultiTextureMesh(horizonMultiMeshRef, "tex0");

			prog.Stop();
		}

		public void Dispose()
		{
			api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

			// horizonMeshRef?.Dispose();
			horizonMultiMeshRef?.Dispose();
		}
	}
}
