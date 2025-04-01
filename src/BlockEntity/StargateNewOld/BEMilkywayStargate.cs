using AstriaPorta.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AstriaPorta.Content
{
	public class BEMilkywayStargate : BEStargate
	{
		protected override void InitializeRenderer(ICoreClientAPI capi)
		{
			if (!rendererInitialized || renderer == null)
			{
				MeshData ringMesh = StargateMeshHelper.GenRingMesh(capi, Block, "milkyway");
				MeshData chevronMesh = StargateMeshHelper.GenChevronMesh(capi, Block, "milkyway");

				renderer = new MilkywayGateRenderer((ICoreClientAPI)Api, Pos, chevronMesh, ringMesh);
				renderer.orientation = Block.Shape.rotateY;

				animUtil.InitializeAnimator("milkyway_chevron_animation", null, null, new Vec3f(0, Block.Shape.rotateY, 0));

				((ICoreClientAPI)Api).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
				UpdateRendererState();
				UpdateChevronGlow();
			}

			InitializeHorizonRenderer(capi, new Vec4f(0.2f, 0.2f, 0.8f, 0.5f));
		}
	}
}
