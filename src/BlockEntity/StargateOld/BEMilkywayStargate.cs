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
		protected override void InitializeRenderer(ICoreAPI api)
		{
			if (!rendererInitialized || renderer == null)
			{
				MeshData ringMesh = GenRingMesh();
				MeshData chevronMesh = GenChevronMesh();

				renderer = new MilkywayGateRenderer((ICoreClientAPI)Api, Pos, chevronMesh, ringMesh);
				renderer.orientation = Block.Shape.rotateY;

				animUtil.InitializeAnimator("milkyway_chevron_animation", null, null, new Vec3f(0, Block.Shape.rotateY, 0));

				((ICoreClientAPI)Api).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);
				UpdateRendererState();
				UpdateChevronGlow();
			}

			InitializeHorizonRenderer(api);
		}
	}
}
