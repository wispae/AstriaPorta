using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace AstriaPorta.Content
{
	public class MilkywayRenderManager : StargateRenderManager
	{

		public MilkywayRenderManager(BEStargate gate, ICoreClientAPI capi) : base(gate, capi) { }

		public override void InitializeRenderer()
		{
			AssetLocation horizonLoc = new AssetLocation("astriaporta", "gates/kawoosh2");
			IShaderProgram horizonProgram = capi.ModLoader.GetModSystem<AstriaPortaModSystem>().eventHorizonShaderProgram;

			MeshData horizonMesh = StargateMeshHelper.GenRoundHorizonMesh(capi, 1.3f, 39, 16, 0.5f + 1.3f + 0.065f, 0.5f + 1.3f + 0.025f);

			eventHorizonRenderer = new EventHorizonRenderer(capi, gate.Pos, horizonMesh, horizonProgram, horizonLoc, false);
			eventHorizonRenderer.shouldRender = false;
			eventHorizonRenderer.Orientation = gate.Block.Shape.rotateY;

			horizonInitialized = true;
		}
	}
}
